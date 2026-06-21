using Demo.Abstractions;
using Demo.Models;
using Demo.Services.Hotkeys;
using Demo.Services.Infrastructure;

namespace Demo.Services
{
    public static class HotkeyServices
    {
        private static readonly IHotkeyConfigRepository ConfigRepositoryInstance = new HotkeyConfigFileRepository();
        private static readonly IHotkeyBindableCatalog BindableCatalogInstance = new HotkeyBindableCatalog();
        private static readonly HotkeyInputController InputControllerInstance =
            new HotkeyInputController(ConfigRepositoryInstance, BindableCatalogInstance);

        public static IHotkeyConfigRepository ConfigRepository => ConfigRepositoryInstance;
        public static IHotkeyBindableCatalog BindableCatalog => BindableCatalogInstance;
        public static IHotkeyInputController InputController => InputControllerInstance;

        public static HotkeySelection PendingSelection { get; private set; }

        public static void RememberSelection(HotkeySelection selection) =>
            PendingSelection = selection;

        public static event System.Action<HotkeySelection> SelectionApplied;

        public static void NotifySelection(HotkeySelection selection)
        {
            PendingSelection = selection;
            SelectionApplied?.Invoke(selection);
        }
    }
}
