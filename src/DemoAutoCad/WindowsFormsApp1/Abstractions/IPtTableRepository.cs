using Demo.Models;
using System;
using System.Collections.Generic;

namespace Demo.Abstractions
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
