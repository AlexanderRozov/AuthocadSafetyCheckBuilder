using Demo.Abstractions;
using Demo.Models;
using Demo.Services.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services.Hotkeys
{
    public sealed class HotkeyBindableCatalog : IHotkeyBindableCatalog
    {
        public IReadOnlyList<HotkeyBindableItem> GetItems(HotkeyListKind listKind)
        {
            switch (listKind)
            {
                case HotkeyListKind.Shape:
                    return BlockCatalog.GetAllIncludingPrecreated()
                        .Select(b => new HotkeyBindableItem
                        {
                            ListKind = HotkeyListKind.Shape,
                            ItemId = b.Id,
                            Code = b.Code,
                            DisplayName = b.Name
                        })
                        .OrderBy(i => i.DisplayName)
                        .ToList();
                default:
                    return DeviceCatalog.GetAll()
                        .Select(d => new HotkeyBindableItem
                        {
                            ListKind = HotkeyListKind.Device,
                            ItemId = d.Id,
                            Code = d.Code,
                            DisplayName = d.Name
                        })
                        .OrderBy(i => i.DisplayName)
                        .ToList();
            }
        }

        public HotkeyBindableItem Find(HotkeyListKind listKind, string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            return GetItems(listKind).FirstOrDefault(i => i.ItemId == itemId);
        }
    }
}
