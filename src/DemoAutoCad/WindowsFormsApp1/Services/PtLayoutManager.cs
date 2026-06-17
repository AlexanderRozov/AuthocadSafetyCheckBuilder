using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;

namespace Demo.Services
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

            return session;
        }

        public static PtObject AddDevice(Database db, PlaceDeviceRequest request)
        {
            var session = PtTableRepository.Get(request.TableId);
            if (session == null)
                throw new System.InvalidOperationException("Таблица не найдена.");

            using (var tr = db.TransactionManager.StartTransaction())
            {
                DrawingService.EnsureLayers(tr, db);

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
                    AddFirstDataColumn(table, request, columnNumber, fdNumber);
                else
                    AppendTableColumn(table, request, columnNumber, fdNumber);

                var center = request.InsertionPoint;
                var ptObject = DrawingService.DrawDevice(
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
                    var link = PtObjectRepository.AddLink(
                        session.Id,
                        request.ParentObjectId.Value,
                        ptObject.InstanceId);

                    var parent = PtObjectRepository.Get(request.ParentObjectId.Value);
                    if (parent != null)
                    {
                        link.ArrowId = DrawingService.DrawArrow(
                            tr, ms, parent.Center, ptObject.Center);
                    }
                }

                tr.Commit();
                return ptObject;
            }
        }

        public static void DeleteObject(Database db, PtObject obj)
        {
            var session = PtTableRepository.Get(obj.TableId);
            if (session == null)
                return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                DrawingService.EraseObject(tr, obj);

                if (!session.TableId.IsNull)
                {
                    var table = (Table)tr.GetObject(session.TableId, OpenMode.ForWrite);
                    var col = obj.ColumnIndex;
                    if (col < table.Columns.Count)
                    {
                        table.Cells[0, col].TextString = string.Empty;
                        table.Cells[1, col].TextString = string.Empty;
                        table.Cells[2, col].TextString = string.Empty;
                        table.Cells[3, col].TextString = string.Empty;
                        table.Cells[4, col].TextString = string.Empty;
                        table.Cells[5, col].TextString = string.Empty;
                        if (table.Rows.Count > 6)
                            table.Cells[6, col].TextString = string.Empty;
                    }
                }

                tr.Commit();
            }

            PtObjectRepository.Remove(obj);
        }

        public static void SyncObjectToDrawing(Database db, PtObject obj)
        {
            var session = PtTableRepository.Get(obj.TableId);
            if (session == null || session.TableId.IsNull)
                return;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var table = (Table)tr.GetObject(session.TableId, OpenMode.ForWrite);
                var col = obj.ColumnIndex;
                if (col < table.Columns.Count)
                {
                    table.Cells[0, col].TextString = obj.ColumnNumber.ToString();
                    table.Cells[1, col].TextString = obj.FdCode;
                    table.Cells[2, col].TextString = obj.JsCode;
                    table.Cells[3, col].TextString = obj.Code;
                    table.Cells[4, col].TextString = obj.Number;
                    table.Cells[5, col].TextString = obj.FullName;
                    if (table.Rows.Count > 6)
                        table.Cells[6, col].TextString = PtObjectRepository.GetBlockName(obj);
                }

                if (!obj.LabelTextId.IsNull)
                {
                    var label = (MText)tr.GetObject(obj.LabelTextId, OpenMode.ForWrite);
                    label.Contents = obj.Label;
                    label.TextHeight = obj.FontSize;
                }

                if (!obj.IdTextId.IsNull)
                {
                    var idText = (MText)tr.GetObject(obj.IdTextId, OpenMode.ForWrite);
                    idText.Contents = $"*-{obj.Code}-{obj.Number}";
                    idText.TextHeight = obj.FontSize;
                }

                tr.Commit();
            }
        }

        private static Table CreateEmptyTable(Transaction tr, Database db, Point3d origin)
        {
            var table = new Table();
            table.SetDatabaseDefaults(db);
            table.TableStyle = db.Tablestyle;
            table.Layer = PtLayoutConstants.LayerTable;
            table.Position = origin;
            table.SetSize(PtLayoutConstants.TableRowCount, 1);

            for (var row = 0; row < PtLayoutConstants.TableRowCount; row++)
                table.Rows[row].Height = PtLayoutConstants.RowHeight;

            table.Columns[0].Width = PtLayoutConstants.LabelColumnWidth;
            SetLabelCells(table);
            return table;
        }

        private static void AddFirstDataColumn(
            Table table,
            PlaceDeviceRequest request,
            int columnNumber,
            int fdNumber)
        {
            table.InsertColumns(1, PtLayoutConstants.DataColumnWidth, 1);
            FillDataColumn(table, 1, request, columnNumber, fdNumber);
        }

        private static void AppendTableColumn(
            Table table,
            PlaceDeviceRequest request,
            int columnNumber,
            int fdNumber)
        {
            var newColIndex = table.Columns.Count;
            table.InsertColumns(newColIndex, PtLayoutConstants.DataColumnWidth, 1);
            FillDataColumn(table, newColIndex, request, columnNumber, fdNumber);
        }

        private static void SetLabelCells(Table table)
        {
            table.Cells[0, 0].TextString = "Узел В";
            table.Cells[1, 0].TextString = string.Empty;
            table.Cells[2, 0].TextString = string.Empty;
            table.Cells[3, 0].TextString = "Код";
            table.Cells[4, 0].TextString = "Номер";
            table.Cells[5, 0].TextString = "Наименование";
            table.Cells[6, 0].TextString = "Блок";
        }

        private static void FillDataColumn(
            Table table,
            int col,
            PlaceDeviceRequest request,
            int columnNumber,
            int fdNumber)
        {
            table.Cells[0, col].TextString = columnNumber.ToString();
            table.Cells[1, col].TextString = $"FD-{fdNumber:D4}";
            table.Cells[2, col].TextString = $"JS05-UC-{1000 + columnNumber}A";
            table.Cells[3, col].TextString = request.DeviceType.Code;
            table.Cells[4, col].TextString = request.Number;
            table.Cells[5, col].TextString = request.DeviceType.Name;
            if (table.Rows.Count > 6)
            {
                var blockName = request.BlockGroupId.HasValue
                    ? PtBlockRepository.Get(request.BlockGroupId.Value)?.Name ?? string.Empty
                    : string.Empty;
                table.Cells[6, col].TextString = blockName;
            }
        }
    }
}
