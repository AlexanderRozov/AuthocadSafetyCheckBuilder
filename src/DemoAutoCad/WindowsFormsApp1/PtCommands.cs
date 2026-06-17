using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Demo.ui;

namespace AutoCadPlugin.Commands
{
    public class PtCommands
    {
        private static PtMainForm _form;

        [CommandMethod("PLACEPT")]
        public void PlacePt()
        {
            ShowMainForm();
        }

        [CommandMethod("PTPANEL")]
        public void ShowPanel()
        {
            ShowMainForm();
        }

        private static void ShowMainForm()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            if (_form == null || _form.IsDisposed)
            {
                _form = new PtMainForm();
                Application.ShowModelessDialog(_form);
            }
            else
            {
                _form.Show();
                _form.BringToFront();
            }
        }
    }
}
