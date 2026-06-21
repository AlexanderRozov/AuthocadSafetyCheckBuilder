using System;
using System.Collections.Generic;
using Pt.Abstractions;
using Pt.Models;
using Pt.Services.Infrastructure;

namespace Pt.Services
{
    public static class PtObjectRepository
    {
        private static IPtObjectRepository Impl => PtServiceRegistry.Current.Objects;

        public static IReadOnlyList<PtObject> All => Impl.All;

        public static IReadOnlyList<PtObjectLink> AllLinks => Impl.AllLinks;

        public static IEnumerable<PtObject> GetByTable(Guid tableId) => Impl.GetByTable(tableId);

        public static PtObject Get(Guid id) => Impl.Get(id);

        public static string GetNextNumber(string code) => Impl.GetNextNumber(code);

        public static string PeekNextNumber(string code) => Impl.PeekNextNumber(code);

        public static void Add(PtObject ptObject) => Impl.Add(ptObject);

        public static PtObjectLink AddLink(Guid tableId, Guid fromId, Guid toId) =>
            Impl.AddLink(tableId, fromId, toId);

        public static void RemoveIncomingLinks(Guid toObjectId) =>
            Impl.RemoveIncomingLinks(toObjectId);

        public static bool WouldCreateParentCycle(Guid fromId, Guid toId) =>
            Impl.WouldCreateParentCycle(fromId, toId);

        public static void RemoveLinksForObject(Guid objectId) =>
            Impl.RemoveLinksForObject(objectId);

        public static void Remove(PtObject obj) => Impl.Remove(obj);

        public static void ClearParentReference(Guid parentId) =>
            Impl.ClearParentReference(parentId);

        public static void SetColumnOrder(Guid tableId, IReadOnlyList<Guid> orderedIds) =>
            Impl.SetColumnOrder(tableId, orderedIds);

        public static string GetBlockName(PtObject obj) => Impl.GetBlockName(obj);
    }
}
