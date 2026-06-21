using System.Collections.Generic;
using Pt.Models;

namespace Pt.Abstractions
{
    public interface IDeviceCatalog
    {
        IReadOnlyList<DeviceType> GetAll();
    }
}
