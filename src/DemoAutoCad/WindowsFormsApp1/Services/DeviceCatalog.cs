using System.Collections.Generic;
using Pt.Abstractions;
using Pt.Models;
using Pt.Services.Infrastructure;

namespace Pt.Services
{
    public static class DeviceCatalog
    {
        private static IDeviceCatalog Impl => PtServiceRegistry.DeviceCatalog;

        public static List<DeviceType> GetAll() => new List<DeviceType>(Impl.GetAll());
    }
}
