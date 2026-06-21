using Autodesk.AutoCAD.DatabaseServices;

namespace Pt.Services
{
    public static class HandleHelper
    {
        public static long? ToHandle(Transaction tr, ObjectId id)
        {
            if (id.IsNull)
                return null;

            try
            {
                var obj = tr.GetObject(id, OpenMode.ForRead, false);
                if (obj == null || obj.IsErased)
                    return null;

                return obj.Handle.Value;
            }
            catch
            {
                return null;
            }
        }

        public static ObjectId FromHandle(Database db, long? handle)
        {
            if (!handle.HasValue || handle.Value == 0)
                return ObjectId.Null;

            try
            {
                var id = db.GetObjectId(false, new Handle(handle.Value), 0);
                return id.IsNull ? ObjectId.Null : id;
            }
            catch
            {
                return ObjectId.Null;
            }
        }

        public static bool IsValid(Transaction tr, ObjectId id)
        {
            if (id.IsNull || id.IsErased)
                return false;

            try
            {
                var obj = tr.GetObject(id, OpenMode.ForRead, false);
                return obj != null && !obj.IsErased;
            }
            catch
            {
                return false;
            }
        }
    }
}
