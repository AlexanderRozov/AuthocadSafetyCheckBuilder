using Demo.Models;
using System;
using System.Collections.Generic;

namespace Demo.Abstractions
{
    public interface IPtObjectRepository
    {
        IReadOnlyList<PtObject> All { get; }
        IReadOnlyList<PtObjectLink> AllLinks { get; }
        IEnumerable<PtObject> GetByTable(Guid tableId);
        PtObject Get(Guid id);
        string GetNextNumber(string code);
        string PeekNextNumber(string code);
        void Add(PtObject ptObject);
        PtObjectLink AddLink(Guid tableId, Guid fromId, Guid toId);
        void RemoveIncomingLinks(Guid toObjectId);
        bool WouldCreateParentCycle(Guid fromId, Guid toId);
        void RemoveLinksForObject(Guid objectId);
        void Remove(PtObject obj);
        void ClearParentReference(Guid parentId);
        void SetColumnOrder(Guid tableId, IReadOnlyList<Guid> orderedIds);
        string GetBlockName(PtObject obj);
    }
}
