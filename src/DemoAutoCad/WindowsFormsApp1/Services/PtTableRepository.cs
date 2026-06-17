using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services
{
    public static class PtTableRepository
    {
        private static readonly List<PtTableSession> Tables = new List<PtTableSession>();
        private static Guid? _activeTableId;
        private static int _tableCounter;

        public static IReadOnlyList<PtTableSession> All => Tables;

        public static PtTableSession ActiveTable =>
            _activeTableId.HasValue
                ? Tables.FirstOrDefault(t => t.Id == _activeTableId.Value)
                : null;

        public static void SetActive(Guid tableId)
        {
            _activeTableId = tableId;
        }

        public static PtTableSession Create(string name)
        {
            _tableCounter++;
            var table = new PtTableSession
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrEmpty(name) ? $"Таблица {_tableCounter}" : name
            };
            Tables.Add(table);
            _activeTableId = table.Id;
            return table;
        }

        public static PtTableSession Get(Guid id) =>
            Tables.FirstOrDefault(t => t.Id == id);
    }
}
