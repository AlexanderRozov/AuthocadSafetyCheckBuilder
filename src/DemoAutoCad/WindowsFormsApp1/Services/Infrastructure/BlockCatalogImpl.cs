using System.Collections.Generic;
using System.Linq;
using Pt.Abstractions;
using Pt.Models;

namespace Pt.Services.Infrastructure
{
    public sealed class BlockCatalogImpl : IBlockCatalog
    {
        private static readonly IReadOnlyList<BlockTemplate> Items = new List<BlockTemplate>
        {
            new BlockTemplate { Id = "rect", Name = "Прямоугольник", ShapeType = DeviceShapeType.Rectangle },
            new BlockTemplate { Id = "square", Name = "Квадрат", ShapeType = DeviceShapeType.Square },
            new BlockTemplate { Id = "triangle", Name = "Треугольник", ShapeType = DeviceShapeType.Triangle },
            new BlockTemplate { Id = "circle", Name = "Круг", ShapeType = DeviceShapeType.Circle },
            new BlockTemplate { Id = "diamond", Name = "Ромб", ShapeType = DeviceShapeType.Diamond },
            new BlockTemplate { Id = "pt_bz", Name = "Блок BZ", BlockName = "PT_BZ", ShapeType = DeviceShapeType.AutoCadBlock },
            new BlockTemplate { Id = "pt_mr", Name = "Блок MR", BlockName = "PT_MR", ShapeType = DeviceShapeType.AutoCadBlock },
            new BlockTemplate { Id = "pt_mdu", Name = "Блок MDU", BlockName = "PT_MDU", ShapeType = DeviceShapeType.AutoCadBlock },
            new BlockTemplate { Id = "pt_bth", Name = "Блок BTH", BlockName = "PT_BTH", ShapeType = DeviceShapeType.AutoCadBlock },
            new BlockTemplate { Id = "pt_bial", Name = "Блок BIAL", BlockName = "PT_BIAL", ShapeType = DeviceShapeType.AutoCadBlock }
        };

        public IReadOnlyList<BlockTemplate> GetAll() => Items;

        public BlockTemplate GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return Items.FirstOrDefault(item => item.Id == id);
        }
    }
}
