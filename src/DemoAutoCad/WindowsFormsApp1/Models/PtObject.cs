using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace Demo.Models
{
    public class PtObject
    {
        public Guid InstanceId { get; set; }
        public string CatalogId { get; set; }
        public string Code { get; set; }
        public string Number { get; set; }
        public string Label { get; set; }
        public string FullName { get; set; }
        public int ColumnIndex { get; set; }
        public ObjectId RectangleId { get; set; }
        public ObjectId LabelTextId { get; set; }
        public ObjectId IdTextId { get; set; }
    }
}
