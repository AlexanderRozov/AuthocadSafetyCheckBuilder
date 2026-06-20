using Demo.Models;
using Demo.Services;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Demo.ui
{
    public partial class PtMainWindow
    {
        private void InitTemplatesTab()
        {
            RefreshTemplatesTab();
        }

        internal void RefreshTemplatesTab()
        {
            if (lstPrecreatedTemplates == null)
                return;

            var selectedId = (lstPrecreatedTemplates.SelectedItem as PrecreatedTemplate)?.Id;
            lstPrecreatedTemplates.ItemsSource = PrecreatedTemplateRepository.All.ToList();
            RefreshBlockCatalog();

            if (!string.IsNullOrEmpty(selectedId))
            {
                var match = PrecreatedTemplateRepository.Get(selectedId);
                if (match != null)
                    lstPrecreatedTemplates.SelectedItem = match;
            }

            UpdateTemplatePreviewPanel(lstPrecreatedTemplates.SelectedItem as PrecreatedTemplate);
        }

        private void LstPrecreatedTemplates_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstPrecreatedTemplates.SelectedItem is PrecreatedTemplate template)
            {
                txtTemplateName.Text = template.Name;
                txtTemplateDescription.Text = template.Description ?? string.Empty;
            }

            UpdateTemplatePreviewPanel(lstPrecreatedTemplates.SelectedItem as PrecreatedTemplate);
        }

        private void CmbBlock_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateNumberAndPreview();
            UpdatePlacementPreview();
        }

        private void UpdatePlacementPreview()
        {
            if (borderShapePreview == null)
                return;

            var block = CurrentBlock;
            if (block?.IsPrecreatedObject != true)
            {
                borderShapePreview.Visibility = Visibility.Collapsed;
                return;
            }

            var template = PrecreatedTemplateRepository.Get(block.Id);
            if (template == null)
            {
                borderShapePreview.Visibility = Visibility.Collapsed;
                return;
            }

            borderShapePreview.Visibility = Visibility.Visible;
            imgPlacementPreview.Source = PrecreatedPreviewHelper.CreateImage(template.PreviewImageBase64);
            txtPlacementPreviewCode.Text = template.DisplayLabel;
            txtPlacementPreviewDescription.Text = string.IsNullOrWhiteSpace(template.Description)
                ? "—"
                : template.Description;
            txtPlacementPreviewBlock.Text = $"Блок: {template.BlockName}";

            TrySelectDeviceByCode(template.Code);
        }

        private void TrySelectDeviceByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || cmbDeviceType == null)
                return;

            var device = DeviceCatalog.GetAll()
                .FirstOrDefault(d => string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase));

            if (device != null)
                cmbDeviceType.SelectedItem = device;
        }

        private void UpdateTemplatePreviewPanel(PrecreatedTemplate template)
        {
            if (imgTemplatePreview == null)
                return;

            if (template == null)
            {
                imgTemplatePreview.Source = null;
                txtTemplatePreviewCode.Text = "—";
                txtTemplatePreviewDescription.Text = "Выберите шаблон в списке.";
                txtTemplatePreviewBlock.Text = string.Empty;
                return;
            }

            imgTemplatePreview.Source = PrecreatedPreviewHelper.CreateImage(template.PreviewImageBase64);
            txtTemplatePreviewCode.Text = template.DisplayLabel;
            txtTemplatePreviewDescription.Text = string.IsNullOrWhiteSpace(template.Description)
                ? "—"
                : template.Description;
            txtTemplatePreviewBlock.Text = $"Блок: {template.BlockName}";
        }

        private void BtnCreateTemplateFromDrawing_OnClick(object sender, RoutedEventArgs e)
        {
            var name = txtTemplateName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите имя или код шаблона.", "Шаблоны", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PtInteractionSession.BeginCreatePrecreatedTemplate(name);
            SendPickCommand("PTPICKPRECREATE");
        }

        private void BtnImportFromLegendTable_OnClick(object sender, RoutedEventArgs e)
        {
            PtInteractionSession.BeginImportLegendTable();
            SendPickCommand("PTPICKLEGENDTABLE");
        }

        private void BtnImportFromCsv_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV / Excel (*.csv;*.txt)|*.csv;*.txt|Все файлы (*.*)|*.*",
                Title = "Выберите CSV (экспорт из Excel)"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                LegendImportService.ImportResult result = null;
                DocumentLockHelper.Run((_, db) =>
                {
                    result = LegendImportService.ImportFromCsvFile(db, dialog.FileName);
                    PtPersistenceService.Save(db, PtDocumentRegistry.GetByDatabase(db));
                });

                RefreshTemplatesTab();
                ShowImportResult(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Шаблоны", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void ShowImportResult(LegendImportService.ImportResult result)
        {
            if (result == null)
                return;

            var message = new StringBuilder();
            message.AppendLine($"Добавлено: {result.Imported}");
            message.AppendLine($"Пропущено: {result.Skipped}");
            if (result.Messages.Count > 0)
            {
                message.AppendLine();
                message.AppendLine(string.Join(Environment.NewLine, result.Messages.Take(8)));
            }

            MessageBox.Show(
                message.ToString(),
                "Импорт шаблонов",
                MessageBoxButton.OK,
                result.Skipped > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }

        private void BtnBrowseImportDwg_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Чертёж AutoCAD (*.dwg)|*.dwg|Все файлы (*.*)|*.*",
                Title = "Выберите файл чертежа"
            };

            if (dialog.ShowDialog() != true)
                return;

            txtImportDwgPath.Text = dialog.FileName;
            try
            {
                var blocks = PrecreatedBlockService.ListBlockNames(dialog.FileName);
                cmbImportBlockName.ItemsSource = blocks;
                cmbImportBlockName.SelectedIndex = blocks.Count > 0 ? 0 : -1;

                if (blocks.Count == 0)
                {
                    MessageBox.Show("В файле не найдено именованных блоков.", "Шаблоны",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Шаблоны", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnImportTemplateFromFile_OnClick(object sender, RoutedEventArgs e)
        {
            var filePath = txtImportDwgPath.Text?.Trim();
            var blockName = cmbImportBlockName.SelectedItem as string;
            var displayName = txtTemplateName.Text?.Trim();
            var description = txtTemplateDescription.Text?.Trim();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                MessageBox.Show("Выберите файл DWG.", "Шаблоны", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(blockName))
            {
                MessageBox.Show("Выберите блок для импорта.", "Шаблоны", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = blockName;

            try
            {
                DocumentLockHelper.Run((_, db) =>
                {
                    var template = PrecreatedBlockService.ImportFromDrawing(
                        db,
                        filePath,
                        blockName,
                        displayName,
                        displayName,
                        description);

                    PtPersistenceService.Save(db, PtDocumentRegistry.GetByDatabase(db));
                    MessageBox.Show(
                        $"Шаблон «{template.Name}» импортирован.\nБлок: {template.BlockName}",
                        "Шаблоны",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });

                RefreshTemplatesTab();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Шаблоны", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeletePrecreatedTemplate_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(lstPrecreatedTemplates.SelectedItem is PrecreatedTemplate template))
            {
                MessageBox.Show("Выберите шаблон в списке.", "Шаблоны", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PrecreatedBlockService.IsTemplateInUse(template.Id))
            {
                MessageBox.Show(
                    "Шаблон используется размещёнными объектами. Сначала удалите или смените форму этих объектов.",
                    "Шаблоны",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                    $"Удалить шаблон «{template.Name}» из библиотеки?\nБлок {template.BlockName} останется в чертеже.",
                    "Шаблоны",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                DocumentLockHelper.Run((_, db) =>
                {
                    PrecreatedTemplateRepository.Remove(template.Id);
                    PtPersistenceService.Save(db, PtDocumentRegistry.GetByDatabase(db));
                });

                RefreshTemplatesTab();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Шаблоны", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
