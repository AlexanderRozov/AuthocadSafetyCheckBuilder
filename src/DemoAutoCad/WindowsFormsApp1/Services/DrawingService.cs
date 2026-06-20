using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services
{
    public static class DrawingService
    {
        public static void EnsureLayers(Transaction tr, Database db)
        {
            EnsureLayer(tr, db, PtLayoutConstants.LayerTable, 7);
            EnsureLayer(tr, db, PtLayoutConstants.LayerBus, 1);
            EnsureLayer(tr, db, PtLayoutConstants.LayerDevices, 7);
            EnsureLayer(tr, db, PtLayoutConstants.LayerText, 7);
            EnsureLayer(tr, db, PtLayoutConstants.LayerLinks, 3);
            EnsureLayer(tr, db, PtLayoutConstants.LayerDetectorZone, 8);
        }

        private static double GetBlockFitScale(Transaction tr, ObjectId blockId)
        {
            try
            {
                var btr = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
                var ext = new Extents3d();
                var hasExtents = false;

                foreach (ObjectId id in btr)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null)
                        continue;

                    ext.AddExtents(ent.GeometricExtents);
                    hasExtents = true;
                }

                if (!hasExtents)
                    return 1;

                var width = ext.MaxPoint.X - ext.MinPoint.X;
                var height = ext.MaxPoint.Y - ext.MinPoint.Y;
                if (width < 0.001 || height < 0.001)
                    return 1;

                var scaleX = PtLayoutConstants.RectangleWidth / width;
                var scaleY = PtLayoutConstants.RectangleHeight / height;
                return Math.Min(scaleX, scaleY);
            }
            catch
            {
                return 1;
            }
        }

        private static void EnsureLayer(Transaction tr, Database db, string name, short colorIndex)
        {
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
            if (lt.Has(name))
                return;

            var layer = new LayerTableRecord
            {
                Name = name,
                Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
            };
            lt.Add(layer);
            tr.AddNewlyCreatedDBObject(layer, true);
        }

        public static ObjectId DrawRectangle(
            Transaction tr,
            BlockTableRecord ms,
            Point3d center,
            double width,
            double height)
        {
            var halfW = width / 2;
            var halfH = height / 2;

            var pline = new Polyline(4);
            pline.AddVertexAt(0, new Point2d(center.X - halfW, center.Y - halfH), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(center.X + halfW, center.Y - halfH), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(center.X + halfW, center.Y + halfH), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(center.X - halfW, center.Y + halfH), 0, 0, 0);
            pline.Closed = true;

            return AppendDeviceEntity(tr, ms, pline);
        }

        public static ObjectId DrawSquare(Transaction tr, BlockTableRecord ms, Point3d center, double side)
        {
            return DrawRectangle(tr, ms, center, side, side);
        }

        public static ObjectId DrawTriangle(Transaction tr, BlockTableRecord ms, Point3d center, double width, double height)
        {
            var halfW = width / 2;
            var halfH = height / 2;

            var pline = new Polyline(3);
            pline.AddVertexAt(0, new Point2d(center.X, center.Y + halfH), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(center.X - halfW, center.Y - halfH), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(center.X + halfW, center.Y - halfH), 0, 0, 0);
            pline.Closed = true;

            return AppendDeviceEntity(tr, ms, pline);
        }

        public static ObjectId DrawCircle(Transaction tr, BlockTableRecord ms, Point3d center, double radius)
        {
            var circle = new Circle(center, Vector3d.ZAxis, radius);
            return AppendDeviceEntity(tr, ms, circle);
        }

        public static ObjectId DrawDiamond(Transaction tr, BlockTableRecord ms, Point3d center, double width, double height)
        {
            var halfW = width / 2;
            var halfH = height / 2;

            var pline = new Polyline(4);
            pline.AddVertexAt(0, new Point2d(center.X, center.Y + halfH), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(center.X + halfW, center.Y), 0, 0, 0);
            pline.AddVertexAt(2, new Point2d(center.X, center.Y - halfH), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(center.X - halfW, center.Y), 0, 0, 0);
            pline.Closed = true;

            return AppendDeviceEntity(tr, ms, pline);
        }

        private static ObjectId AppendDeviceEntity(Transaction tr, BlockTableRecord ms, Entity entity)
        {
            entity.Layer = PtLayoutConstants.LayerDevices;
            entity.ColorIndex = 7;
            ms.AppendEntity(entity);
            tr.AddNewlyCreatedDBObject(entity, true);
            return entity.ObjectId;
        }

        public static double GetShapeHalfHeight(BlockTemplate template, double? customRadius = null)
        {
            if (customRadius.HasValue && (template?.ShapeType ?? DeviceShapeType.Rectangle) == DeviceShapeType.Circle)
                return customRadius.Value;

            var width = PtLayoutConstants.RectangleWidth;
            var height = PtLayoutConstants.RectangleHeight;
            var shapeType = template?.ShapeType ?? DeviceShapeType.Rectangle;

            switch (shapeType)
            {
                case DeviceShapeType.Square:
                case DeviceShapeType.Circle:
                    return Math.Min(width, height) / 2;
                default:
                    return height / 2;
            }
        }

        public static double GetShapeHalfHeight(BlockTemplate template) =>
            GetShapeHalfHeight(template, null);

        public static double GetShapeHalfHeight(PtObject obj)
        {
            var template = BlockCatalog.GetById(obj?.ShapeId);
            if (template != null)
                return GetShapeHalfHeight(template);

            return PtLayoutConstants.RectangleHeight / 2;
        }

        public static ObjectId DrawDeviceShape(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            BlockTemplate template,
            Point3d center,
            double? customRadius = null)
        {
            if (template == null)
                return DrawRectangle(tr, ms, center, PtLayoutConstants.RectangleWidth, PtLayoutConstants.RectangleHeight);

            if (template.IsAutoCadBlock)
                return InsertAutoCadBlock(tr, db, ms, template, center);

            var width = PtLayoutConstants.RectangleWidth;
            var height = PtLayoutConstants.RectangleHeight;
            var side = Math.Min(width, height);

            switch (template.ShapeType)
            {
                case DeviceShapeType.Square:
                    return DrawSquare(tr, ms, center, side);
                case DeviceShapeType.Triangle:
                    return DrawTriangle(tr, ms, center, width, height);
                case DeviceShapeType.Circle:
                    return DrawCircle(tr, ms, center, customRadius ?? side / 2);
                case DeviceShapeType.Diamond:
                    return DrawDiamond(tr, ms, center, width, height);
                default:
                    return DrawRectangle(tr, ms, center, width, height);
            }
        }

        private static ObjectId InsertAutoCadBlock(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            BlockTemplate blockTemplate,
            Point3d center)
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            if (!bt.Has(blockTemplate.BlockName))
                return DrawRectangle(tr, ms, center, PtLayoutConstants.RectangleWidth, PtLayoutConstants.RectangleHeight);

            var blockRef = new BlockReference(center, bt[blockTemplate.BlockName])
            {
                Layer = PtLayoutConstants.LayerDevices
            };

            var scale = GetBlockFitScale(tr, bt[blockTemplate.BlockName]);
            blockRef.ScaleFactors = new Scale3d(scale, scale, 1);

            ms.AppendEntity(blockRef);
            tr.AddNewlyCreatedDBObject(blockRef, true);
            return blockRef.ObjectId;
        }

        public static ObjectId InsertBlockOrRectangle(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            BlockTemplate blockTemplate,
            Point3d center)
        {
            return DrawDeviceShape(tr, db, ms, blockTemplate, center);
        }

        public static ObjectId DrawMText(
            Transaction tr,
            BlockTableRecord ms,
            Point3d position,
            string text,
            double textHeight,
            AttachmentPoint attachment)
        {
            var mtext = new MText
            {
                Contents = text,
                Location = position,
                TextHeight = textHeight,
                Attachment = attachment,
                Layer = PtLayoutConstants.LayerText,
                ColorIndex = 7
            };

            ms.AppendEntity(mtext);
            tr.AddNewlyCreatedDBObject(mtext, true);
            return mtext.ObjectId;
        }

        public static ObjectId DrawBusLine(
            Transaction tr,
            BlockTableRecord ms,
            Point3d start,
            Point3d end)
        {
            var line = new Polyline(2);
            line.AddVertexAt(0, new Point2d(start.X, start.Y), 0, 0, 0);
            line.AddVertexAt(1, new Point2d(end.X, end.Y), 0, 0, 0);
            line.Layer = PtLayoutConstants.LayerBus;
            line.ColorIndex = 1;

            ms.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);
            return line.ObjectId;
        }

        public static void UpdateBusLine(Transaction tr, ObjectId busLineId, Point3d start, Point3d end)
        {
            if (busLineId.IsNull)
                return;

            var line = (Polyline)tr.GetObject(busLineId, OpenMode.ForWrite);
            line.SetPointAt(0, new Point2d(start.X, start.Y));
            line.SetPointAt(1, new Point2d(end.X, end.Y));
        }

        public static ObjectId DrawArrow(
            Transaction tr,
            BlockTableRecord ms,
            Point3d from,
            Point3d to,
            double fromHalfHeight = 0,
            double toHalfHeight = 0)
        {
            var fromHalf = fromHalfHeight > 0
                ? fromHalfHeight
                : PtLayoutConstants.RectangleHeight / 2;
            var toHalf = toHalfHeight > 0
                ? toHalfHeight
                : PtLayoutConstants.RectangleHeight / 2;

            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001)
                return ObjectId.Null;

            var ux = dx / len;
            var uy = dy / len;
            var arrowLen = Math.Min(15, len * 0.3);
            var arrowAngle = Math.PI / 6;

            var endX = to.X - ux * (toHalf + 2);
            var endY = to.Y - uy * (toHalf + 2);
            var startX = from.X + ux * (fromHalf + 2);
            var startY = from.Y + uy * (fromHalf + 2);

            var pline = new Polyline(5);
            pline.AddVertexAt(0, new Point2d(startX, startY), 0, 0, 0);
            pline.AddVertexAt(1, new Point2d(endX, endY), 0, 0, 0);

            var cos = Math.Cos(arrowAngle);
            var sin = Math.Sin(arrowAngle);
            var ax1 = -ux * cos + uy * sin;
            var ay1 = -ux * sin - uy * cos;
            var ax2 = -ux * cos - uy * sin;
            var ay2 = ux * sin - uy * cos;

            pline.AddVertexAt(2, new Point2d(endX + ax1 * arrowLen, endY + ay1 * arrowLen), 0, 0, 0);
            pline.AddVertexAt(3, new Point2d(endX, endY), 0, 0, 0);
            pline.AddVertexAt(4, new Point2d(endX + ax2 * arrowLen, endY + ay2 * arrowLen), 0, 0, 0);

            pline.Layer = PtLayoutConstants.LayerLinks;
            pline.ColorIndex = 3;

            ms.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
            return pline.ObjectId;
        }

        public static PtObject DrawDevice(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            PlaceDeviceRequest request,
            Point3d center,
            int columnIndex,
            Guid tableId)
        {
            var fontSize = request.FontSize > 0
                ? request.FontSize
                : PtLayoutConstants.DefaultTextHeight;

            var entityId = DrawDeviceShape(tr, db, ms, request.BlockTemplate, center, request.DetectorRadius);

            var shapeHalfHeight = GetShapeHalfHeight(request.BlockTemplate, request.DetectorRadius);
            var idOffsetY = shapeHalfHeight + PtLayoutConstants.IdTextOffsetY;

            var labelId = DrawMText(
                tr, ms, center,
                request.Label,
                fontSize,
                AttachmentPoint.MiddleCenter);

            var idPosition = new Point3d(
                center.X,
                center.Y - idOffsetY,
                0);

            var idTextId = DrawMText(
                tr, ms, idPosition,
                request.FullId,
                fontSize,
                AttachmentPoint.TopCenter);

            var groupId = CreateObjectGroup(tr, db, request.Label, entityId, labelId, idTextId);

            var ptObject = new PtObject
            {
                InstanceId = Guid.NewGuid(),
                TableId = tableId,
                CatalogId = request.DeviceType.Id,
                Code = request.DeviceType.Code,
                Number = request.Number,
                Label = request.Label,
                FullName = request.DeviceType.Name,
                ColumnIndex = columnIndex,
                FontSize = fontSize,
                BlockName = request.BlockTemplate?.BlockName,
                ShapeId = request.BlockTemplate?.Id,
                BlockGroupId = request.BlockGroupId,
                Center = center,
                EntityId = entityId,
                LabelTextId = labelId,
                IdTextId = idTextId,
                GroupId = groupId
            };

            return ptObject;
        }

        public static ObjectId CreateObjectGroup(
            Transaction tr,
            Database db,
            string name,
            params ObjectId[] entityIds)
        {
            var gd = (DBDictionary)tr.GetObject(db.GroupDictionaryId, OpenMode.ForWrite);
            var group = new Group($"PT_{name}", true);
            foreach (var id in entityIds)
            {
                if (!id.IsNull)
                    group.Append(id);
            }

            var key = Guid.NewGuid().ToString("N");
            gd.SetAt(key, group);
            tr.AddNewlyCreatedDBObject(group, true);
            return group.ObjectId;
        }

        public static void EraseObject(Transaction tr, PtObject obj)
        {
            EraseEntity(tr, obj.GroupId);
            if (obj.GroupId.IsNull)
            {
                EraseEntity(tr, obj.EntityId);
                EraseEntity(tr, obj.LabelTextId);
                EraseEntity(tr, obj.IdTextId);
            }

            foreach (var link in PtObjectRepository.AllLinks
                .Where(l => l.FromObjectId == obj.InstanceId || l.ToObjectId == obj.InstanceId)
                .ToList())
            {
                EraseEntity(tr, link.ArrowId);
            }
        }

        public static void EraseEntity(Transaction tr, ObjectId id)
        {
            EraseIfValid(tr, id);
        }

        private static void EraseIfValid(Transaction tr, ObjectId id)
        {
            if (id.IsNull || id.IsErased)
                return;

            try
            {
                var ent = tr.GetObject(id, OpenMode.ForWrite, false);
                if (ent != null && !ent.IsErased)
                    ent.Erase();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                // entity may already be gone
            }
        }

        public static ObjectId DrawZoneBoundary(
            Transaction tr,
            BlockTableRecord ms,
            IList<BoundaryPoint> points)
        {
            if (points == null || points.Count < 3)
                return ObjectId.Null;

            var pline = new Polyline(points.Count);
            for (var i = 0; i < points.Count; i++)
                pline.AddVertexAt(i, new Point2d(points[i].X, points[i].Y), 0, 0, 0);

            pline.Closed = true;
            pline.Layer = PtLayoutConstants.LayerDetectorZone;
            pline.ColorIndex = 8;

            ms.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
            return pline.ObjectId;
        }

        public static ObjectId DrawZoneHatch(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            ObjectId boundaryId,
            string patternName)
        {
            if (boundaryId.IsNull)
                return ObjectId.Null;

            var hatch = new Hatch();
            ms.AppendEntity(hatch);
            tr.AddNewlyCreatedDBObject(hatch, true);

            hatch.SetHatchPattern(HatchPatternType.PreDefined, string.IsNullOrWhiteSpace(patternName) ? "ANSI31" : patternName);
            hatch.Associative = true;
            hatch.Layer = PtLayoutConstants.LayerDetectorZone;
            hatch.ColorIndex = 8;

            var loopIds = new ObjectIdCollection { boundaryId };
            hatch.AppendLoop(HatchLoopTypes.Default, loopIds);
            hatch.EvaluateHatch(true);

            return hatch.ObjectId;
        }

        public static ObjectId RecreateZoneHatch(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            ObjectId boundaryId,
            ObjectId oldHatchId,
            string patternName)
        {
            EraseIfValid(tr, oldHatchId);
            return DrawZoneHatch(tr, db, ms, boundaryId, patternName);
        }
    }
}
