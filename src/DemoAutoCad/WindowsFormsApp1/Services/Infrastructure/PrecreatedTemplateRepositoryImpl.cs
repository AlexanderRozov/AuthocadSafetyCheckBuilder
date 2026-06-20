using Demo.Abstractions;
using Demo.Models;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services.Infrastructure
{
    public sealed class PrecreatedTemplateRepositoryImpl : IPrecreatedTemplateRepository
    {
        private readonly PtDocumentState _state;

        public PrecreatedTemplateRepositoryImpl(PtDocumentState state)
        {
            _state = state;
        }

        public IReadOnlyList<PrecreatedTemplate> All => _state.PrecreatedTemplates;

        public PrecreatedTemplate Get(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return _state.PrecreatedTemplates.FirstOrDefault(t => t.Id == id);
        }

        public BlockTemplate GetBlockTemplate(string id) => Get(id)?.ToBlockTemplate();

        public PrecreatedTemplate Add(PrecreatedTemplate template)
        {
            if (template == null)
                return null;

            _state.PrecreatedTemplates.Add(template);
            return template;
        }

        public void Remove(string id)
        {
            var item = Get(id);
            if (item != null)
                _state.PrecreatedTemplates.Remove(item);
        }
    }
}
