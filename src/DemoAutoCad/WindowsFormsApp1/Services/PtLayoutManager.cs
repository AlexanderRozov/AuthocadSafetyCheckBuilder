using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Linq;
using Pt.Models;

namespace Pt.Services
{
    public static class PtLayoutManager
    {
        public static PtTableSession CreateTableAtPoint(Database db, Point3d origin)
        {
            var session = PtTableRepository.Create(null);

            using (var tr = db.TransactionManager.StartTransaction())
            {
                DrawingService.EnsureLayers(tr, db);

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite);

                var table = CreateEmptyTable(tr, db, origin);
                ms.AppendEntity(table);
                tr.AddNewlyCreatedDBObject(table, true);

                session.Origin = origin;
                session.TableId = table.ObjectId;
                session.DeviceCount = 0;

                tr.Commit();
            }

            Persist(db);
            return session;
        }

        public static PtObject AddDevice(Database db, PlaceDeviceRequest request)
        {
            var session = PtTableRepository.Get(request.TableId);
            if (session == null)
                throw new System.InvalidOperationException("Таблица не найдена.");

            PtObject ptObject;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                DrawingService.EnsureLayers(tr, db);

                if (session.TableId.IsNull || !HandleHelper.IsValid(tr, session.TableId))
                    throw new InvalidOperationException(
                        "Таблица не найдена на чертеже. Создайте таблицу заново.");

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite);

                session.DeviceCount++;
                var dataColumnIndex = session.DeviceCount;
                var columnNumber = PtLayoutConstants.FirstColumnNumber + session.DeviceCount - 1;
                var fdNumber = PtLayoutConstants.FirstFdNumber + session.DeviceCount - 1;

                var table = (Table)tr.GetObject(session.TableId, OpenMode.ForWrite);

                if (table.Columns.Count < 2)
                    AddFirstDataColumn(tr, db, table, request, columnNumber, fdNumber);
                else
                    AppendTableColumn(tr, db, table, request, columnNumber, fdNumber);

                var center = request.InsertionPoint;
                ptObject = DrawingService.DrawDevice(
                    tr, db, ms, request, center, dataColumnIndex, session.Id);

                ptObject.ColumnNumber = columnNumber;
                ptObject.FdCode = $"FD-{fdNumber:D4}";
                ptObject.JsCode = $"JS05-UC-{1000 + columnNumber}A";
                ptObject.TableId = session.Id;
                ptObject.ParentObjectId = request.ParentObjectId;
                ptObject.BlockGroupId = request.BlockGroupId;

                PtObjectRepository.Add(ptObject);

                if (request.ParentObjectId.HasValue)
                {
                    CreateObjectLink(tr, db, ms, session.Id,
                        request.ParentObjectId.Value, ptObject.InstanceId);
                }

