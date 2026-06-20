using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;
using System.Collections.Generic;

namespace Demo.Services.Drawing
{
    internal static class ZoneDrawer
    {
        public static ObjectId DrawZoneBoundary(Transaction tr, BlockTableRecord ms, IList<BoundaryPoint> points)
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
            EntityEraser.EraseIfValid(tr, oldHatchId);
            return DrawZoneHatch(tr, db, ms, boundaryId, patternName);
        }
    }
}
