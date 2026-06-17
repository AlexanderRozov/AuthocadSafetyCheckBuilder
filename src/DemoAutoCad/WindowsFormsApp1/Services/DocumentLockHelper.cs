using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace Demo.Services
{
    public static class DocumentLockHelper
    {
        public static void Run(Action<Document, Database> action)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            using (doc.LockDocument())
                action(doc, doc.Database);
        }

        public static T Run<T>(Func<Document, Database, T> func)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return default;

            using (doc.LockDocument())
                return func(doc, doc.Database);
        }
    }
}
