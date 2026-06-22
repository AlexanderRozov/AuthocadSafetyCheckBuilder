using Autodesk.AutoCAD.DatabaseServices;
using Pt.Models;
using System;

namespace Pt.Services
{
    internal static class PtTableLayout
    {
        public static void ConfigureRowHeights(Table table)
        {
            for (var row = 0; row < table.Rows.Count; row++)
            {
                if (row == PtLayoutConstants.RowPicture)
                    table.Rows[row].Height = PtLayoutConstants.PictureRowHeight;
                else if (row >= PtLayoutConstants.RowCode)
                    table.Rows[row].Height = PtLayoutConstants.VerticalRowHeight;
                else
                    table.Rows[row].Height = PtLayoutConstants.RowHeight;
            }
        }

        public static void SetLabelColumn(Table table)
        {
            table.Cells[PtLayoutConstants.RowNode, 0].TextString = "Узел В";
            table.Cells[PtLayoutConstants.RowModuleJs, 0].TextString = "Модуль";
            table.Cells[PtLayoutConstants.RowModuleFd, 0].TextString = string.Empty;
            table.Cells[PtLayoutConstants.RowPicture, 0].TextString = string.Empty;
            table.Cells[PtLayoutConstants.RowCode, 0].TextString = "Код";
            table.Cells[PtLayoutConstants.RowNumber, 0].TextString = "Номер";
            table.Cells[PtLayoutConstants.RowName, 0].TextString = "Наименование";

            ApplyLabelMerges(table);
        }

        public static void ApplyLabelMerges(Table table)
        {
            if (table.Rows.Count <= PtLayoutConstants.RowModuleFd)
                return;

            try
            {
                var moduleRange = CellRange.Create(
                    table,
                    PtLayoutConstants.RowModuleJs,
                    0,
                    PtLayoutConstants.RowModuleFd,
                    0);
                table.MergeCells(moduleRange);
            }
            catch
            {
                // merged already or table not ready
            }
        }

        public static void FillDataColumn(
            Table table,
            Transaction tr,
            Database db,
            int col,
            int columnNumber,
            int fdNumber,
            string code,
            string number,
            string fullName,
            BlockTemplate blockTemplate)
        {
            table.Cells[PtLayoutConstants.RowNode, col].TextString = columnNumber.ToString();
            table.Cells[PtLayoutConstants.RowModuleJs, col].TextString =
                $"JS05-UC-{1000 + columnNumber}A";
            table.Cells[PtLayoutConstants.RowModuleFd, col].TextString = $"FD-{fdNumber:D4}";
            table.Cells[PtLayoutConstants.RowCode, col].TextString = code ?? string.Empty;
            table.Cells[PtLayoutConstants.RowNumber, col].TextString = number ?? string.Empty;
            table.Cells[PtLayoutConstants.RowName, col].TextString = fullName ?? string.Empty;

            SetPictureCell(table, tr, db, col, blockTemplate);
        }

        public static void FillDataColumnFromObject(
            Table table,
            Transaction tr,
            Database db,
            int col,
            PtObject obj)
        {
            table.Cells[PtLayoutConstants.RowNode, col].TextString = obj.ColumnNumber.ToString();
            table.Cells[PtLayoutConstants.RowModuleJs, col].TextString = obj.JsCode ?? string.Empty;
            table.Cells[PtLayoutConstants.RowModuleFd, col].TextString = obj.FdCode ?? string.Empty;
            table.Cells[PtLayoutConstants.RowCode, col].TextString = obj.Code ?? string.Empty;
            table.Cells[PtLayoutConstants.RowNumber, col].TextString = obj.Number ?? string.Empty;
            table.Cells[PtLayoutConstants.RowName, col].TextString = obj.FullName ?? string.Empty;

            SetPictureCell(table, tr, db, col, BlockCatalog.GetById(obj.ShapeId));
        }

        public static void StyleTable(Table table)
        {
            for (var row = 0; row < table.Rows.Count; row++)
            {
                for (var col = 0; col < table.Columns.Count; col++)
                    ApplyCellStyle(table.Cells[row, col], row);
            }

            ApplyLabelMerges(table);
        }

        public static void StyleColumnCells(Table table, int col)
        {
            for (var row = 0; row < table.Rows.Count; row++)
                ApplyCellStyle(table.Cells[row, col], row);
        }

        private static void SetPictureCell(
            Table table,
            Transaction tr,
            Database db,
            int col,
            BlockTemplate blockTemplate)
        {
            var cell = table.Cells[PtLayoutConstants.RowPicture, col];
            ClearCellContents(cell);
            cell.BlockTableRecordId = ObjectId.Null;
            cell.TextString = string.Empty;

            if (blockTemplate?.UsesBlockReference != true ||
                string.IsNullOrWhiteSpace(blockTemplate.BlockName))
            {
                return;
            }

            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            if (!bt.Has(blockTemplate.BlockName))
                return;

            cell.BlockTableRecordId = bt[blockTemplate.BlockName];
            cell.Alignment = CellAlignment.MiddleCenter;

            var content = EnsureContent(cell);
            if (content != null)
                content.IsAutoScale = true;
        }

        private static void ApplyCellStyle(Cell cell, int row)
        {
            cell.Alignment = CellAlignment.MiddleCenter;

            if (row < PtLayoutConstants.RowCode)
            {
                var content = EnsureContent(cell);
                if (content != null)
                {
                    content.TextHeight = PtLayoutConstants.TableTextHeight;
                    content.Rotation = 0;
                }

                return;
            }

            var text = cell.TextString ?? string.Empty;
            var contentItem = EnsureContent(cell);
            if (contentItem == null)
                return;

            contentItem.TextString = text;
            contentItem.TextHeight = PtLayoutConstants.TableTextHeight;
            contentItem.Rotation = Math.PI / 2;
        }

        private static CellContent EnsureContent(Cell cell)
        {
            if (cell.Contents.Count == 0)
                cell.Contents.Add();

            return cell.Contents[0];
        }

        private static void ClearCellContents(Cell cell)
        {
            while (cell.Contents.Count > 0)
                cell.Contents.RemoveAt(cell.Contents.Count - 1);
        }
    }
}
