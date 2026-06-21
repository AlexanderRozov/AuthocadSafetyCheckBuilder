namespace Pt.Models
{
    public class DeviceType
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
