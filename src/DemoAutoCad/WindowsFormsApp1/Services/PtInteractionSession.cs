using Demo.Models;
using System;

namespace Demo.Services
{
    public static class PtInteractionSession
    {
        public enum InteractionType
        {
            None,
            AddDevice,
            CreateTable,
            PlaceDetectors
        }

        public static InteractionType Type { get; private set; } = InteractionType.None;

        public static PlaceDeviceRequest DeviceRequest { get; private set; }

        public static Guid DetectorTableId { get; private set; }
        public static double DetectorRadius { get; private set; }
        public static DetectorGridDirection DetectorDirection { get; private set; }
        public static string DetectorHatchPattern { get; private set; }

        public static void BeginAddDevice(PlaceDeviceRequest request)
        {
            DeviceRequest = request;
            Type = InteractionType.AddDevice;
        }

        public static void BeginCreateTable()
        {
            Type = InteractionType.CreateTable;
        }

        public static void BeginPlaceDetectors(
            Guid tableId,
            double radius,
            DetectorGridDirection direction,
            string hatchPattern)
        {
            DetectorTableId = tableId;
            DetectorRadius = radius;
            DetectorDirection = direction;
            DetectorHatchPattern = hatchPattern;
            Type = InteractionType.PlaceDetectors;
        }

        public static void Clear()
        {
            Type = InteractionType.None;
            DeviceRequest = null;
        }
    }
}
