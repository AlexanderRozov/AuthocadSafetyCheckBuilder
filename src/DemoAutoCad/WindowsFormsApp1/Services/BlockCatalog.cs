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

        public static BlockTemplate GetById(string id)
        {
            var builtIn = Impl.GetById(id);
            if (builtIn != null)
                return builtIn;

            return PrecreatedTemplateRepository.GetBlockTemplate(id);
        }

        public static List<BlockTemplate> GetAllIncludingPrecreated()
        {
            var items = new List<BlockTemplate>(Impl.GetAll());
            foreach (var template in PrecreatedTemplateRepository.All)
                items.Add(template.ToBlockTemplate());

            return items;
        }
    }
}
