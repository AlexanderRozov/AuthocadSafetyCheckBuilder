using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Pt.Models;

namespace Pt.Services
{
    public static class BlockPreviewRenderer
    {
        private const int ImageSize = 160;
        private const int MaxDepth = 4;

        public static string RenderBlockToBase64Png(Database db, string blockName)
        {
            if (db == null || string.IsNullOrWhiteSpace(blockName))
                return null;

            try
            {
                using (var bitmap = new Bitmap(ImageSize, ImageSize))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.Clear(Color.FromArgb(32, 32, 32));

                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                        if (!bt.Has(blockName))
                        {
                            tr.Commit();
                            return null;
                        }

                        var strokes = new List<StrokeSegment>();
                        CollectBlockStrokes(tr, bt[blockName], Matrix3d.Identity, 0, strokes);

                        if (strokes.Count == 0)
                        {
                            tr.Commit();
                            return null;
                        }

                        DrawStrokes(graphics, strokes);
                        tr.Commit();
                    }

                    using (var stream = new MemoryStream())
                    {
                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        return Convert.ToBase64String(stream.ToArray());
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public static void RefreshTemplatePreview(Database db, PrecreatedTemplate template)
        {
            if (template == null || string.IsNullOrWhiteSpace(template.BlockName))
                return;

            template.PreviewImageBase64 = RenderBlockToBase64Png(db, template.BlockName);
        }

        private sealed class StrokeSegment
        {
            public PointF A;
            public PointF B;
        }

        private static void CollectBlockStrokes(
            Transaction tr,
            ObjectId blockId,
            Matrix3d transform,
            int depth,
            List<StrokeSegment> strokes)
        {
            if (depth > MaxDepth || blockId.IsNull)
                return;

            var btr = tr.GetObject(blockId, OpenMode.ForRead) as BlockTableRecord;
            if (btr == null)
                return;

            foreach (ObjectId id in btr)
            {
                var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (ent == null)
                    continue;

                switch (ent)
                {
                    case Line line:
                        AddLine(strokes, line.StartPoint, line.EndPoint, transform);
                        break;
                    case Polyline polyline:
                        AddPolyline(strokes, polyline, transform);
                        break;
                    case Circle circle:
                        AddCircle(strokes, circle.Center, circle.Radius, transform);
                        break;
                    case Arc arc:
                        AddArc(strokes, arc, transform);
                        break;
                    case BlockReference blockRef:
                        var nested = blockRef.BlockTransform.PreMultiplyBy(transform);
                        CollectBlockStrokes(tr, blockRef.BlockTableRecord, nested, depth + 1, strokes);
                        break;
                }
            }
        }

        private static void AddLine(
            List<StrokeSegment> strokes,
            Point3d a,
            Point3d b,
            Matrix3d transform)
        {
            strokes.Add(new StrokeSegment
            {
                A = ToPointF(a.TransformBy(transform)),
                B = ToPointF(b.TransformBy(transform))
            });
        }

        private static void AddPolyline(List<StrokeSegment> strokes, Polyline polyline, Matrix3d transform)
        {
            for (var i = 0; i < polyline.NumberOfVertices; i++)
            {
                var a = polyline.GetPoint3dAt(i).TransformBy(transform);
                var b = polyline.GetPoint3dAt((i + 1) % polyline.NumberOfVertices).TransformBy(transform);
                if (i == polyline.NumberOfVertices - 1 && !polyline.Closed)
                    break;

                strokes.Add(new StrokeSegment { A = ToPointF(a), B = ToPointF(b) });
            }
        }

        private static void AddCircle(
            List<StrokeSegment> strokes,
            Point3d center,
            double radius,
            Matrix3d transform)
        {
            var c = center.TransformBy(transform);
            const int segments = 24;
            PointF? prev = null;
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * Math.PI * 2 / segments;
                var p = new PointF(
                    (float)(c.X + Math.Cos(angle) * radius),
                    (float)(c.Y + Math.Sin(angle) * radius));

                if (prev.HasValue)
                    strokes.Add(new StrokeSegment { A = prev.Value, B = p });

                prev = p;
            }
        }

        private static void AddArc(List<StrokeSegment> strokes, Arc arc, Matrix3d transform)
        {
            const int segments = 16;
            PointF? prev = null;
            var start = arc.StartAngle;
            var end = arc.EndAngle;
            if (end < start)
                end += Math.PI * 2;

            for (var i = 0; i <= segments; i++)
            {
                var angle = start + (end - start) * i / segments;
                var p3 = new Point3d(
                    arc.Center.X + Math.Cos(angle) * arc.Radius,
                    arc.Center.Y + Math.Sin(angle) * arc.Radius,
                    arc.Center.Z).TransformBy(transform);

                var p = ToPointF(p3);
                if (prev.HasValue)
                    strokes.Add(new StrokeSegment { A = prev.Value, B = p });

                prev = p;
            }
        }

        private static PointF ToPointF(Point3d point) =>
            new PointF((float)point.X, (float)point.Y);

        private static void DrawStrokes(Graphics graphics, List<StrokeSegment> strokes)
        {
            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;

            foreach (var stroke in strokes)
            {
                minX = Math.Min(minX, Math.Min(stroke.A.X, stroke.B.X));
                minY = Math.Min(minY, Math.Min(stroke.A.Y, stroke.B.Y));
                maxX = Math.Max(maxX, Math.Max(stroke.A.X, stroke.B.X));
                maxY = Math.Max(maxY, Math.Max(stroke.A.Y, stroke.B.Y));
            }

            var width = Math.Max(maxX - minX, 0.001f);
            var height = Math.Max(maxY - minY, 0.001f);
            var padding = 8f;
            var scale = Math.Min(
                (ImageSize - padding * 2) / width,
                (ImageSize - padding * 2) / height);

            var centerX = (minX + maxX) / 2f;
            var centerY = (minY + maxY) / 2f;
            var screenCenter = ImageSize / 2f;

            using (var pen = new Pen(Color.White, 1.6f))
            {
                foreach (var stroke in strokes)
                {
                    var a = Map(stroke.A, centerX, centerY, scale, screenCenter);
                    var b = Map(stroke.B, centerX, centerY, scale, screenCenter);
                    graphics.DrawLine(pen, a, b);
                }
            }
        }

        private static PointF Map(
            PointF point,
            float centerX,
            float centerY,
            float scale,
            float screenCenter)
        {
            return new PointF(
                screenCenter + (point.X - centerX) * scale,
                screenCenter - (point.Y - centerY) * scale);
        }
    }
}
