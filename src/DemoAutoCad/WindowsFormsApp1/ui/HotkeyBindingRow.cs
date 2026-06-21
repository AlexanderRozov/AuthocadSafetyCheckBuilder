using Demo.Models;
using System.Collections.Generic;

namespace Demo.ui
{
    public sealed class HotkeyBindingRow
    {
        public string Key { get; set; }
        public HotkeyListKind ListKind { get; set; }
        public string ItemId { get; set; }

        public string Summary { get; set; }
    }
}
