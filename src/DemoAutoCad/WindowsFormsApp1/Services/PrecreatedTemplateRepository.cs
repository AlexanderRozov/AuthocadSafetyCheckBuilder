using System.Collections.Generic;
using Pt.Abstractions;
using Pt.Models;
using Pt.Services.Infrastructure;

namespace Pt.Services
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
