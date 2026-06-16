using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AutoCadPlugin.Commands
{
    public class PtCommands
    {
        [CommandMethod("PLACEPT")]
        public void PlacePt()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var result = ed.GetPoint(
                "\nУкажите точку вставки:");

            if (result.Status != PromptStatus.OK)
                return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)
                    tr.GetObject(
                        db.BlockTableId,
                        OpenMode.ForRead);

                var ms = (BlockTableRecord)
                    tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite);

                var circle = new Circle(
                    result.Value,
                    Vector3d.ZAxis,
                    10);

                ms.AppendEntity(circle);
                tr.AddNewlyCreatedDBObject(circle, true);

                tr.Commit();
            }
        }
    }
}