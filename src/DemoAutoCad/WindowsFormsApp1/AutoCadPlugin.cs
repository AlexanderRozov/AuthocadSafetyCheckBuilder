using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Demo.Services;

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
        }

        public void Terminate()
        {
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
