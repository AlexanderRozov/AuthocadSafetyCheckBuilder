using Pt.Models;

namespace Pt.Abstractions
{
    public interface IBlockCatalog
    {
        System.Collections.Generic.IReadOnlyList<BlockTemplate> GetAll();
        BlockTemplate GetById(string id);
    }
}
