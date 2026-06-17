using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;

namespace Demo.Services
{
    public static class PtLayoutManager
    {
        private static ObjectId _tableId = ObjectId.Null;
        private static ObjectId _busLineId = ObjectId.Null;
        private static int _deviceCount;

        public static PtObject AddDevice(Database db, PlaceDeviceRequest request)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                DrawingService.EnsureLayers(tr, db);

                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(
                    bt[BlockTableRecord.ModelSpace],
                    OpenMode.ForWrite);

                _deviceCount++;
                var dataColumnIndex = _deviceCount;
                var columnNumber = PtLayoutConstants.FirstColumnNumber + _deviceCount - 1;
                var fdNumber = PtLayoutConstants.FirstFdNumber + _deviceCount - 1;

                if (_tableId.IsNull)
                    CreateInitialTable(tr, db, ms, request, columnNumber, fdNumber);
                else
                    AppendTableColumn(tr, request, columnNumber, fdNumber);

                var centerX = GetDataColumnCenterX(dataColumnIndex);
                var center = new Point3d(centerX, PtLayoutConstants.BusLineY, 0);

                UpdateBusLineGeometry(tr, ms, dataColumnIndex);

                var ptObject = DrawingService.DrawDevice(tr, ms, request, center, dataColumnIndex);
                PtObjectRepository.Add(ptObject);

                tr.Commit();
                return ptObject;
            }
        }

        private static void CreateInitialTable(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            PlaceDeviceRequest request,
            int columnNumber,
            int fdNumber)
        {
            var table = new Table();
            table.SetDatabaseDefaults(db);
            table.TableStyle = db.Tablestyle;
            table.Layer = PtLayoutConstants.LayerTable;
            table.Position = new Point3d(
                PtLayoutConstants.TableOriginX,
                PtLayoutConstants.TableOriginY,
                0);

            table.SetSize(PtLayoutConstants.TableRowCount, 2);
            for (var row = 0; row < PtLayoutConstants.TableRowCount; row++)
                table.Rows[row].Height = PtLayoutConstants.RowHeight;

            table.Columns[0].Width = PtLayoutConstants.LabelColumnWidth;
            table.Columns[1].Width = PtLayoutConstants.DataColumnWidth;

            SetLabelCells(table);
            FillDataColumn(table, 1, request, columnNumber, fdNumber);

            ms.AppendEntity(table);
            tr.AddNewlyCreatedDBObject(table, true);
            _tableId = table.ObjectId;
        }

        private static void AppendTableColumn(
            Transaction tr,
            PlaceDeviceRequest request,
            int columnNumber,
            int fdNumber)
        {
            var table = (Table)tr.GetObject(_tableId, OpenMode.ForWrite);
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
        }

        private static double GetDataColumnCenterX(int dataColumnIndex)
        {
            return PtLayoutConstants.TableOriginX
                + PtLayoutConstants.LabelColumnWidth
                + (dataColumnIndex - 0.5) * PtLayoutConstants.DataColumnWidth;
        }

        private static void UpdateBusLineGeometry(Transaction tr, BlockTableRecord ms, int dataColumnCount)
        {
            var startX = GetDataColumnCenterX(1);
            var endX = GetDataColumnCenterX(dataColumnCount) + PtLayoutConstants.BusLineExtension;
            var start = new Point3d(startX, PtLayoutConstants.BusLineY, 0);
            var end = new Point3d(endX, PtLayoutConstants.BusLineY, 0);

            if (_busLineId.IsNull)
            {
                _busLineId = DrawingService.DrawBusLine(tr, ms, start, end);
            }
            else
            {
                DrawingService.UpdateBusLine(tr, _busLineId, start, end);
            }
        }
    }
}
