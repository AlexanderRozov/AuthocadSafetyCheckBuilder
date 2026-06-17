using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using Demo.Models;
using Demo.Services;
using Demo.ui;
using System.Windows.Forms;

namespace AutoCadPlugin.Commands
{
    public class PtCommands
    {
        [CommandMethod("PLACEPT")]
        public void PlacePt()
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            PlaceDeviceRequest request;
            using (var form = new ContextForm())
            {
                if (form.ShowDialog() != DialogResult.OK)
                    return;

                request = form.SelectedDevice;
            }

            if (request == null)
                return;

            var ptObject = PtLayoutManager.AddDevice(doc.Database, request);

            doc.Editor.WriteMessage(
                $"\nОбъект {ptObject.Label} добавлен в колонку {ptObject.ColumnIndex}.");
        }
    }
}
