namespace Demo.Models
{
    /// <summary>
    /// User-defined composite object stored as an AutoCAD block in the drawing.
    /// </summary>
    public class PrecreatedTemplate
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string BlockName { get; set; }
        public string SourceFile { get; set; }
        public double ShapeHalfHeight { get; set; }
        public string PreviewImageBase64 { get; set; }

        public string DisplayLabel
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Code) && !string.IsNullOrWhiteSpace(Name))
                    return $"{Code} — {Name}";

                if (!string.IsNullOrWhiteSpace(Code))
                    return Code;

                return string.IsNullOrWhiteSpace(Name) ? BlockName : Name;
            }
        }

        public BlockTemplate ToBlockTemplate()
        {
            return new BlockTemplate
            {
                Id = Id,
                Name = DisplayLabel,
                BlockName = BlockName,
                ShapeType = DeviceShapeType.PrecreatedObject,
                ShapeHalfHeight = ShapeHalfHeight,
                SourceFile = SourceFile,
                Description = Description,
                Code = Code
            };
        }
    }
}
