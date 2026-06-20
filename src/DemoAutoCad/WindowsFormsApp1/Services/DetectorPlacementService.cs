using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services
{
    public static class DetectorPlacementService
    {
        private const double Epsilon = 1e-6;

        public static PtDetectorZone PlaceInArea(
            Database db,
            Guid tableId,
            IList<Point3d> boundaryPoints3d,
            double radius,
            double? gridStepOverride,
            DetectorGridDirection direction,
            string hatchPattern)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));

            if (boundaryPoints3d == null || boundaryPoints3d.Count < 3)
                throw new ArgumentException("Контур должен содержать минимум 3 точки.");

            if (radius <= 0)
                throw new ArgumentException("Радиус должен быть больше нуля.");

            var boundary = boundaryPoints3d
                .Select(p => new BoundaryPoint(p.X, p.Y))
                .ToList();

            var step = gridStepOverride ?? radius * Math.Sqrt(2);
            var effectiveDirection = ResolveDirection(direction, boundary);
            var centers = ComputePlacements(boundary, radius, step, effectiveDirection);

            var bth = DeviceCatalog.GetAll().FirstOrDefault(d => d.Id == "bth")
                ?? throw new InvalidOperationException("Тип BTH не найден в каталоге.");
            var circleTemplate = BlockCatalog.GetById("circle")
                ?? throw new InvalidOperationException("Форма «круг» не найдена.");

            ObjectId boundaryId;
            ObjectId hatchId;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                DrawingService.EnsureLayers(tr, db);
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                boundaryId = DrawingService.DrawZoneBoundary(tr, ms, boundary);
                hatchId = DrawingService.DrawZoneHatch(tr, db, ms, boundaryId, hatchPattern);
                tr.Commit();
            }

            var detectorIds = new List<Guid>();
            foreach (var center in centers)
            {
                var request = new PlaceDeviceRequest
                {
                    TableId = tableId,
                    DeviceType = bth,
                    BlockTemplate = circleTemplate,
                    Number = PtObjectRepository.GetNextNumber(bth.Code),
                    FontSize = PtLayoutConstants.DefaultTextHeight,
                    CustomLabel = null,
                    InsertionPoint = new Point3d(center.X, center.Y, 0),
                    DetectorRadius = radius
                };

                var ptObject = PtLayoutManager.AddDevice(db, request);
                detectorIds.Add(ptObject.InstanceId);
            }

            var zone = PtDetectorZoneRepository.Create(
                tableId,
                boundary,
                radius,
                step,
                hatchPattern,
                effectiveDirection,
                boundaryId,
                hatchId,
                detectorIds);

            PtPersistenceService.Save(db, PtDocumentRegistry.GetByDatabase(db));
            return zone;
        }

        public static void DeleteZone(Database db, Guid zoneId)
        {
            var zone = PtDetectorZoneRepository.Get(zoneId);
            if (zone == null)
                return;

            foreach (var objectId in zone.DetectorObjectIds.ToList())
            {
                var obj = PtObjectRepository.Get(objectId);
                if (obj != null)
                    PtLayoutManager.DeleteObject(db, obj);
            }

            using (var tr = db.TransactionManager.StartTransaction())
            {
                DrawingService.EraseEntity(tr, zone.HatchId);
                DrawingService.EraseEntity(tr, zone.BoundaryId);
                tr.Commit();
            }

            PtDetectorZoneRepository.Remove(zoneId);
            PtPersistenceService.Save(db, PtDocumentRegistry.GetByDatabase(db));
        }

        public static void ApplyHatchPattern(Database db, Guid zoneId, string patternName)
        {
            var zone = PtDetectorZoneRepository.Get(zoneId);
            if (zone == null || zone.BoundaryId.IsNull)
                return;

            ObjectId newHatchId;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                newHatchId = DrawingService.RecreateZoneHatch(
                    tr, db, ms, zone.BoundaryId, zone.HatchId, patternName);
                tr.Commit();
            }

            PtDetectorZoneRepository.UpdateHatchPattern(zoneId, patternName, newHatchId);
            PtPersistenceService.Save(db, PtDocumentRegistry.GetByDatabase(db));
        }

        private static DetectorGridDirection ResolveDirection(
            DetectorGridDirection direction,
            IList<BoundaryPoint> boundary)
        {
            if (direction != DetectorGridDirection.Auto)
                return direction;

            var minX = boundary.Min(p => p.X);
            var maxX = boundary.Max(p => p.X);
            var minY = boundary.Min(p => p.Y);
            var maxY = boundary.Max(p => p.Y);

            return (maxX - minX) > (maxY - minY)
                ? DetectorGridDirection.HorizontalRows
                : DetectorGridDirection.VerticalColumns;
        }

        private static List<Point2d> ComputePlacements(
            IList<BoundaryPoint> boundary,
            double radius,
            double step,
            DetectorGridDirection direction)
        {
            var minX = boundary.Min(p => p.X) - radius;
            var maxX = boundary.Max(p => p.X) + radius;
            var minY = boundary.Min(p => p.Y) - radius;
            var maxY = boundary.Max(p => p.Y) + radius;

            var candidates = new List<Point2d>();
            for (var y = minY; y <= maxY + Epsilon; y += step)
            {
                for (var x = minX; x <= maxX + Epsilon; x += step)
                {
                    var center = new Point2d(x, y);
                    if (ShouldPlaceCenter(center, radius, boundary))
                        candidates.Add(center);
                }
            }

            var controlPoints = BuildControlPoints(boundary);
            var pruned = PruneCandidates(candidates, controlPoints, radius, direction);
            return pruned;
        }

        private static List<Point2d> PruneCandidates(
            List<Point2d> candidates,
            List<Point2d> controlPoints,
            double radius,
            DetectorGridDirection direction)
        {
            if (candidates.Count == 0)
                return candidates;

            var ordered = OrderForPruning(candidates, direction);
            var kept = new List<Point2d>(ordered);

            for (var i = kept.Count - 1; i >= 0; i--)
            {
                var trial = kept.Where((_, idx) => idx != i).ToList();
                if (trial.Count == 0)
                    break;

                if (IsFullyCovered(controlPoints, trial, radius))
                    kept.RemoveAt(i);
            }

            return kept;
        }

        private static List<Point2d> OrderForPruning(
            List<Point2d> candidates,
            DetectorGridDirection direction)
        {
            switch (direction)
            {
                case DetectorGridDirection.VerticalColumns:
                    return candidates
                        .OrderBy(p => p.X)
                        .ThenBy(p => p.Y)
                        .ToList();
                case DetectorGridDirection.HorizontalRows:
                default:
                    return candidates
                        .OrderBy(p => p.Y)
                        .ThenBy(p => p.X)
                        .ToList();
            }
        }

        private static List<Point2d> BuildControlPoints(IList<BoundaryPoint> boundary)
        {
            var points = new List<Point2d>();
            for (var i = 0; i < boundary.Count; i++)
            {
                var a = boundary[i];
                var b = boundary[(i + 1) % boundary.Count];
                points.Add(new Point2d(a.X, a.Y));
                points.Add(new Point2d((a.X + b.X) / 2, (a.Y + b.Y) / 2));
            }

            return points;
        }

        private static bool ShouldPlaceCenter(Point2d center, double radius, IList<BoundaryPoint> boundary)
        {
            if (PointInPolygon(center, boundary))
                return true;

            for (var i = 0; i < boundary.Count; i++)
            {
                var a = new Point2d(boundary[i].X, boundary[i].Y);
                var b = new Point2d(boundary[(i + 1) % boundary.Count].X, boundary[(i + 1) % boundary.Count].Y);
                if (DistanceToSegment(center, a, b) <= radius + Epsilon)
                    return true;
            }

            foreach (var vertex in boundary)
            {
                var v = new Point2d(vertex.X, vertex.Y);
                if (center.GetDistanceTo(v) <= radius + Epsilon)
                    return true;
            }

            return false;
        }

        private static bool IsFullyCovered(IList<Point2d> controlPoints, IList<Point2d> centers, double radius)
        {
            foreach (var point in controlPoints)
            {
                var covered = false;
                foreach (var center in centers)
                {
                    if (center.GetDistanceTo(point) <= radius + Epsilon)
                    {
                        covered = true;
                        break;
                    }
                }

                if (!covered)
                    return false;
            }

            return true;
        }

        private static bool PointInPolygon(Point2d point, IList<BoundaryPoint> polygon)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var xi = polygon[i].X;
                var yi = polygon[i].Y;
                var xj = polygon[j].X;
                var yj = polygon[j].Y;

                var intersect = yi > point.Y != yj > point.Y &&
                    point.X < (xj - xi) * (point.Y - yi) / (yj - yi + Epsilon) + xi;

                if (intersect)
                    inside = !inside;
            }

            return inside;
        }

        private static double DistanceToSegment(Point2d p, Point2d a, Point2d b)
        {
            var dx = b.X - a.X;
            var dy = b.Y - a.Y;
            var lenSq = dx * dx + dy * dy;

            if (lenSq < Epsilon)
                return p.GetDistanceTo(a);

            var t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq;
            t = Math.Max(0, Math.Min(1, t));

            var proj = new Point2d(a.X + t * dx, a.Y + t * dy);
            return p.GetDistanceTo(proj);
        }
    }
}
