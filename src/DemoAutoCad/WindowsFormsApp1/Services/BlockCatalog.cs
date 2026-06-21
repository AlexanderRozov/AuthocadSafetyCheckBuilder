using System.Collections.Generic;
using Pt.Abstractions;
using Pt.Models;
using Pt.Services.Infrastructure;

namespace Pt.Services
{
    public static class BlockCatalog
    {
        private static IBlockCatalog Impl => PtServiceRegistry.BlockCatalog;

        public static List<BlockTemplate> GetAll() => [.. Impl.GetAll()];

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
