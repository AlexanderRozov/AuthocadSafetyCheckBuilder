using Demo.Abstractions;
using Demo.Models;
using Demo.Services.Infrastructure;
using System.Collections.Generic;

namespace Demo.Services
{
    public static class DeviceCatalog
    {
        private static IDeviceCatalog Impl => PtServiceRegistry.DeviceCatalog;

        public static List<DeviceType> GetAll() => new List<DeviceType>(Impl.GetAll());
    }
}
