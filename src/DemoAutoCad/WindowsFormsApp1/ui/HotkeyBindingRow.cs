using System.Collections.Generic;
using Pt.Models;

namespace Pt.ui
{
    public sealed class HotkeyBindingRow
    {
        public string Key { get; set; }
        public HotkeyListKind ListKind { get; set; }
        public string ItemId { get; set; }

        public string Summary { get; set; }
    }
}
