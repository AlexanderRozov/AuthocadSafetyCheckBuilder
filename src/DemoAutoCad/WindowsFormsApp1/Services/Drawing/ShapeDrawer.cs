using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using Pt.Models;

namespace Pt.Services.Drawing
{
    internal readonly struct ShapeInnerBounds
    {
        public ShapeInnerBounds(double halfWidth, double halfHeight, double paddingFactor = 0.9)
        {
            HalfWidth = halfWidth;
            HalfHeight = halfHeight;
            PaddingFactor = paddingFactor;
        }

        public double HalfWidth { get; }
        public double HalfHeight { get; }
        public double PaddingFactor { get; }

        public double InnerWidth => HalfWidth * 2 * PaddingFactor;
        public double InnerHeight => HalfHeight * 2 * PaddingFactor;
    }

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

            if (template.IsPrecreatedObject)
                return InsertPrecreatedBlock(tr, db, ms, template, center);

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

        public static ShapeInnerBounds GetShapeInnerBounds(
            BlockTemplate template,
            double? customRadius = null)
        {
            if (template?.IsPrecreatedObject == true && template.ShapeHalfHeight > 0)
            {
                var halfHeight = template.ShapeHalfHeight;
                var aspect = PtLayoutConstants.RectangleWidth / PtLayoutConstants.RectangleHeight;
                return new ShapeInnerBounds(halfHeight * aspect, halfHeight);
            }

            if (customRadius.HasValue &&
                (template?.ShapeType ?? DeviceShapeType.Rectangle) == DeviceShapeType.Circle)
            {
                var inscribed = customRadius.Value / Math.Sqrt(2);
                return new ShapeInnerBounds(inscribed, inscribed);
            }

            var width = PtLayoutConstants.RectangleWidth;
            var height = PtLayoutConstants.RectangleHeight;
            var shapeType = template?.ShapeType ?? DeviceShapeType.Rectangle;

            switch (shapeType)
            {
                case DeviceShapeType.Square:
                case DeviceShapeType.Circle:
                    var side = Math.Min(width, height);
                    var half = side / 2;
                    if (shapeType == DeviceShapeType.Circle)
                    {
                        var inscribed = half / Math.Sqrt(2);
                        return new ShapeInnerBounds(inscribed, inscribed);
                    }

                    return new ShapeInnerBounds(half, half);
                case DeviceShapeType.Triangle:
                    return new ShapeInnerBounds(width / 2 * 0.75, height / 2 * 0.55);
                case DeviceShapeType.Diamond:
                    return new ShapeInnerBounds(width / 2 * 0.65, height / 2 * 0.65);
                default:
                    return new ShapeInnerBounds(width / 2, height / 2);
            }
        }

        public static ShapeInnerBounds GetShapeInnerBounds(
            Transaction tr,
            BlockTemplate template,
            ObjectId entityId,
            double? customRadius = null)
        {
            if (!customRadius.HasValue &&
                (template?.ShapeType ?? DeviceShapeType.Rectangle) == DeviceShapeType.Circle &&
                !entityId.IsNull)
            {
                try
                {
                    var circle = tr.GetObject(entityId, OpenMode.ForRead) as Circle;
                    if (circle != null)
                        customRadius = circle.Radius;
                }
                catch
                {
                    // ignore
                }
            }

            return GetShapeInnerBounds(template, customRadius);
        }

        public static double GetShapeHalfHeight(BlockTemplate template, double? customRadius = null)
        {
            if (template?.IsPrecreatedObject == true && template.ShapeHalfHeight > 0)
                return template.ShapeHalfHeight;

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

        private static ObjectId InsertPrecreatedBlock(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            BlockTemplate blockTemplate,
            Point3d center)
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            if (string.IsNullOrEmpty(blockTemplate.BlockName) || !bt.Has(blockTemplate.BlockName))
                return DrawRectangle(tr, ms, center, PtLayoutConstants.RectangleWidth, PtLayoutConstants.RectangleHeight);

            var blockRef = new BlockReference(center, bt[blockTemplate.BlockName])
            {
                Layer = PtLayoutConstants.LayerDevices
            };

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
