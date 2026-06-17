using Demo.Models;
using System.Collections.Generic;

namespace Demo.Services
{
    public static class DeviceCatalog
    {
        public static List<DeviceType> GetAll()
        {
            return new List<DeviceType>
            {
                new DeviceType { Id = "bth", Name = "Извещатель дымовой", Code = "BTH" },
                new DeviceType { Id = "btk", Name = "Извещатель тепловой", Code = "BTK" },
                new DeviceType { Id = "bthl", Name = "Извещатель линейный тепловой", Code = "BTHL(Tr)" },
                new DeviceType { Id = "btf", Name = "Извещатель пламени", Code = "BTF" },
                new DeviceType { Id = "hssd", Name = "Извещатель аспирационный", Code = "HSSD" },
                new DeviceType { Id = "btkl", Name = "Извещатель термокабель", Code = "BTK (L)" },
                new DeviceType { Id = "btm", Name = "Извещатель ручной", Code = "BTM" },
                new DeviceType { Id = "btme", Name = "Извещатель ручной Ех", Code = "BTM (Eх)" },
                new DeviceType { Id = "ae", Name = "Адресная метка", Code = "AE" },
                new DeviceType { Id = "mr", Name = "Релейный модуль", Code = "MR" },
                new DeviceType { Id = "bz", Name = "Изолятор шлейфа адресный", Code = "BZ" },
                new DeviceType { Id = "bial", Name = "Оповещатель световой \"Выход\"", Code = "BIAL" },
                new DeviceType { Id = "biall", Name = "Оповещатель пожарный световой (лампа-вспышка)", Code = "BIAL (L)" },
                new DeviceType { Id = "bialle", Name = "Оповещатель пожарный световой (лампа-вспышка) Ех", Code = "BIAL (L Ех)" },
                new DeviceType { Id = "bias", Name = "Оповещатель пожарный звуковой", Code = "BIAS" },
                new DeviceType { Id = "biase", Name = "Оповещатель пожарный звуковой Ех", Code = "BIAS (Eх)" },
                new DeviceType { Id = "ups", Name = "Источник бесперебойного питания", Code = "UPS" },
                new DeviceType { Id = "mdu", Name = "Модуль управления противопожарным клапаном адресный", Code = "MDU" },
                new DeviceType { Id = "sib", Name = "Кнопка", Code = "SIB" },
                new DeviceType { Id = "shkaf_dv", Name = "Шкаф ДВ", Code = "Шкаф ДВ" },
                new DeviceType { Id = "shkaf_pd", Name = "Шкаф ПД", Code = "Шкаф ПД" },
                new DeviceType { Id = "shkaf_pv", Name = "Шкаф ПВ (ОВ)", Code = "Шкаф ПВ (ОВ)" },
                new DeviceType { Id = "irp", Name = "IRP", Code = "IRP" },
                new DeviceType { Id = "shkaf_skud", Name = "Шкаф СКУД", Code = "Шкаф СКУД" },
                new DeviceType { Id = "jf01", Name = "ШКАФ КСПА", Code = "JF01-CA-000Х" }
            };
        }
    }
}
