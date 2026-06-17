using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;

namespace Demo.Models
{
    public class PtObject
    {
        public Guid InstanceId { get; set; }
        public Guid TableId { get; set; }
        public string CatalogId { get; set; }
        public string Code { get; set; }
        public string Number { get; set; }
        public string Label { get; set; }
        public string FullName { get; set; }
        public string FdCode { get; set; }
        public string JsCode { get; set; }
        public int ColumnIndex { get; set; }
        public int ColumnNumber { get; set; }
        public double FontSize { get; set; }
        public string BlockName { get; set; }
        public string ShapeId { get; set; }
        public Point3d Center { get; set; }
        public Guid? ParentObjectId { get; set; }
        public Guid? BlockGroupId { get; set; }
        public ObjectId GroupId { get; set; }
        public ObjectId EntityId { get; set; }
        public ObjectId LabelTextId { get; set; }
        public ObjectId IdTextId { get; set; }
    }
}
