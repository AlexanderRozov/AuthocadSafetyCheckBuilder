using Demo.Abstractions;
using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services.Infrastructure
{
    public sealed class PtTableRepositoryImpl : IPtTableRepository
    {
        private readonly PtDocumentState _state;

        public PtTableRepositoryImpl(PtDocumentState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public IReadOnlyList<PtTableSession> All => _state.Tables;

        public PtTableSession ActiveTable =>
            _state.ActiveTableId.HasValue
                ? _state.Tables.FirstOrDefault(t => t.Id == _state.ActiveTableId.Value)
                : null;

        public void SetActive(Guid tableId) => _state.ActiveTableId = tableId;

        public PtTableSession Create(string name)
        {
            _state.TableCounter++;
            var table = new PtTableSession
            {
                Id = Guid.NewGuid(),
                Name = string.IsNullOrEmpty(name) ? $"Таблица {_state.TableCounter}" : name
            };
            _state.Tables.Add(table);
            _state.ActiveTableId = table.Id;
            return table;
        }

        public PtTableSession Get(Guid id) =>
            _state.Tables.FirstOrDefault(t => t.Id == id);
    }
}
