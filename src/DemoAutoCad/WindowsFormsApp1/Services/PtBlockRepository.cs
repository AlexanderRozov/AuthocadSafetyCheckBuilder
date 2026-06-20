using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services
{
    public static class PtBlockRepository
    {
        private static PtDocumentState State => PtDocumentRegistry.Current;

        public static IReadOnlyList<PtObjectBlock> All => State.Blocks;

        public static IEnumerable<PtObjectBlock> GetByTable(Guid tableId) =>
            State.Blocks.Where(b => b.TableId == tableId);

        public static PtObjectBlock Get(Guid id) =>
            State.Blocks.FirstOrDefault(b => b.Id == id);

        public static PtObjectBlock Create(Guid tableId, string name)
        {
            State.BlockCounter++;
            var block = new PtObjectBlock
            {
                Id = Guid.NewGuid(),
                TableId = tableId,
                Name = string.IsNullOrWhiteSpace(name) ? $"Блок {State.BlockCounter}" : name.Trim()
            };
            State.Blocks.Add(block);
            return block;
        }

        public static void Remove(Guid blockId)
        {
            State.Blocks.RemoveAll(b => b.Id == blockId);
            foreach (var obj in State.Objects.Where(o => o.BlockGroupId == blockId))
                obj.BlockGroupId = null;
        }
    }
}
