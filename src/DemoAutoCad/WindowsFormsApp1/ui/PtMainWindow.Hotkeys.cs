using Demo.Models;
using Demo.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using FormsKeys = System.Windows.Forms.Keys;

namespace Demo.ui
{
    public partial class PtMainWindow
    {
        private readonly List<HotkeyBindingRow> _hotkeyBindingRows = new List<HotkeyBindingRow>();
        private bool _suppressHotkeyUi;

        private void InitHotkeysTab()
        {
            HotkeyServices.SelectionApplied += OnHotkeySelectionApplied;
            HotkeyServices.InputController.ArmedStateChanged += OnHotkeyArmedStateChanged;
            LoadHotkeyConfigToUi();
            UpdateHotkeyArmedStatus();
        }

        private void OnHotkeySelectionApplied(HotkeySelection selection)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => OnHotkeySelectionApplied(selection)));
                return;
            }

            ApplyHotkeySelection(selection);
            BeginPlacementAfterHotkeySelection();
        }

        private void BeginPlacementAfterHotkeySelection()
        {
            Guid? parentId = (cmbParent?.SelectedItem as ParentItem)?.Id;
            TryAddObject(parentId, null);
        }

        private void OnHotkeyArmedStateChanged(bool armed)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => OnHotkeyArmedStateChanged(armed)));
                return;
            }

            UpdateHotkeyArmedStatus(armed);
            UpdateHotkeyArmedBanner(armed);
        }

        private void UpdateHotkeyArmedBanner(bool armed)
        {
            if (borderHotkeyArmedBanner == null)
                return;

            borderHotkeyArmedBanner.Visibility = armed ? Visibility.Visible : Visibility.Collapsed;
            Title = armed ? "ПТ — РЕЖИМ ВЫБОРА" : "ПТ — объекты и таблицы";

            if (tabMain != null)
                tabMain.IsEnabled = !armed;

            if (armed)
            {
                Keyboard.ClearFocus();
                if (Visibility == Visibility.Visible)
                    ShowActivated = false;
            }
        }

        public void ApplyHotkeySelection(HotkeySelection selection)
        {
            if (selection == null)
                return;

            if (selection.ListKind == HotkeyListKind.Device)
            {
                var device = DeviceCatalog.GetAll().FirstOrDefault(d => d.Id == selection.ItemId);
                if (device != null)
                    cmbDeviceType.SelectedItem = device;
            }
            else
            {
                RefreshBlockCatalog();
                var block = BlockCatalog.GetById(selection.ItemId);
                if (block != null)
                    cmbBlock.SelectedItem = block;
            }

            UpdateNumberAndPreview();
            UpdatePlacementPreview();

            if (lblHotkeyStatus != null)
                lblHotkeyStatus.Text = $"Выбрано: {DescribeHotkeySelection(selection)}";
        }

        private static string DescribeHotkeySelection(HotkeySelection selection)
        {
            var item = HotkeyServices.BindableCatalog.Find(selection.ListKind, selection.ItemId);
            return item?.DisplayLabel ?? selection.ItemId;
        }

        private void ApplyPendingHotkeySelection()
        {
            if (HotkeyServices.PendingSelection != null)
                ApplyHotkeySelection(HotkeyServices.PendingSelection);
        }

        private void LoadHotkeyConfigToUi()
        {
            if (chkHotkeysEnabled == null)
                return;

            _suppressHotkeyUi = true;
            try
            {
                var config = HotkeyServices.ConfigRepository.Load();
                chkHotkeysEnabled.IsChecked = config.Enabled;
                txtModeTriggerKey.Text = config.ModeTriggerKey;
                txtHotkeyTimeout.Text = config.TimeoutSeconds.ToString();

                _hotkeyBindingRows.Clear();
                foreach (var binding in config.Bindings ?? new List<HotkeyBindingEntry>())
                {
                    if (!TryParseListKind(binding.ListKind, out var listKind))
                        continue;

                    _hotkeyBindingRows.Add(new HotkeyBindingRow
                    {
                        Key = binding.Key,
                        ListKind = listKind,
                        ItemId = binding.ItemId,
                        Summary = BuildBindingSummary(binding.Key, listKind, binding.ItemId)
                    });
                }

                lstHotkeyBindings.ItemsSource = null;
                lstHotkeyBindings.ItemsSource = _hotkeyBindingRows.ToList();
            }
            finally
            {
                _suppressHotkeyUi = false;
            }

            HotkeyServices.InputController.ReloadConfig();
            RefreshHotkeyItemChoices();
        }

        private void SaveHotkeyConfigFromUi()
        {
            if (!int.TryParse(txtHotkeyTimeout.Text, out var timeout) || timeout <= 0)
                timeout = 8;

            var config = new HotkeyUserConfig
            {
                Enabled = chkHotkeysEnabled.IsChecked == true,
                ModeTriggerKey = txtModeTriggerKey.Text?.Trim(),
                TimeoutSeconds = timeout,
                Bindings = _hotkeyBindingRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.Key) && !string.IsNullOrWhiteSpace(r.ItemId))
                    .Select(r => new HotkeyBindingEntry
                    {
                        Key = r.Key.Trim(),
                        ListKind = r.ListKind == HotkeyListKind.Shape ? "shape" : "device",
                        ItemId = r.ItemId
                    })
                    .ToList()
            };

            HotkeyServices.ConfigRepository.Save(config);
            HotkeyServices.InputController.ReloadConfig();
            LoadHotkeyConfigToUi();

            MessageBox.Show(
                $"Настройки сохранены.\n{HotkeyServices.ConfigRepository.ConfigFilePath}",
                "Горячие клавиши",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RefreshHotkeyItemChoices()
        {
            if (cmbHotkeyItem == null)
                return;

            var kind = cmbHotkeyCategory.SelectedIndex == 1
                ? HotkeyListKind.Shape
                : HotkeyListKind.Device;

            cmbHotkeyItem.ItemsSource = HotkeyServices.BindableCatalog.GetItems(kind);
            cmbHotkeyItem.DisplayMemberPath = nameof(HotkeyBindableItem.DisplayLabel);
            cmbHotkeyItem.SelectedValuePath = nameof(HotkeyBindableItem.ItemId);
            if (cmbHotkeyItem.Items.Count > 0)
                cmbHotkeyItem.SelectedIndex = 0;
        }

        private void CmbHotkeyCategory_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressHotkeyUi)
                return;

            RefreshHotkeyItemChoices();
        }

        private void LstHotkeyBindings_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressHotkeyUi || !(lstHotkeyBindings.SelectedItem is HotkeyBindingRow row))
                return;

            txtBindingKey.Text = row.Key;
            cmbHotkeyCategory.SelectedIndex = row.ListKind == HotkeyListKind.Shape ? 1 : 0;
            RefreshHotkeyItemChoices();
            cmbHotkeyItem.SelectedValue = row.ItemId;
        }

        private void BtnAddHotkeyBinding_OnClick(object sender, RoutedEventArgs e)
        {
            var key = txtBindingKey.Text?.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show("Укажите клавишу для привязки.", "Горячие клавиши",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.Equals(key, txtModeTriggerKey.Text?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Эта клавиша уже используется для входа в режим.", "Горячие клавиши",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!(cmbHotkeyItem.SelectedValue is string itemId) || string.IsNullOrWhiteSpace(itemId))
            {
                MessageBox.Show("Выберите элемент из списка.", "Горячие клавиши",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var listKind = cmbHotkeyCategory.SelectedIndex == 1
                ? HotkeyListKind.Shape
                : HotkeyListKind.Device;

            var existing = _hotkeyBindingRows.FirstOrDefault(r =>
                string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.ListKind = listKind;
                existing.ItemId = itemId;
                existing.Summary = BuildBindingSummary(key, listKind, itemId);
            }
            else
            {
                _hotkeyBindingRows.Add(new HotkeyBindingRow
                {
                    Key = key,
                    ListKind = listKind,
                    ItemId = itemId,
                    Summary = BuildBindingSummary(key, listKind, itemId)
                });
            }

            lstHotkeyBindings.ItemsSource = null;
            lstHotkeyBindings.ItemsSource = _hotkeyBindingRows.ToList();
        }

        private void BtnRemoveHotkeyBinding_OnClick(object sender, RoutedEventArgs e)
        {
            if (!(lstHotkeyBindings.SelectedItem is HotkeyBindingRow row))
                return;

            _hotkeyBindingRows.Remove(row);
            lstHotkeyBindings.ItemsSource = null;
            lstHotkeyBindings.ItemsSource = _hotkeyBindingRows.ToList();
        }

        private void BtnSaveHotkeys_OnClick(object sender, RoutedEventArgs e) =>
            SaveHotkeyConfigFromUi();

        private void BtnReloadHotkeys_OnClick(object sender, RoutedEventArgs e) =>
            LoadHotkeyConfigToUi();

        private void HotkeyCapture_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (HotkeyServices.InputController.IsArmed)
                return;

            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt || e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftShift || e.Key == Key.RightShift)
                return;

            var wpfKey = e.Key == Key.System ? e.SystemKey : e.Key;
            if (wpfKey == Key.Escape)
                return;

            var virtualKey = KeyInterop.VirtualKeyFromKey(wpfKey);
            if (sender is TextBox textBox)
                textBox.Text = ((FormsKeys)virtualKey).ToString();

            e.Handled = true;
        }

        private static string BuildBindingSummary(string key, HotkeyListKind listKind, string itemId)
        {
            var item = HotkeyServices.BindableCatalog.Find(listKind, itemId);
            var target = item?.DisplayLabel ?? itemId;
            var category = listKind == HotkeyListKind.Shape ? "Форма" : "Тип";
            return $"{key} → {category}: {target}";
        }

        private static bool TryParseListKind(string value, out HotkeyListKind listKind)
        {
            listKind = HotkeyListKind.Device;
            if (string.Equals(value, "shape", StringComparison.OrdinalIgnoreCase))
            {
                listKind = HotkeyListKind.Shape;
                return true;
            }

            return string.Equals(value, "device", StringComparison.OrdinalIgnoreCase) ||
                   Enum.TryParse(value, true, out listKind);
        }

        private void UpdateHotkeyArmedStatus(bool? armed = null)
        {
            if (lblHotkeyStatus == null)
                return;

            var isArmed = armed ?? HotkeyServices.InputController.IsArmed;
            lblHotkeyStatus.Text = isArmed
                ? "● РЕЖИМ ВЫБОРА АКТИВЕН — нажмите клавишу привязки или Esc."
                : $"Нажмите {txtModeTriggerKey?.Text ?? "F11"} в чертеже для входа в режим выбора.";
            lblHotkeyStatus.Foreground = isArmed
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 126, 34))
                : System.Windows.Media.Brushes.Black;
        }
    }
}
