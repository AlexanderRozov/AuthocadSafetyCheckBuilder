using System;
using System.Collections.Generic;
using Pt.Abstractions;
using Pt.Models;
using Pt.Services.Infrastructure;

namespace Pt.Services
{
    public static class PtBlockRepository
    {
        private static IPtBlockRepository Impl => PtServiceRegistry.Current.Blocks;

        public static IReadOnlyList<PtObjectBlock> All => Impl.All;

        public static IEnumerable<PtObjectBlock> GetByTable(Guid tableId) => Impl.GetByTable(tableId);

        public static PtObjectBlock Get(Guid id) => Impl.Get(id);

        public static PtObjectBlock Create(Guid tableId, string name) => Impl.Create(tableId, name);

        public static void Remove(Guid blockId) => Impl.Remove(blockId);
    }
}
