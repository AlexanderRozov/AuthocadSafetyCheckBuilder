using System;
using System.Collections.Generic;
using Pt.Models;

namespace Pt.Abstractions
{
    public interface IPtBlockRepository
    {
        IReadOnlyList<PtObjectBlock> All { get; }
        IEnumerable<PtObjectBlock> GetByTable(Guid tableId);
        PtObjectBlock Get(Guid id);
        PtObjectBlock Create(Guid tableId, string name);
        void Remove(Guid blockId);
    }
}
