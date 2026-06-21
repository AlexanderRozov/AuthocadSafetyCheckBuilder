using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace Pt.Services.Drawing
{
    internal static class AnnotationDrawer
    {
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

        public static ObjectId DrawBusLine(Transaction tr, BlockTableRecord ms, Point3d start, Point3d end)
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
    }
}
