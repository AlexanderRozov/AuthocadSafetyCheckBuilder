using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services
{
    public static class PtBlockRepository
    {
        private static readonly List<PtObjectBlock> Blocks = new List<PtObjectBlock>();
        private static int _counter;

        public static IReadOnlyList<PtObjectBlock> All => Blocks;

        public static IEnumerable<PtObjectBlock> GetByTable(Guid tableId) =>
            Blocks.Where(b => b.TableId == tableId);

        public static PtObjectBlock Get(Guid id) =>
            Blocks.FirstOrDefault(b => b.Id == id);

        public static PtObjectBlock Create(Guid tableId, string name)
        {
            _counter++;
            var block = new PtObjectBlock
            {
                Id = Guid.NewGuid(),
                TableId = tableId,
                Name = string.IsNullOrWhiteSpace(name) ? $"Блок {_counter}" : name.Trim()
            };
            Blocks.Add(block);
            return block;
        }

        public static void Remove(Guid blockId)
        {
            Blocks.RemoveAll(b => b.Id == blockId);
            foreach (var obj in PtObjectRepository.All.Where(o => o.BlockGroupId == blockId))
                obj.BlockGroupId = null;
        }
    }
}
