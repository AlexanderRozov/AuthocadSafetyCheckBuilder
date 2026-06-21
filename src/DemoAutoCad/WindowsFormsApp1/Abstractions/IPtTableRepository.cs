using System;
using System.Collections.Generic;
using Pt.Models;

namespace Pt.Abstractions
{
    public interface IPtTableRepository
    {
        IReadOnlyList<PtTableSession> All { get; }
        PtTableSession ActiveTable { get; }
        void SetActive(Guid tableId);
        PtTableSession Create(string name);
        PtTableSession Get(Guid id);
    }
}
