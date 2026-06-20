using Demo.Abstractions;
using Demo.Models;
using Demo.Services.Infrastructure;
using System.Collections.Generic;

namespace Demo.Services
{
    public static class BlockCatalog
    {
        private static IBlockCatalog Impl => PtServiceRegistry.BlockCatalog;

        public static List<BlockTemplate> GetAll() => new List<BlockTemplate>(Impl.GetAll());

        public static BlockTemplate GetById(string id) => Impl.GetById(id);
    }
}
