using Demo.Models;

namespace Demo.Abstractions
{
    public interface IBlockCatalog
    {
        System.Collections.Generic.IReadOnlyList<BlockTemplate> GetAll();
        BlockTemplate GetById(string id);
    }
}
