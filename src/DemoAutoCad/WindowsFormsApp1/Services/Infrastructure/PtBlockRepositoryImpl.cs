using Demo.Abstractions;
using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services.Infrastructure
{
    public sealed class PtBlockRepositoryImpl : IPtBlockRepository
    {
        private readonly PtDocumentState _state;

        public PtBlockRepositoryImpl(PtDocumentState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public IReadOnlyList<PtObjectBlock> All => _state.Blocks;

        public IEnumerable<PtObjectBlock> GetByTable(Guid tableId) =>
            _state.Blocks.Where(b => b.TableId == tableId);

        public PtObjectBlock Get(Guid id) =>
            _state.Blocks.FirstOrDefault(b => b.Id == id);

        public PtObjectBlock Create(Guid tableId, string name)
        {
            _state.BlockCounter++;
            var block = new PtObjectBlock
            {
                Id = Guid.NewGuid(),
                TableId = tableId,
                Name = string.IsNullOrWhiteSpace(name) ? $"Блок {_state.BlockCounter}" : name.Trim()
            };
            _state.Blocks.Add(block);
            return block;
        }

        public void Remove(Guid blockId)
        {
            _state.Blocks.RemoveAll(b => b.Id == blockId);
            foreach (var obj in _state.Objects.Where(o => o.BlockGroupId == blockId))
                obj.BlockGroupId = null;
        }
    }
}
