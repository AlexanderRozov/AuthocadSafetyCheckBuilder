using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace Pt.Services.Drawing
{
    internal static class LabelTextFitter
    {
        private const double MinTextHeight = 1.0;
        private const int MaxIterations = 24;

        public static ObjectId DrawInsideLabel(
            Transaction tr,
            BlockTableRecord ms,
            Point3d center,
            string text,
            double preferredHeight,
            ShapeInnerBounds innerBounds)
        {
            var mtext = new MText
            {
                Location = center,
                Attachment = AttachmentPoint.MiddleCenter,
                Layer = PtLayoutConstants.LayerText,
                ColorIndex = 7
            };

            ms.AppendEntity(mtext);
            tr.AddNewlyCreatedDBObject(mtext, true);
            ApplyFit(mtext, text, preferredHeight, innerBounds);
            return mtext.ObjectId;
        }

        public static void ApplyFit(
            MText mtext,
            string text,
            double preferredHeight,
            ShapeInnerBounds innerBounds)
        {
            mtext.Contents = text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(mtext.Contents))
            {
                mtext.TextHeight = preferredHeight > 0
                    ? preferredHeight
                    : PtLayoutConstants.DefaultTextHeight;
                return;
            }

            var maxWidth = innerBounds.InnerWidth;
            var maxHeight = innerBounds.InnerHeight;
            var height = preferredHeight > 0 ? preferredHeight : PtLayoutConstants.DefaultTextHeight;

            mtext.Width = maxWidth;
            mtext.TextHeight = height;
            mtext.LineSpacingStyle = LineSpacingStyle.AtLeast;
            mtext.LineSpacingFactor = 0.85;

            for (var i = 0; i < MaxIterations; i++)
            {
                if (Fits(mtext, maxWidth, maxHeight))
                    return;

                height *= 0.9;
                if (height < MinTextHeight)
                {
                    mtext.TextHeight = MinTextHeight;
                    return;
                }

                mtext.TextHeight = height;
            }
        }

        private static bool Fits(MText mtext, double maxWidth, double maxHeight)
        {
            try
            {
                var width = mtext.ActualWidth;
                var height = mtext.ActualHeight;
                if (width > 0.001 && height > 0.001)
                    return width <= maxWidth + 0.05 && height <= maxHeight + 0.05;

                var ext = mtext.GeometricExtents;
                width = ext.MaxPoint.X - ext.MinPoint.X;
                height = ext.MaxPoint.Y - ext.MinPoint.Y;
                return width <= maxWidth + 0.05 && height <= maxHeight + 0.05;
            }
            catch
            {
                return true;
            }
        }
    }
}
