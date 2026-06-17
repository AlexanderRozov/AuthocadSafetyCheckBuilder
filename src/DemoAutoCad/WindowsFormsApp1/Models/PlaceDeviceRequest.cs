using Autodesk.AutoCAD.Geometry;

namespace Demo.Models
{
    public class PlaceDeviceRequest
    {
        public System.Guid TableId { get; set; }
        public DeviceType DeviceType { get; set; }
        public BlockTemplate BlockTemplate { get; set; }
        public string Number { get; set; }
        public double FontSize { get; set; }
        public System.Guid? ParentObjectId { get; set; }
        public System.Guid? BlockGroupId { get; set; }
        public Point3d InsertionPoint { get; set; }

        public string Label => $"{DeviceType.Code}-{Number}";

        public string FullId => $"*-{DeviceType.Code}-{Number}";
    }
}
