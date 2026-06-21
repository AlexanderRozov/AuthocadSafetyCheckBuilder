namespace Demo.Models
{
    public enum HotkeyListKind
    {
        Device,
        Shape
    }

    public sealed class HotkeyUserConfig
    {
        public bool Enabled { get; set; } = true;
        public string ModeTriggerKey { get; set; } = "F11";
        public int TimeoutSeconds { get; set; } = 8;
        public System.Collections.Generic.List<HotkeyBindingEntry> Bindings { get; set; }
            = new System.Collections.Generic.List<HotkeyBindingEntry>();
    }

    public sealed class HotkeyBindingEntry
    {
        public string Key { get; set; }
        public string ListKind { get; set; }
        public string ItemId { get; set; }
    }

    public sealed class HotkeyBindableItem
    {
        public HotkeyListKind ListKind { get; set; }
        public string ItemId { get; set; }
        public string Code { get; set; }
        public string DisplayName { get; set; }

        public string CategoryLabel => ListKind == HotkeyListKind.Device ? "Тип объекта" : "Форма";

        public string DisplayLabel => string.IsNullOrWhiteSpace(Code)
            ? DisplayName
            : $"{Code} — {DisplayName}";
    }

    public sealed class HotkeySelection
    {
        public HotkeySelection(HotkeyListKind listKind, string itemId)
        {
            ListKind = listKind;
            ItemId = itemId;
        }

        public HotkeyListKind ListKind { get; }
        public string ItemId { get; }
    }
}
