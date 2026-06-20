using System;
using System.Collections.Generic;

namespace Demo.Models
{
    public class PtDrawingSnapshot
    {
        public int Version { get; set; } = 1;
        public string ActiveTableId { get; set; }
        public int TableCounter { get; set; }
        public int BlockCounter { get; set; }
        public Dictionary<string, int> NumberCounters { get; set; } = new Dictionary<string, int>();
        public List<TableSnapshot> Tables { get; set; } = new List<TableSnapshot>();
        public List<ObjectSnapshot> Objects { get; set; } = new List<ObjectSnapshot>();
        public List<LinkSnapshot> Links { get; set; } = new List<LinkSnapshot>();
        public List<BlockSnapshot> Blocks { get; set; } = new List<BlockSnapshot>();
    }

    public class TableSnapshot
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double OriginX { get; set; }
        public double OriginY { get; set; }
        public double OriginZ { get; set; }
        public long? TableHandle { get; set; }
        public long? BusLineHandle { get; set; }
        public int DeviceCount { get; set; }
    }

    public class ObjectSnapshot
    {
        public string InstanceId { get; set; }
        public string TableId { get; set; }
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
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double CenterZ { get; set; }
        public string ParentObjectId { get; set; }
        public string BlockGroupId { get; set; }
        public long? GroupHandle { get; set; }
        public long? EntityHandle { get; set; }
        public long? LabelTextHandle { get; set; }
        public long? IdTextHandle { get; set; }
    }

    public class LinkSnapshot
    {
        public string Id { get; set; }
        public string TableId { get; set; }
        public string FromObjectId { get; set; }
        public string ToObjectId { get; set; }
        public long? ArrowHandle { get; set; }
    }

    public class BlockSnapshot
    {
        public string Id { get; set; }
        public string TableId { get; set; }
        public string Name { get; set; }
    }
}
