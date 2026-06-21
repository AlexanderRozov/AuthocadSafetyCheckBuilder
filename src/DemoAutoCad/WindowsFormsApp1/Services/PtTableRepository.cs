using System;
using System.Collections.Generic;
using Pt.Abstractions;
using Pt.Models;
using Pt.Services.Infrastructure;

namespace Pt.Services
{
    public static class PtTableRepository
    {
        private static IPtTableRepository Impl => PtServiceRegistry.Current.Tables;

        public static IReadOnlyList<PtTableSession> All => Impl.All;

        public static PtTableSession ActiveTable => Impl.ActiveTable;

        public static void SetActive(Guid tableId) => Impl.SetActive(tableId);

        public static PtTableSession Create(string name) => Impl.Create(name);

        public static PtTableSession Get(Guid id) => Impl.Get(id);
    }
}
