using Autodesk.AutoCAD.DatabaseServices;
using Demo.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Demo.Services
{
    public static class LegendImportService
    {
        public sealed class ImportResult
        {
            public int Imported { get; set; }
            public int Skipped { get; set; }
            public List<string> Messages { get; } = new List<string>();
        }

        /// <summary>
        /// CSV/TSV columns: Code;Description;BlockName;Name;SourceDwg
        /// Excel: сохраните лист как «CSV (разделители — точка с запятой)».
        /// </summary>
        public static ImportResult ImportFromCsvFile(Database db, string filePath)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("Файл не найден.", filePath);

            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length == 0)
                throw new InvalidOperationException("Файл пуст.");

            var delimiter = DetectDelimiter(lines[0]);
            var startRow = LooksLikeHeader(lines[0]) ? 1 : 0;
            var result = new ImportResult();

            for (var i = startRow; i < lines.Length; i++)
            {
                var line = lines[i]?.Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = SplitCsvLine(line, delimiter);
                if (parts.Count == 0)
                    continue;

                var code = GetPart(parts, 0);
                var description = GetPart(parts, 1);
                var blockName = GetPart(parts, 2);
                var name = GetPart(parts, 3);
                var sourceDwg = GetPart(parts, 4);

                if (string.IsNullOrWhiteSpace(blockName) && !string.IsNullOrWhiteSpace(code))
                    blockName = code;

                if (string.IsNullOrWhiteSpace(name))
                    name = string.IsNullOrWhiteSpace(code) ? blockName : code;

                try
                {
                    ImportRow(db, code, name, description, blockName, sourceDwg);
                    result.Imported++;
                }
                catch (Exception ex)
                {
                    result.Skipped++;
                    result.Messages.Add($"Строка {i + 1}: {ex.Message}");
                }
            }

            return result;
        }

        public static ImportResult ImportFromAutoCadTable(Database db, ObjectId tableId)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));
            if (tableId.IsNull)
                throw new ArgumentException("Таблица не выбрана.");

            var result = new ImportResult();

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var table = tr.GetObject(tableId, OpenMode.ForRead) as Table;
                if (table == null)
                    throw new InvalidOperationException("Выбранный объект не является таблицей AutoCAD.");

                var startRow = 1;
                for (var row = startRow; row < table.Rows.Count; row++)
                {
                    try
                    {
                        var symbolBlock = ResolveBlockFromCell(table, row, 0, tr);
                        var code = GetCellText(table, row, 1);
                        var description = GetCellText(table, row, 2);

                        if (string.IsNullOrWhiteSpace(symbolBlock))
                            symbolBlock = GetCellText(table, row, 0);

                        if (string.IsNullOrWhiteSpace(code) &&
                            string.IsNullOrWhiteSpace(description) &&
                            string.IsNullOrWhiteSpace(symbolBlock))
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(code))
                            code = symbolBlock;

                        var name = string.IsNullOrWhiteSpace(code) ? symbolBlock : code;
                        ImportRow(db, code, name, description, symbolBlock, null);
                        result.Imported++;
                    }
                    catch (Exception ex)
                    {
                        result.Skipped++;
                        result.Messages.Add($"Строка {row + 1}: {ex.Message}");
                    }
                }

                tr.Commit();
            }

            return result;
        }

        private static void ImportRow(
            Database db,
            string code,
            string name,
            string description,
            string blockName,
            string sourceDwg)
        {
            if (string.IsNullOrWhiteSpace(blockName))
                throw new InvalidOperationException("Не указан блок.");

            PrecreatedTemplate template;
            if (!string.IsNullOrWhiteSpace(sourceDwg) && File.Exists(sourceDwg))
            {
                template = PrecreatedBlockService.ImportFromDrawing(
                    db,
                    sourceDwg,
                    blockName,
                    name,
                    code,
                    description);
            }
            else if (BlockExists(db, blockName))
            {
                template = PrecreatedBlockService.RegisterExistingBlock(
                    db,
                    blockName,
                    name,
                    code,
                    description);
            }
            else
            {
                throw new InvalidOperationException($"Блок «{blockName}» не найден в чертеже.");
            }

            if (template == null)
                throw new InvalidOperationException("Не удалось создать шаблон.");
        }

        private static bool BlockExists(Database db, string blockName)
        {
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var exists = bt.Has(blockName);
                tr.Commit();
                return exists;
            }
        }

        private static string ResolveBlockFromCell(Table table, int row, int col, Transaction tr)
        {
            var cell = table.Cells[row, col];

            try
            {
                if (cell.BlockTableRecordId.IsValid && !cell.BlockTableRecordId.IsNull)
                {
                    var btr = tr.GetObject(cell.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;
                    if (btr != null && !string.IsNullOrWhiteSpace(btr.Name))
                        return btr.Name;
                }
            }
            catch
            {
                // ignore — fallback to text
            }

            return null;
        }

        private static string GetCellText(Table table, int row, int col)
        {
            if (row >= table.Rows.Count || col >= table.Columns.Count)
                return string.Empty;

            return table.Cells[row, col].TextString?.Trim() ?? string.Empty;
        }

        private static char DetectDelimiter(string line)
        {
            if (line.IndexOf(';') >= 0)
                return ';';
            if (line.IndexOf('\t') >= 0)
                return '\t';
            if (line.IndexOf(',') >= 0)
                return ',';

            return ';';
        }

        private static bool LooksLikeHeader(string line)
        {
            var lower = line.ToLowerInvariant();
            return lower.Contains("code") ||
                   lower.Contains("код") ||
                   lower.Contains("description") ||
                   lower.Contains("опис") ||
                   lower.Contains("block");
        }

        private static List<string> SplitCsvLine(string line, char delimiter)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (ch == delimiter && !inQuotes)
                {
                    parts.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            parts.Add(current.ToString().Trim());
            return parts;
        }

        private static string GetPart(IList<string> parts, int index) =>
            index < parts.Count ? parts[index]?.Trim() : string.Empty;
    }
}
