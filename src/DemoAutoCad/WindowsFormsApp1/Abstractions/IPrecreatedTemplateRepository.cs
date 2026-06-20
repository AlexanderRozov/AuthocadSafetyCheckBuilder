using Demo.Models;

namespace Demo.Abstractions
{
    public interface IPrecreatedTemplateRepository
    {
        System.Collections.Generic.IReadOnlyList<PrecreatedTemplate> All { get; }
        PrecreatedTemplate Get(string id);
        BlockTemplate GetBlockTemplate(string id);
        PrecreatedTemplate Add(PrecreatedTemplate template);
        void Remove(string id);
    }
}
