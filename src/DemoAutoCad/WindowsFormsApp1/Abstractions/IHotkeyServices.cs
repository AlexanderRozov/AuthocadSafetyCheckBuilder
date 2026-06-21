using Pt.Models;

namespace Pt.Abstractions
{
    public interface IHotkeyConfigRepository
    {
        HotkeyUserConfig Load();
        void Save(HotkeyUserConfig config);
        string ConfigFilePath { get; }
    }

    public interface IHotkeyBindableCatalog
    {
        System.Collections.Generic.IReadOnlyList<HotkeyBindableItem> GetItems(HotkeyListKind listKind);
        HotkeyBindableItem Find(HotkeyListKind listKind, string itemId);
    }

    public interface IHotkeyInputController
    {
        bool IsArmed { get; }
        void ReloadConfig();
        void Attach();
        void Detach();
        bool TryProcessKey(System.Windows.Forms.Keys key);
        event System.Action<HotkeySelection> SelectionApplied;
        event System.Action<bool> ArmedStateChanged;
    }
}
