using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace Demo.Services
{
    public static class DetectorAreaPicker
    {
        public static List<Point3d> PickPolyline(Editor ed)
        {
            if (ed == null)
                return null;

            var points = new List<Point3d>();

            while (true)
            {
                var options = points.Count == 0
                    ? new PromptPointOptions("\nУкажите первую точку контура:")
                    : new PromptPointOptions("\nСледующая точка контура (Enter — замкнуть):")
                    {
                        UseBasePoint = true,
                        BasePoint = points[points.Count - 1]
                    };

                options.AllowNone = true;
                var result = ed.GetPoint(options);

                if (result.Status == PromptStatus.OK)
                {
                    points.Add(result.Value);
                    continue;
                }

                if (result.Status == PromptStatus.None && points.Count >= 3)
                    return points;

                return null;
            }
        }
    }
}
