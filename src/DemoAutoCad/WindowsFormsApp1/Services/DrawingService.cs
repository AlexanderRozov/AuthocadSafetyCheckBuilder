using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;

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
        }

        private static void EnsureLayer(Transaction tr, Database db, string name, short colorIndex)
        {
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(name))
                return;

            lt.UpgradeOpen();
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

        public static PtObject DrawDevice(
            Transaction tr,
            BlockTableRecord ms,
            PlaceDeviceRequest request,
            Point3d center,
            int columnIndex)
        {
            var rectId = DrawRectangle(
                tr, ms, center,
                PtLayoutConstants.RectangleWidth,
                PtLayoutConstants.RectangleHeight);

            var labelId = DrawMText(
                tr, ms, center,
                request.Label,
                PtLayoutConstants.TextHeight,
                AttachmentPoint.MiddleCenter);

            var idPosition = new Point3d(
                center.X,
                center.Y - PtLayoutConstants.RectangleHeight / 2 - PtLayoutConstants.IdTextOffsetY,
                0);

            var idTextId = DrawMText(
                tr, ms, idPosition,
                request.FullId,
                PtLayoutConstants.TextHeight,
                AttachmentPoint.TopCenter);

            return new PtObject
            {
                InstanceId = System.Guid.NewGuid(),
                CatalogId = request.DeviceType.Id,
                Code = request.DeviceType.Code,
                Number = request.Number,
                Label = request.Label,
                FullName = request.DeviceType.Name,
                ColumnIndex = columnIndex,
                RectangleId = rectId,
                LabelTextId = labelId,
                IdTextId = idTextId
            };
        }
    }
}
