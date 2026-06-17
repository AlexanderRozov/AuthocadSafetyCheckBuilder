using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace Demo.Models
{
    public class PtTableSession
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Point3d Origin { get; set; }
        public ObjectId TableId { get; set; }
        public ObjectId BusLineId { get; set; }
        public int DeviceCount { get; set; }

        public override string ToString() => Name;
    }
}
