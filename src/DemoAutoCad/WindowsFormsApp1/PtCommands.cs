using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Demo.Services;
using Demo.ui;

namespace AutoCadPlugin.Commands
{
    public class PtCommands
    {
        private static PtMainWindow _window;

        [CommandMethod("PLACEPT")]
        public void PlacePt()
        {
            ShowMainWindow();
        }

        [CommandMethod("PTPANEL")]
        public void ShowPanel()
        {
            ShowMainWindow();
        }

        private static void ShowMainWindow()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            PtDocumentRegistry.EnsureLoaded(doc);

            if (_window == null)
            {
                _window = new PtMainWindow();
                Application.ShowModelessWindow(_window);
            }
            else
            {
                _window.ReloadFromDocument();
                _window.Show();
            }
        }
    }
}
