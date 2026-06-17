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
        }

        public static BlockTemplate GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            foreach (var item in GetAll())
            {
                if (item.Id == id)
                    return item;
            }

            return null;
        }
    }
}
