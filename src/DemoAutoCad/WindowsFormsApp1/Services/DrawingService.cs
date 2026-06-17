using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;
using System;
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
            pline.Layer = PtLayoutConstants.LayerDevices;
            pline.ColorIndex = 7;

            ms.AppendEntity(pline);
            tr.AddNewlyCreatedDBObject(pline, true);
            return pline.ObjectId;
        }

        public static ObjectId InsertBlockOrRectangle(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            BlockTemplate blockTemplate,
            Point3d center)
        {
            if (blockTemplate == null || blockTemplate.IsRectangle)
                return DrawRectangle(tr, ms, center, PtLayoutConstants.RectangleWidth, PtLayoutConstants.RectangleHeight);

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
            Point3d to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.001)
                return ObjectId.Null;

            var ux = dx / len;
            var uy = dy / len;
            var arrowLen = Math.Min(15, len * 0.3);
            var arrowAngle = Math.PI / 6;

            var endX = to.X - ux * (PtLayoutConstants.RectangleHeight / 2 + 2);
            var endY = to.Y - uy * (PtLayoutConstants.RectangleHeight / 2 + 2);
            var startX = from.X + ux * (PtLayoutConstants.RectangleHeight / 2 + 2);
            var startY = from.Y + uy * (PtLayoutConstants.RectangleHeight / 2 + 2);

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

            var entityId = InsertBlockOrRectangle(tr, db, ms, request.BlockTemplate, center);

            var labelId = DrawMText(
                tr, ms, center,
                request.Label,
                fontSize,
                AttachmentPoint.MiddleCenter);

            var idPosition = new Point3d(
                center.X,
                center.Y - PtLayoutConstants.RectangleHeight / 2 - PtLayoutConstants.IdTextOffsetY,
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
            EraseIfValid(tr, obj.GroupId);
            if (obj.GroupId.IsNull)
            {
                EraseIfValid(tr, obj.EntityId);
                EraseIfValid(tr, obj.LabelTextId);
                EraseIfValid(tr, obj.IdTextId);
            }

            foreach (var link in PtObjectRepository.AllLinks
                .Where(l => l.FromObjectId == obj.InstanceId || l.ToObjectId == obj.InstanceId)
                .ToList())
            {
                EraseIfValid(tr, link.ArrowId);
            }
        }

        private static void EraseIfValid(Transaction tr, ObjectId id)
        {
            if (id.IsNull)
                return;

            var ent = tr.GetObject(id, OpenMode.ForWrite, false);
            if (ent != null && !ent.IsErased)
                ent.Erase();
        }
    }
}
