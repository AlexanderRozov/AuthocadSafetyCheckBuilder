namespace Demo.Models
{
    public class PlaceDeviceRequest
    {
        public DeviceType DeviceType { get; set; }
        public string Number { get; set; }

        public string Label => $"{DeviceType.Code}-{Number}";

        public string FullId => $"*-{DeviceType.Code}-{Number}";
    }
}
