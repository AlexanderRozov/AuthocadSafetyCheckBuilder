using Demo.Models;
using System.Collections.Generic;

namespace Demo.Services
{
    public static class BlockCatalog
    {
        public static List<BlockTemplate> GetAll()
        {
            return new List<BlockTemplate>
            {
                new BlockTemplate { Id = "rect", Name = "Прямоугольник (по умолчанию)", BlockName = null },
                new BlockTemplate { Id = "pt_bz", Name = "Блок BZ", BlockName = "PT_BZ" },
                new BlockTemplate { Id = "pt_mr", Name = "Блок MR", BlockName = "PT_MR" },
                new BlockTemplate { Id = "pt_mdu", Name = "Блок MDU", BlockName = "PT_MDU" },
                new BlockTemplate { Id = "pt_bth", Name = "Блок BTH", BlockName = "PT_BTH" },
                new BlockTemplate { Id = "pt_bial", Name = "Блок BIAL", BlockName = "PT_BIAL" }
            };
        }
    }
}
