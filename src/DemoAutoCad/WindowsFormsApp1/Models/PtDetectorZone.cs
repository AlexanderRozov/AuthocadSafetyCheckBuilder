using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;

namespace Pt.Models
{
    public class PtDetectorZone
    {
        public Guid Id { get; set; }
        public Guid TableId { get; set; }
        public string Name { get; set; }
        public List<BoundaryPoint> BoundaryPoints { get; set; } = new List<BoundaryPoint>();
        public double Radius { get; set; }
        public double GridStep { get; set; }
        public string HatchPattern { get; set; }
        public DetectorGridDirection GridDirection { get; set; }
        public List<Guid> DetectorObjectIds { get; set; } = new List<Guid>();
        public ObjectId BoundaryId { get; set; }
        public ObjectId HatchId { get; set; }

        public override string ToString() =>
            $"{Name} ({DetectorObjectIds.Count} изв.)";
    }

    public class BoundaryPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public BoundaryPoint() { }

        public BoundaryPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
