using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Linq;
using Pt.Models;

namespace Pt.Services.Drawing
{
    internal static class EntityEraser
    {
        public static void EraseObject(Transaction tr, PtObject obj)
        {
            EraseEntity(tr, obj.GroupId);
            if (obj.GroupId.IsNull)
            {
                EraseEntity(tr, obj.EntityId);
                EraseEntity(tr, obj.LabelTextId);
                EraseEntity(tr, obj.IdTextId);
            }

            foreach (var link in PtObjectRepository.AllLinks
                .Where(l => l.FromObjectId == obj.InstanceId || l.ToObjectId == obj.InstanceId)
                .ToList())
            {
                EraseEntity(tr, link.ArrowId);
            }
        }

        public static void EraseEntity(Transaction tr, ObjectId id) => EraseIfValid(tr, id);

        public static void EraseIfValid(Transaction tr, ObjectId id)
        {
            if (id.IsNull || id.IsErased)
                return;

            try
            {
                var ent = tr.GetObject(id, OpenMode.ForWrite, false);
                if (ent != null && !ent.IsErased)
                    ent.Erase();
            }
            catch (Autodesk.AutoCAD.Runtime.Exception)
            {
                // entity may already be gone
            }
        }
    }
}