                tr.Commit();
            }

            Persist(db);
            return ptObject;
        }

        public static void CreateObjectLink(Database db, Guid tableId, Guid fromId, Guid toId)
        {
            if (fromId == toId)
                return;

            if (PtObjectRepository.WouldCreateParentCycle(fromId, toId))
                throw new System.InvalidOperationException("Нельзя создать циклическую связь.");

            var fromObj = PtObjectRepository.Get(fromId);
            var toObj = PtObjectRepository.Get(toId);
            if (fromObj == null || toObj == null)
                return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                DrawingService.EnsureLayers(tr, db);
                ObjectSelectionService.SyncCenterFromDrawing(db, fromObj, tr);
                ObjectSelectionService.SyncCenterFromDrawing(db, toObj, tr);

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite);

                EraseIncomingLinkArrows(tr, toId);

                var link = PtObjectRepository.AddLink(tableId, fromId, toId);
                link.ArrowId = DrawingService.DrawArrow(
                    tr, ms, fromObj.Center, toObj.Center,
                    DrawingService.GetShapeHalfHeight(fromObj),
                    DrawingService.GetShapeHalfHeight(toObj));

                tr.Commit();
            }

            Persist(db);
        }

        private static void CreateObjectLink(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            Guid tableId,
            Guid fromId,
            Guid toId)
        {
            var fromObj = PtObjectRepository.Get(fromId);
            var toObj = PtObjectRepository.Get(toId);
            if (fromObj == null || toObj == null)
                return;

            ObjectSelectionService.SyncCenterFromDrawing(db, fromObj, tr);
            ObjectSelectionService.SyncCenterFromDrawing(db, toObj, tr);
            EraseIncomingLinkArrows(tr, toId);

            var link = PtObjectRepository.AddLink(tableId, fromId, toId);
            link.ArrowId = DrawingService.DrawArrow(
                tr, ms, fromObj.Center, toObj.Center,
                DrawingService.GetShapeHalfHeight(fromObj),
                DrawingService.GetShapeHalfHeight(toObj));
        }

        private static void EraseIncomingLinkArrows(Transaction tr, Guid toObjectId)
        {
            foreach (var link in PtObjectRepository.AllLinks.Where(l => l.ToObjectId == toObjectId).ToList())
                DrawingService.EraseEntity(tr, link.ArrowId);

            PtObjectRepository.RemoveIncomingLinks(toObjectId);
        }

        public static void DeleteObject(Database db, PtObject obj)
        {
            var session = PtTableRepository.Get(obj.TableId);
            if (session == null)
                return;

            var tableId = obj.TableId;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                DrawingService.EraseObject(tr, obj);
                tr.Commit();
            }

            PtObjectRepository.ClearParentReference(obj.InstanceId);
            PtObjectRepository.Remove(obj);
            SyncTableColumnOrder(db, tableId);
            Persist(db);
        }

        public static void SyncTableColumnOrder(Database db, Guid tableId)
        {
            var session = PtTableRepository.Get(tableId);
            if (session == null || session.TableId.IsNull)
                return;

            var objects = PtObjectRepository.GetByTable(tableId).ToList();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var table = (Table)tr.GetObject(session.TableId, OpenMode.ForWrite);

                while (table.Columns.Count > 1)
                    table.DeleteColumns(1, 1);

                for (var i = 0; i < objects.Count; i++)
                {
                    var obj = objects[i];
                    var colIndex = table.Columns.Count;
                    table.InsertColumns(colIndex, PtLayoutConstants.DataColumnWidth, 1);
                    obj.ColumnIndex = colIndex;
                    obj.ColumnNumber = PtLayoutConstants.FirstColumnNumber + i;
                    var fdNumber = PtLayoutConstants.FirstFdNumber + i;
                    obj.FdCode = $"FD-{fdNumber:D4}";
                    obj.JsCode = $"JS05-UC-{1000 + obj.ColumnNumber}A";
                    PtTableLayout.FillDataColumnFromObject(table, tr, db, colIndex, obj);
                }

                session.DeviceCount = objects.Count;
                PtTableLayout.ConfigureRowHeights(table);
                PtTableLayout.SetLabelColumn(table);
                PtTableLayout.StyleTable(table);
                tr.Commit();
            }

            Persist(db);
        }

        public static void SyncObjectToDrawing(Database db, PtObject obj)
        {
            var session = PtTableRepository.Get(obj.TableId);
            if (session == null)
                return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                if (!session.TableId.IsNull && HandleHelper.IsValid(tr, session.TableId))
                {
                    var table = (Table)tr.GetObject(session.TableId, OpenMode.ForWrite);
                    var col = obj.ColumnIndex;
                    if (col > 0 && col < table.Columns.Count)
                    {
                        PtTableLayout.FillDataColumnFromObject(table, tr, db, col, obj);
                        PtTableLayout.StyleColumnCells(table, col);
                    }
                }

                if (!obj.LabelTextId.IsNull && HandleHelper.IsValid(tr, obj.LabelTextId))
                {
                    var label = (MText)tr.GetObject(obj.LabelTextId, OpenMode.ForWrite);
                    DrawingService.FitInsideLabel(tr, label, obj.Label, obj.FontSize, obj);
                }

                if (!obj.IdTextId.IsNull && HandleHelper.IsValid(tr, obj.IdTextId))
                {
                    var idText = (MText)tr.GetObject(obj.IdTextId, OpenMode.ForWrite);
                    idText.Contents = $"*-{obj.Code}-{obj.Number}";
                    idText.TextHeight = obj.FontSize;
                }

                tr.Commit();
            }

            Persist(db);
        }

        private static void Persist(Database db)
        {
            PtPersistenceService.Save(db, PtDocumentRegistry.GetByDatabase(db));
        }

        private static Table CreateEmptyTable(Transaction tr, Database db, Point3d origin)
        {
            var table = new Table();
            table.SetDatabaseDefaults(db);
            table.TableStyle = db.Tablestyle;
            table.Layer = PtLayoutConstants.LayerTable;
            table.Position = origin;
            table.SetSize(PtLayoutConstants.TableRowCount, 1);

            PtTableLayout.ConfigureRowHeights(table);
            table.Columns[0].Width = PtLayoutConstants.LabelColumnWidth;
            PtTableLayout.SetLabelColumn(table);
            PtTableLayout.StyleTable(table);
            return table;
        }

        private static void AddFirstDataColumn(
            Transaction tr,
            Database db,
            Table table,
            PlaceDeviceRequest request,
            int columnNumber,
            int fdNumber)
        {
            table.InsertColumns(1, PtLayoutConstants.DataColumnWidth, 1);
            FillDataColumn(tr, db, table, 1, request, columnNumber, fdNumber);
            PtTableLayout.ConfigureRowHeights(table);
            PtTableLayout.SetLabelColumn(table);
            PtTableLayout.StyleTable(table);
        }

        private static void AppendTableColumn(
            Transaction tr,
            Database db,
            Table table,
            PlaceDeviceRequest request,
            int columnNumber,
            int fdNumber)
        {
            var newColIndex = table.Columns.Count;
            table.InsertColumns(newColIndex, PtLayoutConstants.DataColumnWidth, 1);
            FillDataColumn(tr, db, table, newColIndex, request, columnNumber, fdNumber);
            PtTableLayout.ConfigureRowHeights(table);
            PtTableLayout.StyleTable(table);
        }

        private static void FillDataColumn(
            Transaction tr,
            Database db,
            Table table,
            int col,
            PlaceDeviceRequest request,
            int columnNumber,
            int fdNumber)
        {
            PtTableLayout.FillDataColumn(
                table,
                tr,
                db,
                col,
                columnNumber,
                fdNumber,
                request.DeviceType.Code,
                request.Number,
                request.DeviceType.Name,
                request.BlockTemplate);
        }
    }
}
