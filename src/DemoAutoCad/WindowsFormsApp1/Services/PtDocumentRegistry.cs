using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;

namespace Pt.Services
{
    public static class PtDocumentRegistry
    {
        private static readonly Dictionary<string, PtDocumentState> States =
            new Dictionary<string, PtDocumentState>(StringComparer.OrdinalIgnoreCase);

        public static event Action<Document> DocumentDataLoaded;

        public static PtDocumentState Current
        {
            get
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                return doc != null ? Get(doc) : new PtDocumentState();
            }
        }

        public static PtDocumentState Get(Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var key = GetKey(doc);
            if (!States.TryGetValue(key, out var state))
            {
                state = new PtDocumentState();
                States[key] = state;
            }

            return state;
        }

        public static PtDocumentState GetByDatabase(Database db)
        {
            var doc = FindDocument(db);
            return doc != null ? Get(doc) : Current;
        }

        public static void EnsureLoaded(Document doc)
        {
            if (doc == null)
                return;

            var state = Get(doc);
            if (state.IsHydrated)
                return;

            DocumentLockHelper.Run((lockedDoc, db) =>
            {
                PtPersistenceService.Load(db, state);
            });

            state.IsHydrated = true;
            DocumentDataLoaded?.Invoke(doc);
        }

        public static void Save(Document doc)
        {
            if (doc == null)
                return;

            var state = Get(doc);
            DocumentLockHelper.Run((lockedDoc, db) =>
            {
                PtPersistenceService.Save(db, state);
            });
        }

        public static void Save(Database db)
        {
            var doc = FindDocument(db);
            if (doc != null)
                Save(doc);
            else
                DocumentLockHelper.Run((_, database) => PtPersistenceService.Save(database, Current));
        }

        public static void Remove(Document doc)
        {
            if (doc == null)
                return;

            States.Remove(GetKey(doc));
        }

        public static Document FindDocument(Database db)
        {
            if (db == null)
                return null;

            foreach (Document doc in Application.DocumentManager)
            {
                if (doc.Database == db)
                    return doc;
            }

            return null;
        }

        private static string GetKey(Document doc)
        {
            var db = doc.Database;
            var name = db.Filename;
            if (string.IsNullOrEmpty(name))
                name = db.OriginalFileName;
            if (string.IsNullOrEmpty(name))
                return $"unsaved_{doc.GetHashCode()}";

            return name;
        }
    }
}
