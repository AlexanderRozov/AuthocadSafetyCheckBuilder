using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Demo.Services;
using Demo.ui;

[assembly: ExtensionApplication(typeof(AutoCadPlugin.PluginEntry))]
[assembly: CommandClass(typeof(AutoCadPlugin.Commands.PtCommands))]

namespace AutoCadPlugin
{
    public class PluginEntry : IExtensionApplication
    {
        public void Initialize()
        {
            UI.ContextMenuManager.Initialize();

            Application.DocumentManager.DocumentActivated += OnDocumentActivated;
            Application.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
                PtDocumentRegistry.EnsureLoaded(doc);

            HotkeyServices.InputController.SelectionApplied += HotkeyServices.NotifySelection;
            HotkeyServices.InputController.Attach();

            HotkeyArmedIndicator.ShowHandler = (message, timeout) =>
                HotkeyArmedIndicatorWindow.ShowIndicator(message, timeout);
            HotkeyArmedIndicator.HideHandler = HotkeyArmedIndicatorWindow.HideIndicator;
        }

        public void Terminate()
        {
            HotkeyArmedIndicator.ShowHandler = null;
            HotkeyArmedIndicator.HideHandler = null;
            HotkeyArmedIndicator.HideIndicator();

            HotkeyServices.InputController.SelectionApplied -= HotkeyServices.NotifySelection;
            HotkeyServices.InputController.Detach();

            Application.DocumentManager.DocumentActivated -= OnDocumentActivated;
            Application.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
        }

        private static void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
        {
            if (e?.Document == null)
                return;

            PtDocumentRegistry.EnsureLoaded(e.Document);
        }

        private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            if (e?.Document == null)
                return;

            try
            {
                PtDocumentRegistry.Save(e.Document);
            }
            catch
            {
                // best-effort on close
            }

            PtDocumentRegistry.Remove(e.Document);
        }
    }
}
