using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Pt.Models;

namespace Pt.Services
{
    public static class PrecreatedBlockService
    {
        private const string BlockPrefix = "PT_PRE_";

        public static PrecreatedTemplate CreateFromSelection(
            Database db,
            Editor editor,
            string displayName)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Укажите имя шаблона.");

            var selection = editor.GetSelection(new PromptSelectionOptions
            {
                MessageForAdding = "\nВыберите объекты для шаблона: "
            });

            if (selection.Status != PromptStatus.OK || selection.Value.Count == 0)
                return null;

            var basePoint = editor.GetPoint("\nУкажите базовую точку вставки шаблона:");
            if (basePoint.Status != PromptStatus.OK)
                return null;

            return CreateBlockFromEntities(
                db,
                selection.Value.GetObjectIds(),
                basePoint.Value,
                displayName.Trim(),
                null,
                null,
                null);
        }

        public static PrecreatedTemplate RegisterExistingBlock(
            Database db,
            string blockName,
            string name,
            string code,
            string description)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));
            if (string.IsNullOrWhiteSpace(blockName))
                throw new ArgumentException("Укажите имя блока.");

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                    throw new InvalidOperationException($"Блок «{blockName}» не найден в чертеже.");

                var halfHeight = ComputeBlockHalfHeight(tr, bt[blockName]);
                tr.Commit();

                return RegisterTemplate(
                    db,
                    name,
                    code,
                    description,
                    blockName,
                    null,
                    halfHeight);
            }
        }

        public static List<string> ListBlockNames(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return new List<string>();

            using (var sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile(filePath, FileShare.Read, true, null);

                using (var tr = sourceDb.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                    var names = new List<string>();

                    foreach (ObjectId id in bt)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(id, OpenMode.ForRead);
                        if (btr.IsAnonymous || btr.IsLayout)
                            continue;

                        var name = btr.Name;
                        if (string.IsNullOrEmpty(name) || name.StartsWith("*", StringComparison.Ordinal))
                            continue;

                        names.Add(name);
                    }

                    tr.Commit();
                    return names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
                }
            }
        }

        public static PrecreatedTemplate ImportFromDrawing(
            Database targetDb,
            string filePath,
            string sourceBlockName,
            string displayName)
        {
            return ImportFromDrawing(targetDb, filePath, sourceBlockName, displayName, null, null);
        }

        public static PrecreatedTemplate ImportFromDrawing(
            Database targetDb,
            string filePath,
            string sourceBlockName,
            string displayName,
            string code,
            string description)
        {
            if (targetDb == null)
                throw new ArgumentNullException(nameof(targetDb));
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("Файл чертежа не найден.", filePath);
            if (string.IsNullOrWhiteSpace(sourceBlockName))
                throw new ArgumentException("Укажите имя блока в исходном файле.");
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Укажите имя шаблона.");

            using (var sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile(filePath, FileShare.Read, true, null);

                ObjectId sourceBlockId;
                using (var tr = sourceDb.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(sourceDb.BlockTableId, OpenMode.ForRead);
                    if (!bt.Has(sourceBlockName))
                        throw new InvalidOperationException($"Блок «{sourceBlockName}» не найден в файле.");

                    sourceBlockId = bt[sourceBlockName];
                    tr.Commit();
                }

                var targetBlockName = CreateUniqueBlockName(targetDb, displayName);

                ObjectId importedBlockId;
                using (var tr = targetDb.TransactionManager.StartTransaction())
                {
                    var targetBt = (BlockTable)tr.GetObject(targetDb.BlockTableId, OpenMode.ForRead);
                    var ids = new ObjectIdCollection { sourceBlockId };
                    var mapping = new IdMapping();

                    sourceDb.WblockCloneObjects(
                        ids,
                        targetBt.ObjectId,
                        mapping,
                        DuplicateRecordCloning.Replace,
                        false);

                    if (!mapping.Contains(sourceBlockId))
                        throw new InvalidOperationException("Не удалось импортировать блок.");

                    importedBlockId = mapping[sourceBlockId].Value;

                    var imported = (BlockTableRecord)tr.GetObject(importedBlockId, OpenMode.ForWrite);
                    if (!string.Equals(imported.Name, targetBlockName, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            imported.Name = targetBlockName;
                        }
                        catch
                        {
                            targetBlockName = imported.Name;
                        }
                    }

                    var halfHeight = ComputeBlockHalfHeight(tr, importedBlockId);
                    tr.Commit();

                    return RegisterTemplate(
                        targetDb,
                        displayName.Trim(),
                        code,
                        description,
                        targetBlockName,
                        filePath,
                        halfHeight);
                }
            }
        }

        public static bool IsTemplateInUse(string templateId)
        {
            return PtObjectRepository.All.Any(o => o.ShapeId == templateId);
        }

        private static PrecreatedTemplate CreateBlockFromEntities(
            Database db,
            ICollection<ObjectId> sourceIds,
            Point3d basePoint,
            string displayName,
            string code,
            string description,
            string sourceFile)
        {
            var blockName = CreateUniqueBlockName(db, displayName);
            var toBlock = Matrix3d.Displacement(basePoint.GetVectorTo(Point3d.Origin));

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForWrite);
                var btr = new BlockTableRecord
                {
                    Name = blockName,
                    Origin = basePoint
                };

                var blockId = bt.Add(btr);
                tr.AddNewlyCreatedDBObject(btr, true);

                foreach (var sourceId in sourceIds)
                {
                    if (sourceId.IsNull || !sourceId.IsValid)
                        continue;

                    var entity = tr.GetObject(sourceId, OpenMode.ForRead, false) as Entity;
                    if (entity == null)
                        continue;

                    var clone = entity.Clone() as Entity;
                    if (clone == null)
                        continue;

                    clone.TransformBy(toBlock);
                    btr.AppendEntity(clone);
                    tr.AddNewlyCreatedDBObject(clone, true);
                }

                if (btr.IsErased || IsBlockEmpty(tr, blockId))
                    throw new InvalidOperationException("Не удалось создать блок: нет подходящих объектов.");

                var halfHeight = ComputeBlockHalfHeight(tr, blockId);
                tr.Commit();

                return RegisterTemplate(
                    db,
                    displayName,
                    code,
                    description,
                    blockName,
                    sourceFile,
                    halfHeight);
            }
        }

        private static PrecreatedTemplate RegisterTemplate(
            Database db,
            string displayName,
            string code,
            string description,
            string blockName,
            string sourceFile,
            double halfHeight)
        {
            if (halfHeight <= 0)
                halfHeight = PtLayoutConstants.RectangleHeight / 2;

            var template = new PrecreatedTemplate
            {
                Id = "pre_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                Code = code,
                Name = displayName,
                Description = description,
                BlockName = blockName,
                SourceFile = sourceFile,
                ShapeHalfHeight = halfHeight
            };

            BlockPreviewRenderer.RefreshTemplatePreview(db, template);
            PrecreatedTemplateRepository.Add(template);
            return template;
        }

        private static string CreateUniqueBlockName(Database db, string displayName)
        {
            var baseName = BlockPrefix + SanitizeName(displayName);
            if (baseName.Length <= BlockPrefix.Length)
                baseName = BlockPrefix + "OBJ";

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(baseName))
                {
                    tr.Commit();
                    return baseName;
                }

                for (var i = 2; i < 1000; i++)
                {
                    var candidate = $"{baseName}_{i}";
                    if (!bt.Has(candidate))
                    {
                        tr.Commit();
                        return candidate;
                    }
                }

                tr.Commit();
            }

            return BlockPrefix + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
        }

        private static string SanitizeName(string value)
        {
            var sb = new StringBuilder();
            foreach (var ch in value.Trim().ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    sb.Append(ch);
                else if (char.IsWhiteSpace(ch) || ch == '-')
                    sb.Append('_');
            }

            var result = sb.ToString().Trim('_');
            return result.Length > 20 ? result.Substring(0, 20) : result;
        }

        private static bool IsBlockEmpty(Transaction tr, ObjectId blockId)
        {
            var btr = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
            foreach (ObjectId _ in btr)
                return false;

            return true;
        }

        private static double ComputeBlockHalfHeight(Transaction tr, ObjectId blockId)
        {
            try
            {
                var btr = (BlockTableRecord)tr.GetObject(blockId, OpenMode.ForRead);
                var ext = new Extents3d();
                var hasExtents = false;

                foreach (ObjectId id in btr)
                {
                    var ent = tr.GetObject(id, OpenMode.ForRead) as Entity;
                    if (ent == null)
                        continue;

                    ext.AddExtents(ent.GeometricExtents);
                    hasExtents = true;
                }

                if (!hasExtents)
                    return PtLayoutConstants.RectangleHeight / 2;

                var height = ext.MaxPoint.Y - ext.MinPoint.Y;
                if (height < 0.001)
                    return PtLayoutConstants.RectangleHeight / 2;

                return height / 2;
            }
            catch
            {
                return PtLayoutConstants.RectangleHeight / 2;
            }
        }
    }
}
