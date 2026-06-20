using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;
using System;

namespace Demo.Services.Drawing
{
    internal static class ShapeDrawer
    {
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

        public static ObjectId DrawSquare(Transaction tr, BlockTableRecord ms, Point3d center, double side) =>
            DrawRectangle(tr, ms, center, side, side);

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

        private static ObjectId AppendDeviceEntity(Transaction tr, BlockTableRecord ms, Entity entity)
        {
            entity.Layer = PtLayoutConstants.LayerDevices;
            entity.ColorIndex = 7;
            ms.AppendEntity(entity);
            tr.AddNewlyCreatedDBObject(entity, true);
            return entity.ObjectId;
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
    }
}
