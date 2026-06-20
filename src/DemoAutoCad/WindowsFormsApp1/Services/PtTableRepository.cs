using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services
{
    public static class PtTableRepository
    {
        private static PtDocumentState State => PtDocumentRegistry.Current;

        public static IReadOnlyList<PtTableSession> All => State.Tables;

        public static PtTableSession ActiveTable =>
            State.ActiveTableId.HasValue
                ? State.Tables.FirstOrDefault(t => t.Id == State.ActiveTableId.Value)
                : null;

        public static void SetActive(Guid tableId)
        {
            State.ActiveTableId = tableId;
        }

        public static PtTableSession Create(string name)
        {
            State.TableCounter++;
            var table = new PtTableSession
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrEmpty(name) ? $"Таблица {State.TableCounter}" : name
            };
            State.Tables.Add(table);
            State.ActiveTableId = table.Id;
            return table;
        }

        public static PtTableSession Get(Guid id) =>
            State.Tables.FirstOrDefault(t => t.Id == id);
    }
}
