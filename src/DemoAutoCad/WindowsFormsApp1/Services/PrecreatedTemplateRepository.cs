using Demo.Abstractions;
using Demo.Models;
using Demo.Services.Infrastructure;
using System.Collections.Generic;

namespace Demo.Services
{
    public static class PrecreatedTemplateRepository
    {
        private static IPrecreatedTemplateRepository Impl => PtServiceRegistry.Current.PrecreatedTemplates;

        public static IReadOnlyList<PrecreatedTemplate> All => Impl.All;

        public static PrecreatedTemplate Get(string id) => Impl.Get(id);

        public static BlockTemplate GetBlockTemplate(string id) => Impl.GetBlockTemplate(id);

        public static PrecreatedTemplate Add(PrecreatedTemplate template) => Impl.Add(template);

        public static void Remove(string id) => Impl.Remove(id);
    }
}
