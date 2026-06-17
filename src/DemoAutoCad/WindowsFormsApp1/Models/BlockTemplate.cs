namespace Demo.Models
{
    public class BlockTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BlockName { get; set; }
        public DeviceShapeType ShapeType { get; set; } = DeviceShapeType.Rectangle;

        public bool IsAutoCadBlock => ShapeType == DeviceShapeType.AutoCadBlock;

        public override string ToString() => Name;
    }
}
