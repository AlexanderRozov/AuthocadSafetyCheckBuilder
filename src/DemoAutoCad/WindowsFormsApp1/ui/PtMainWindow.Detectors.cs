using Demo.Models;
using Demo.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Demo.ui
{
    public partial class PtMainWindow
    {
        private void InitDetectorTab()
        {
            cmbGridDirection.Items.Add(new ComboBoxItem
            {
                Content = "Авто",
                Tag = DetectorGridDirection.Auto
            });
            cmbGridDirection.Items.Add(new ComboBoxItem
            {
                Content = "Горизонтальные ряды",
                Tag = DetectorGridDirection.HorizontalRows
            });
            cmbGridDirection.Items.Add(new ComboBoxItem
            {
                Content = "Вертикальные столбцы",
                Tag = DetectorGridDirection.VerticalColumns
            });
            cmbGridDirection.SelectedIndex = 0;

            cmbHatchPattern.ItemsSource = new[]
            {
               "ANSI31", "SOLID", "ANSI32", "ANSI37", "AR-SAND"
            };
            cmbHatchPattern.SelectedIndex = 1;

            txtDetectorRadius.Text = PtLayoutConstants.DefaultDetectorRadius.ToString(
                "0.##", System.Globalization.CultureInfo.InvariantCulture);
            UpdateGridStepHint();
        }

        private void RefreshDetectorTab()
        {
            var tables = PtTableRepository.All.ToList();
            var selected = cmbDetectorTable.SelectedItem as PtTableSession ?? CurrentTable;

            cmbDetectorTable.ItemsSource = tables;
            if (selected != null)
                SelectComboItem(cmbDetectorTable, selected.Id);
            else if (tables.Count > 0)
                cmbDetectorTable.SelectedIndex = 0;

            RefreshDetectorZonesList();
        }

        private void RefreshDetectorZonesList()
        {
            var table = cmbDetectorTable.SelectedItem as PtTableSession;
            lstDetectorZones.ItemsSource = table != null
                ? PtDetectorZoneRepository.GetByTable(table.Id).ToList()
                : new List<PtDetectorZone>();
        }

        private void UpdateGridStepHint()
        {
            if (lblGridStepHint == null || txtDetectorRadius == null)
                return;

            if (!TryParseDouble(txtDetectorRadius.Text, out var radius) || radius <= 0)
            {
                lblGridStepHint.Text = "—";
                return;
            }

            var step = radius * Math.Sqrt(2);
            lblGridStepHint.Text = $"≈ {step:0.##} м (R × √2)";
        }

        private static bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(
                text?.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }

        private DetectorGridDirection GetSelectedGridDirection()
        {
            if (cmbGridDirection.SelectedItem is ComboBoxItem item && item.Tag is DetectorGridDirection dir)
                return dir;

            return DetectorGridDirection.Auto;
        }

        private void CmbDetectorTable_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
            RefreshDetectorZonesList();

        private void TxtDetectorRadius_OnTextChanged(object sender, TextChangedEventArgs e) =>
            UpdateGridStepHint();

        private void LstDetectorZones_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstDetectorZones.SelectedItem is PtDetectorZone zone &&
                !string.IsNullOrWhiteSpace(zone.HatchPattern))
            {
                cmbHatchPattern.SelectedItem = zone.HatchPattern;
            }
        }

        private void BtnPlaceDetectors_OnClick(object sender, RoutedEventArgs e)
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            var table = cmbDetectorTable.SelectedItem as PtTableSession;
            if (table == null)
            {
                MessageBox.Show("Сначала создайте или выберите таблицу.", "ПТ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryParseDouble(txtDetectorRadius.Text, out var radius) || radius <= 0)
            {
                MessageBox.Show("Укажите корректный радиус.", "ПТ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var hatchPattern = cmbHatchPattern.SelectedItem as string ?? "ANSI31";
            var direction = GetSelectedGridDirection();

            PtInteractionSession.BeginPlaceDetectors(table.Id, radius, direction, hatchPattern);
            SendPickCommand("PTPICKDETECTOR");
        }

        private void BtnDeleteZone_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(lstDetectorZones.SelectedItem is PtDetectorZone zone))
            {
                MessageBox.Show("Выберите зону в списке.", "ПТ",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(
                    $"Удалить зону «{zone.Name}» и все её извещатели ({zone.DetectorObjectIds.Count})?",
                    "ПТ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                DocumentLockHelper.Run((lockedDoc, db) =>
                {
                    DetectorPlacementService.DeleteZone(db, zone.Id);
                    lockedDoc.Editor.Regen();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RefreshTables();
        }

        private void BtnApplyHatch_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(lstDetectorZones.SelectedItem is PtDetectorZone zone))
            {
                MessageBox.Show("Выберите зону в списке.", "ПТ",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var pattern = cmbHatchPattern.SelectedItem as string ?? "ANSI31";

            try
            {
                DocumentLockHelper.Run((lockedDoc, db) =>
                {
                    DetectorPlacementService.ApplyHatchPattern(db, zone.Id, pattern);
                    lockedDoc.Editor.Regen();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RefreshDetectorZonesList();
        }
    }
}
