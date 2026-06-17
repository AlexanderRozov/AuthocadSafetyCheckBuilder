namespace Demo.Models
{
    public class BlockTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BlockName { get; set; }

        public bool IsRectangle => string.IsNullOrEmpty(BlockName);

        public override string ToString() => Name;
    }
}
