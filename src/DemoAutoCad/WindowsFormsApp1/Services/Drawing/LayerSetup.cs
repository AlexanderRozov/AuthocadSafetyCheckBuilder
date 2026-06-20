using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace Demo.Services.Drawing
{
    internal static class LayerSetup
    {
        public static void EnsureLayers(Transaction tr, Database db)
        {
            EnsureLayer(tr, db, PtLayoutConstants.LayerTable, 7);
            EnsureLayer(tr, db, PtLayoutConstants.LayerBus, 1);
            EnsureLayer(tr, db, PtLayoutConstants.LayerDevices, 7);
            EnsureLayer(tr, db, PtLayoutConstants.LayerText, 7);
            EnsureLayer(tr, db, PtLayoutConstants.LayerLinks, 3);
            EnsureLayer(tr, db, PtLayoutConstants.LayerDetectorZone, 8);
        }

        private static void EnsureLayer(Transaction tr, Database db, string name, short colorIndex)
        {
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForWrite);
            if (lt.Has(name))
                return;

            var layer = new LayerTableRecord
            {
                Name = name,
                Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex)
            };
            lt.Add(layer);
            tr.AddNewlyCreatedDBObject(layer, true);
        }
    }
}
