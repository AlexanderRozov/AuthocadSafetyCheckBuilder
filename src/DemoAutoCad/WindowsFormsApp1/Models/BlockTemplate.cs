namespace Pt.Models
{
    public class BlockTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BlockName { get; set; }
        public DeviceShapeType ShapeType { get; set; } = DeviceShapeType.Rectangle;
        public double ShapeHalfHeight { get; set; }
        public string SourceFile { get; set; }
        public string Description { get; set; }
        public string Code { get; set; }

        public bool IsAutoCadBlock => ShapeType == DeviceShapeType.AutoCadBlock;
        public bool IsPrecreatedObject => ShapeType == DeviceShapeType.PrecreatedObject;
        public bool UsesBlockReference => IsAutoCadBlock || IsPrecreatedObject;

        public override string ToString() => Name;
    }
}
