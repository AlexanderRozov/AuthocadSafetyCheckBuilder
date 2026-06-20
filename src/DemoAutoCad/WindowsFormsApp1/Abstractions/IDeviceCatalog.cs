using Demo.Models;
using System.Collections.Generic;

namespace Demo.Abstractions
{
    public interface IDeviceCatalog
    {
        IReadOnlyList<DeviceType> GetAll();
    }
}
