using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Demo.Services
{
    public static class DetectorPlacementService
    {
        private const double Epsilon = 1e-6;
        /// <summary>Max grid cells per axis when building candidates/control points.</summary>
        private const int MaxGridCellsPerAxis = 60;
        /// <summary>Skip expensive refinement when the candidate set is large.</summary>
        private const int HeavyOptimizationCandidateLimit = 400;

        private sealed class CandidateCoverage
        {
            public Point2d Point { get; set; }
            public int[] ControlIndices { get; set; }
            public double Depth { get; set; }
            public bool IsInside { get; set; }

            public double ScoreFor(int coverCount) =>
                coverCount * 1_000_000.0 + Depth * 1_000.0 + (IsInside ? 500.0 : 0.0);
        }

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
            var centers = Task.Run(() =>
                ComputePlacements(boundary, radius, step, effectiveDirection)).GetAwaiter().GetResult();

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
            step = AdjustStepForGridSize(step, boundary, radius);
            var centroid = ComputeCentroid(boundary);

            var phaseOffsets = new[]
            {
                (0.0, 0.0),
                (step / 3.0, 0.0),
                (0.0, step / 3.0)
            };

            var phaseResults = new List<Point2d>[phaseOffsets.Length];
            Parallel.For(0, phaseOffsets.Length, i =>
            {
                var (offsetX, offsetY) = phaseOffsets[i];
                phaseResults[i] = ComputePlacementsWithPhase(
                    boundary, radius, step, direction, offsetX, offsetY, centroid);
            });

            List<Point2d> best = null;
            foreach (var result in phaseResults)
            {
                if (result == null)
                    continue;

                if (best == null || result.Count < best.Count ||
                    (result.Count == best.Count &&
                     InteriorScore(result, boundary) > InteriorScore(best, boundary)))
                {
                    best = result;
                }
            }

            return best ?? new List<Point2d>();
        }

        /// <summary>
        /// Coarsen step when the bounding box would produce too many grid cells (prevents UI freeze).
        /// </summary>
        private static double AdjustStepForGridSize(
            double step,
            IList<BoundaryPoint> boundary,
            double radius)
        {
            if (step <= Epsilon)
                return radius * Math.Sqrt(2);

            var minX = boundary.Min(p => p.X);
            var maxX = boundary.Max(p => p.X);
            var minY = boundary.Min(p => p.Y);
            var maxY = boundary.Max(p => p.Y);
            var extentX = maxX - minX + 2 * radius;
            var extentY = maxY - minY + 2 * radius;

            var minStepX = extentX / MaxGridCellsPerAxis;
            var minStepY = extentY / MaxGridCellsPerAxis;
            return Math.Max(step, Math.Max(minStepX, minStepY));
        }

        private static List<Point2d> ComputePlacementsWithPhase(
            IList<BoundaryPoint> boundary,
            double radius,
            double step,
            DetectorGridDirection direction,
            double offsetX,
            double offsetY,
            Point2d centroid)
        {
            var minX = boundary.Min(p => p.X) - radius;
            var maxX = boundary.Max(p => p.X) + radius;
            var minY = boundary.Min(p => p.Y) - radius;
            var maxY = boundary.Max(p => p.Y) + radius;

            var startX = minX + offsetX;
            var startY = minY + offsetY;

            var allCandidates = BuildCandidateGrid(
                boundary, radius, step, startX, startY, maxX, maxY);

            var insideCandidates = allCandidates
                .AsParallel()
                .Where(c => PointInPolygon(c, boundary))
                .ToList();
            var outsideCandidates = allCandidates
                .AsParallel()
                .Where(c => !PointInPolygon(c, boundary))
                .ToList();

            var controlPoints = BuildControlPoints(boundary, step);
            var insideCoverage = BuildCandidateCoverage(insideCandidates, controlPoints, boundary, radius);
            var selected = GreedyMinCover(insideCoverage, controlPoints, radius);
            selected = CompleteCoverWithOutsideRing(
                selected,
                BuildCandidateCoverage(outsideCandidates, controlPoints, boundary, radius),
                insideCoverage,
                controlPoints,
                radius);

            var pruned = PruneCandidates(selected, controlPoints, boundary, radius, direction);

            if (allCandidates.Count <= HeavyOptimizationCandidateLimit)
            {
                var compacted = CompactTowardCentroid(pruned, controlPoints, boundary, radius, centroid);
                return compacted;
            }

            return pruned;
        }

        private static List<Point2d> BuildCandidateGrid(
            IList<BoundaryPoint> boundary,
            double radius,
            double step,
            double startX,
            double startY,
            double maxX,
            double maxY)
        {
            var rowYs = new List<double>();
            for (var y = startY; y <= maxY + Epsilon; y += step)
                rowYs.Add(y);

            var bag = new ConcurrentBag<Point2d>();
            Parallel.ForEach(rowYs, y =>
            {
                for (var x = startX; x <= maxX + Epsilon; x += step)
                {
                    var center = new Point2d(x, y);
                    if (ShouldPlaceCenter(center, radius, boundary))
                        bag.Add(center);
                }
            });

            return bag.ToList();
        }

        private static List<CandidateCoverage> BuildCandidateCoverage(
            IList<Point2d> candidates,
            IList<Point2d> controlPoints,
            IList<BoundaryPoint> boundary,
            double radius)
        {
            if (candidates.Count == 0)
                return new List<CandidateCoverage>();

            var coverage = new CandidateCoverage[candidates.Count];
            Parallel.For(0, candidates.Count, i =>
            {
                var candidate = candidates[i];
                var indices = new List<int>();
                for (var j = 0; j < controlPoints.Count; j++)
                {
                    if (candidate.GetDistanceTo(controlPoints[j]) <= radius + Epsilon)
                        indices.Add(j);
                }

                coverage[i] = new CandidateCoverage
                {
                    Point = candidate,
                    ControlIndices = indices.ToArray(),
                    Depth = InwardDepth(candidate, boundary),
                    IsInside = PointInPolygon(candidate, boundary)
                };
            });

            return coverage.ToList();
        }

        /// <summary>
        /// Only adds outside-ring detectors when inside-only set cannot cover remaining control points.
        /// </summary>
        private static List<Point2d> CompleteCoverWithOutsideRing(
            List<Point2d> selected,
            List<CandidateCoverage> outsideCoverage,
            List<CandidateCoverage> insideCoverage,
            List<Point2d> controlPoints,
            double radius)
        {
            var result = selected.ToList();
            if (IsFullyCovered(controlPoints, result, radius))
                return result;

            var pool = outsideCoverage
                .Concat(insideCoverage.Where(c => result.All(r => r.GetDistanceTo(c.Point) > Epsilon)))
                .ToList();

            var remaining = new HashSet<int>();
            for (var i = 0; i < controlPoints.Count; i++)
            {
                if (!result.Any(c => c.GetDistanceTo(controlPoints[i]) <= radius + Epsilon))
                    remaining.Add(i);
            }

            while (remaining.Count > 0)
            {
                var best = FindBestCoverageCandidate(pool, remaining, result);
                if (!best.HasValue)
                    break;

                result.Add(best.Value);
                var previousCount = remaining.Count;
                remaining.RemoveWhere(i => best.Value.GetDistanceTo(controlPoints[i]) <= radius + Epsilon);
                if (remaining.Count >= previousCount)
                    break;
            }

            return result;
        }

        private static Point2d? FindBestCoverageCandidate(
            IList<CandidateCoverage> pool,
            HashSet<int> remaining,
            IList<Point2d> selected)
        {
            if (pool.Count == 0 || remaining.Count == 0)
                return null;

            var sync = new object();
            Point2d? best = null;
            var bestScore = double.NegativeInfinity;

            Parallel.ForEach(pool, candidate =>
            {
                if (selected.Any(r => r.GetDistanceTo(candidate.Point) <= Epsilon))
                    return;

                var covers = 0;
                foreach (var index in candidate.ControlIndices)
                {
                    if (remaining.Contains(index))
                        covers++;
                }

                if (covers == 0)
                    return;

                var score = candidate.ScoreFor(covers);

                lock (sync)
                {
                    if (score > bestScore + Epsilon)
                    {
                        best = candidate.Point;
                        bestScore = score;
                    }
                }
            });

            return best;
        }

        /// <summary>
        /// Greedy set cover: prefer candidates that cover the most uncovered control points;
        /// tie-break by inward depth (farther from boundary = better).
        /// </summary>
        private static List<Point2d> GreedyMinCover(
            List<CandidateCoverage> candidates,
            List<Point2d> controlPoints,
            double radius)
        {
            if (candidates.Count == 0 || controlPoints.Count == 0)
                return new List<Point2d>();

            var remaining = new HashSet<int>(Enumerable.Range(0, controlPoints.Count));
            var selected = new List<Point2d>();

            while (remaining.Count > 0)
            {
                var best = FindBestCoverageCandidate(candidates, remaining, selected);
                if (!best.HasValue)
                    break;

                selected.Add(best.Value);
                var previousCount = remaining.Count;
                remaining.RemoveWhere(i => best.Value.GetDistanceTo(controlPoints[i]) <= radius + Epsilon);
                if (remaining.Count >= previousCount)
                    break;
            }

            if (remaining.Count > 0)
            {
                foreach (var index in remaining)
                {
                    var point = controlPoints[index];
                    var nearest = candidates
                        .AsParallel()
                        .Where(c => c.Point.GetDistanceTo(point) <= radius + Epsilon)
                        .OrderByDescending(c => c.IsInside)
                        .ThenByDescending(c => c.Depth)
                        .Select(c => c.Point)
                        .FirstOrDefault();

                    if (nearest != null && selected.All(s => s.GetDistanceTo(nearest) > Epsilon))
                        selected.Add(nearest);
                }
            }

            return selected;
        }

        private static List<Point2d> CompactTowardCentroid(
            List<Point2d> centers,
            List<Point2d> controlPoints,
            IList<BoundaryPoint> boundary,
            double radius,
            Point2d centroid)
        {
            if (centers.Count == 0)
                return centers;

            var result = centers.Select(c => c).ToList();

            for (var pass = 0; pass < 1; pass++)
            {
                for (var i = 0; i < result.Count; i++)
                {
                    result[i] = ShiftTowardCentroid(
                        result, i, controlPoints, boundary, radius, centroid);
                }
            }

            return result;
        }

        private static Point2d ShiftTowardCentroid(
            List<Point2d> centers,
            int index,
            List<Point2d> controlPoints,
            IList<BoundaryPoint> boundary,
            double radius,
            Point2d centroid)
        {
            var current = centers[index];
            var dx = centroid.X - current.X;
            var dy = centroid.Y - current.Y;
            var len = Math.Sqrt(dx * dx + dy * dy);
            if (len < Epsilon)
                return current;

            var ux = dx / len;
            var uy = dy / len;
            var low = 0.0;
            var high = len;
            var best = current;

            for (var iter = 0; iter < 24; iter++)
            {
                var mid = (low + high) * 0.5;
                var trial = new Point2d(current.X + ux * mid, current.Y + uy * mid);

                if (!ShouldPlaceCenter(trial, radius, boundary))
                {
                    high = mid;
                    continue;
                }

                if (IsFullyCoveredWithReplacement(centers, index, trial, controlPoints, radius))
                {
                    best = trial;
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            return best;
        }

        private static bool IsFullyCoveredWithReplacement(
            List<Point2d> centers,
            int replaceIndex,
            Point2d replacement,
            IList<Point2d> controlPoints,
            double radius)
        {
            foreach (var point in controlPoints)
            {
                var covered = false;
                for (var i = 0; i < centers.Count; i++)
                {
                    var center = i == replaceIndex ? replacement : centers[i];
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

        private static double InteriorScore(IList<Point2d> centers, IList<BoundaryPoint> boundary)
        {
            if (centers.Count == 0)
                return 0;

            return centers.Average(c => InwardDepth(c, boundary));
        }

        private static Point2d ComputeCentroid(IList<BoundaryPoint> boundary)
        {
            if (boundary == null || boundary.Count == 0)
                return Point2d.Origin;

            double sx = 0, sy = 0;
            foreach (var p in boundary)
            {
                sx += p.X;
                sy += p.Y;
            }

            return new Point2d(sx / boundary.Count, sy / boundary.Count);
        }

        /// <summary>
        /// Minimum distance to boundary edges; inside points get positive depth.
        /// </summary>
        private static double InwardDepth(Point2d center, IList<BoundaryPoint> boundary)
        {
            var minDist = double.MaxValue;
            for (var i = 0; i < boundary.Count; i++)
            {
                var a = new Point2d(boundary[i].X, boundary[i].Y);
                var b = new Point2d(boundary[(i + 1) % boundary.Count].X, boundary[(i + 1) % boundary.Count].Y);
                minDist = Math.Min(minDist, DistanceToSegment(center, a, b));
            }

            if (PointInPolygon(center, boundary))
                return minDist;

            return minDist * 0.5;
        }

        private static List<Point2d> PruneCandidates(
            List<Point2d> candidates,
            List<Point2d> controlPoints,
            IList<BoundaryPoint> boundary,
            double radius,
            DetectorGridDirection direction)
        {
            if (candidates.Count == 0)
                return candidates;

            var ordered = OrderForPruning(candidates, boundary, direction);
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
            IList<BoundaryPoint> boundary,
            DetectorGridDirection direction)
        {
            var parallel = candidates.AsParallel();
            switch (direction)
            {
                case DetectorGridDirection.VerticalColumns:
                    return parallel
                        .OrderByDescending(p => InwardDepth(p, boundary))
                        .ThenBy(p => p.X)
                        .ThenBy(p => p.Y)
                        .ToList();
                case DetectorGridDirection.HorizontalRows:
                default:
                    return parallel
                        .OrderByDescending(p => InwardDepth(p, boundary))
                        .ThenBy(p => p.Y)
                        .ThenBy(p => p.X)
                        .ToList();
            }
        }

        private static List<Point2d> BuildControlPoints(IList<BoundaryPoint> boundary, double step)
        {
            var points = new List<Point2d>();
            var seen = new ConcurrentDictionary<long, byte>();

            void TryAdd(Point2d p)
            {
                var key = QuantizeKey(p, step);
                if (seen.TryAdd(key, 0))
                    points.Add(p);
            }

            for (var i = 0; i < boundary.Count; i++)
            {
                var a = boundary[i];
                var b = boundary[(i + 1) % boundary.Count];
                TryAdd(new Point2d(a.X, a.Y));
                TryAdd(new Point2d((a.X + b.X) / 2, (a.Y + b.Y) / 2));
            }

            var minX = boundary.Min(p => p.X);
            var maxX = boundary.Max(p => p.X);
            var minY = boundary.Min(p => p.Y);
            var maxY = boundary.Max(p => p.Y);

            var rowYs = new List<double>();
            for (var y = minY + step * 0.5; y <= maxY + Epsilon; y += step)
                rowYs.Add(y);

            var interiorBag = new ConcurrentBag<Point2d>();
            Parallel.ForEach(rowYs, y =>
            {
                for (var x = minX + step * 0.5; x <= maxX + Epsilon; x += step)
                {
                    var p = new Point2d(x, y);
                    if (!PointInPolygon(p, boundary))
                        continue;

                    var key = QuantizeKey(p, step);
                    if (seen.TryAdd(key, 0))
                        interiorBag.Add(p);
                }
            });

            foreach (var p in interiorBag)
                points.Add(p);

            return points;
        }

        private static long QuantizeKey(Point2d p, double step)
        {
            var q = Math.Max(step, Epsilon);
            var ix = (long)Math.Round(p.X / q);
            var iy = (long)Math.Round(p.Y / q);
            return (ix << 32) ^ (iy & 0xFFFFFFFFL);
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
