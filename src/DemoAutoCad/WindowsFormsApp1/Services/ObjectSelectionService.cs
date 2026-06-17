using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services
{
    public static class ObjectSelectionService
    {
        private static PtObject _pendingActivate;

        public static Point3d? PickInsertionPoint(Editor ed)
        {
            var opts = new PromptPointOptions("\nЩёлкните точку вставки объекта (позиция курсора):");
            var picked = ed.GetPoint(opts);
            return picked.Status == PromptStatus.OK ? picked.Value : (Point3d?)null;
        }

        public static void ActivateOnDrawing(PtObject obj)
        {
            if (obj == null)
                return;

            _pendingActivate = obj;
            Application.Idle -= OnIdleActivate;
            Application.Idle += OnIdleActivate;
        }

        private static void OnIdleActivate(object sender, EventArgs e)
        {
            Application.Idle -= OnIdleActivate;

            var obj = _pendingActivate;
            _pendingActivate = null;
            if (obj == null)
                return;

            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            try
            {
                DocumentLockHelper.Run((lockedDoc, db) =>
                {
                    var ed = lockedDoc.Editor;
                    var ids = ResolveValidEntityIds(db, obj);
                    if (ids.Length == 0)
                        return;

                    try
                    {
                        ed.SetImpliedSelection(ids);
                        ed.UpdateScreen();
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception)
                    {
                        // ignore invalid selection
                    }
                });
            }
            catch
            {
                // best-effort
            }
        }

        public static void ClearSelection()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            try
            {
                doc.Editor.SetImpliedSelection(new ObjectId[0]);
            }
            catch
            {
                // ignore
            }
        }

        private static ObjectId[] ResolveValidEntityIds(Database db, PtObject obj)
        {
            var candidates = new List<ObjectId>();

            if (!obj.EntityId.IsNull)
                candidates.Add(obj.EntityId);
            if (!obj.LabelTextId.IsNull)
                candidates.Add(obj.LabelTextId);
            if (!obj.IdTextId.IsNull)
                candidates.Add(obj.IdTextId);

            var valid = new List<ObjectId>();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                foreach (var id in candidates.Distinct())
                {
                    if (IsValidEntity(tr, id))
                        valid.Add(id);
                }

                tr.Commit();
            }

            return valid.ToArray();
        }

        private static bool IsValidEntity(Transaction tr, ObjectId id)
        {
            if (id.IsNull || id.IsErased)
                return false;

            var ent = tr.GetObject(id, OpenMode.ForRead, false) as Entity;
            return ent != null && !ent.IsErased;
        }

        public static void SyncCenterFromDrawing(Database db, PtObject obj, Transaction tr = null)
        {
            if (obj == null || obj.EntityId.IsNull)
                return;

            var ownsTransaction = tr == null;

            if (ownsTransaction)
                tr = db.TransactionManager.StartTransaction();

            try
            {
                var ent = tr.GetObject(obj.EntityId, OpenMode.ForRead, false) as Entity;
                if (ent != null && !ent.IsErased)
                {
                    try
                    {
                        var ext = ent.GeometricExtents;
                        obj.Center = new Point3d(
                            (ext.MinPoint.X + ext.MaxPoint.X) / 2,
                            (ext.MinPoint.Y + ext.MaxPoint.Y) / 2,
                            0);
                    }
                    catch (Autodesk.AutoCAD.Runtime.Exception)
                    {
                        // ignore entities without extents
                    }
                }

                if (ownsTransaction)
                    tr.Commit();
            }
            finally
            {
                if (ownsTransaction && tr != null)
                    tr.Dispose();
            }
        }
    }
}
