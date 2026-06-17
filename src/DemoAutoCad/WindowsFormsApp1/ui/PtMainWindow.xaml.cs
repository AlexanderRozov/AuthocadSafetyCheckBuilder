using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Demo.Models;
using Demo.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Demo.ui
{
    public partial class PtMainWindow : Window
    {
        private bool _suppressTreeSelect;
        private Point _treeDragStart;
        private TreeViewItem _draggedTreeNode;
        private const string TreeDragFormat = "PtTreeObject";

        public PtMainWindow()
        {
            InitializeComponent();
            Closing += (_, e) =>
            {
                e.Cancel = true;
                Hide();
            };
            LoadCatalogs();
            RefreshTables();
        }

        private void LoadCatalogs()
        {
            cmbDeviceType.ItemsSource = DeviceCatalog.GetAll();
            cmbBlock.ItemsSource = BlockCatalog.GetAll();
            cmbDeviceType.SelectedIndex = 0;
            cmbBlock.SelectedIndex = 0;
            UpdateNumberAndPreview();
        }

        private DeviceType CurrentDeviceType => cmbDeviceType.SelectedItem as DeviceType;
        private BlockTemplate CurrentBlock => cmbBlock.SelectedItem as BlockTemplate;
        private PtTableSession CurrentTable => cmbTable.SelectedItem as PtTableSession;
        private PtTableSession CurrentEditTable =>
            cmbTableEdit.SelectedItem as PtTableSession ?? PtTableRepository.ActiveTable;

        private void UpdateNumberAndPreview()
        {
            var device = CurrentDeviceType;
            if (device == null)
            {
                lblNumberValue.Text = string.Empty;
                txtLabel.Text = string.Empty;
                return;
            }

            lblNumberValue.Text = PtObjectRepository.PeekNextNumber(device.Code);
            if (!txtLabel.IsKeyboardFocused)
                txtLabel.Text = $"{device.Code}-{lblNumberValue.Text}";
        }

        private void RefreshTables()
        {
            var tables = PtTableRepository.All.ToList();
            var active = PtTableRepository.ActiveTable;

            cmbTable.ItemsSource = tables;
            cmbTableEdit.ItemsSource = tables.ToList();

            if (active != null)
            {
                SelectComboItem(cmbTable, active.Id);
                SelectComboItem(cmbTableEdit, active.Id);
            }

            RefreshBlockCombo();
            RefreshParentCombo();
            RefreshLinksTab();
            RefreshDevicesGrid();
        }

        private void RefreshBlockCombo()
        {
            var table = CurrentEditTable ?? CurrentTable;
            cmbBlocks.ItemsSource = table != null
                ? PtBlockRepository.GetByTable(table.Id).ToList()
                : new List<PtObjectBlock>();
        }

        private static void SelectComboItem(ComboBox combo, Guid id)
        {
            foreach (var item in combo.Items)
            {
                if (item is PtTableSession t && t.Id == id)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        private static void SelectLinkComboItem(ComboBox combo, Guid objectId)
        {
            foreach (var item in combo.Items)
            {
                if (item is ParentItem parent && parent.Id == objectId)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        private void RefreshParentCombo()
        {
            var table = CurrentTable;
            var items = new List<ParentItem> { new ParentItem(null, "(нет)") };
            if (table != null)
            {
                foreach (var obj in PtObjectRepository.GetByTable(table.Id))
                    items.Add(new ParentItem(obj.InstanceId, obj.Label));
            }
            cmbParent.ItemsSource = items;
        }

        private void RefreshLinksTab()
        {
            treeObjects.Items.Clear();
            lstLinks.Items.Clear();

            var table = CurrentEditTable;
            if (table == null)
                return;

            var root = new TreeViewItem { Header = table.Name, Tag = table.Id, IsExpanded = true };
            var blockNodes = new Dictionary<Guid, TreeViewItem>();

            foreach (var block in PtBlockRepository.GetByTable(table.Id))
            {
                var blockNode = new TreeViewItem { Header = $"[Блок] {block.Name}", Tag = block.Id };
                blockNodes[block.Id] = blockNode;
                root.Items.Add(blockNode);
            }

            var objects = PtObjectRepository.GetByTable(table.Id).ToList();
            var nodeMap = new Dictionary<Guid, TreeViewItem>();

            foreach (var obj in objects)
            {
                var parent = root;
                if (obj.BlockGroupId.HasValue && blockNodes.TryGetValue(obj.BlockGroupId.Value, out var blockNode))
                    parent = blockNode;

                var node = new TreeViewItem { Header = obj.Label, Tag = obj.InstanceId };
                parent.Items.Add(node);
                nodeMap[obj.InstanceId] = node;
            }

            foreach (var obj in objects.Where(o => o.ParentObjectId.HasValue))
            {
                if (!nodeMap.TryGetValue(obj.ParentObjectId.Value, out var parentNode))
                    continue;
                if (!nodeMap.TryGetValue(obj.InstanceId, out var childNode))
                    continue;

                var oldParent = childNode.Parent as ItemsControl;
                oldParent?.Items.Remove(childNode);
                parentNode.Items.Add(childNode);
            }

            treeObjects.Items.Add(root);
            _suppressTreeSelect = true;
            root.IsSelected = true;
            _suppressTreeSelect = false;

            RefreshBlockCombo();

            var linkItems = new List<ParentItem> { new ParentItem(null, "(выберите)") };
            linkItems.AddRange(objects.Select(o => new ParentItem(o.InstanceId, o.Label)));
            cmbLinkFrom.ItemsSource = linkItems.ToList();
            cmbLinkTo.ItemsSource = linkItems.ToList();

            foreach (var link in PtObjectRepository.AllLinks.Where(l => l.TableId == table.Id))
            {
                var from = PtObjectRepository.Get(link.FromObjectId);
                var to = PtObjectRepository.Get(link.ToObjectId);
                if (from != null && to != null)
                    lstLinks.Items.Add($"{from.Label} → {to.Label}");
            }
        }

        private void RefreshDevicesGrid()
        {
            var table = CurrentEditTable;
            if (table == null)
            {
                dgvDevices.ItemsSource = null;
                return;
            }

            dgvDevices.ItemsSource = PtObjectRepository.GetByTable(table.Id)
                .Select(obj => new DeviceGridRow
                {
                    InstanceId = obj.InstanceId,
                    Label = obj.Label,
                    Code = obj.Code,
                    Number = obj.Number,
                    FullName = obj.FullName,
                    FdCode = obj.FdCode,
                    JsCode = obj.JsCode,
                    BlockName = PtObjectRepository.GetBlockName(obj)
                })
                .ToList();
        }

        private void HighlightGridRow(Guid objectId)
        {
            if (dgvDevices.ItemsSource is IEnumerable<DeviceGridRow> rows)
            {
                var row = rows.FirstOrDefault(r => r.InstanceId == objectId);
                if (row != null)
                    dgvDevices.SelectedItem = row;
            }
        }

        private Guid? GetSelectedObjectIdFromTree()
        {
            if (treeObjects.SelectedItem is TreeViewItem node && node.Tag is Guid id)
            {
                if (PtObjectRepository.Get(id) != null)
                    return id;
            }
            return null;
        }

        private double GetFontSize()
        {
            return double.TryParse(txtFontSize.Text, out var size) && size > 0
                ? size
                : PtLayoutConstants.DefaultTextHeight;
        }

        private bool TryAddObject(Guid? parentId, Guid? blockGroupId)
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return false;

            var table = CurrentTable;
            if (table == null)
            {
                MessageBox.Show("Сначала создайте или выберите таблицу.", "ПТ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var device = CurrentDeviceType;
            if (device == null)
                return false;

            HideForDrawingAction();
            var insertionPoint = ObjectSelectionService.PickInsertionPoint(doc.Editor);
            RestoreAfterDrawingAction();

            if (!insertionPoint.HasValue)
                return false;

            var request = new PlaceDeviceRequest
            {
                TableId = table.Id,
                DeviceType = device,
                BlockTemplate = CurrentBlock,
                Number = PtObjectRepository.GetNextNumber(device.Code),
                FontSize = GetFontSize(),
                CustomLabel = txtLabel.Text,
                ParentObjectId = parentId,
                BlockGroupId = blockGroupId,
                InsertionPoint = insertionPoint.Value
            };

            try
            {
                DocumentLockHelper.Run((lockedDoc, db) =>
                {
                    var ptObject = PtLayoutManager.AddDevice(db, request);
                    lockedDoc.Editor.WriteMessage(
                        $"\nОбъект {ptObject.Label} добавлен в \"{table.Name}\".");
                    lockedDoc.Editor.Regen();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            UpdateNumberAndPreview();
            RefreshTables();
            return true;
        }

        private void CmbTable_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbTable.SelectedItem is PtTableSession table)
                PtTableRepository.SetActive(table.Id);
            RefreshParentCombo();
            RefreshBlockCombo();
        }

        private void CmbTableEdit_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshDevicesGrid();
            RefreshLinksTab();
        }

        private void CmbDeviceType_OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
            UpdateNumberAndPreview();

        private void TabMain_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabMain.SelectedItem is TabItem tab &&
                (tab.Header as string == "Связи" || tab.Header as string == "Таблицы"))
                RefreshTables();
        }

        private void BtnNewTable_OnClick(object sender, RoutedEventArgs e)
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            HideForDrawingAction();
            var result = doc.Editor.GetPoint("\nУкажите точку вставки таблицы:");
            RestoreAfterDrawingAction();

            if (result.Status != PromptStatus.OK)
                return;

            DocumentLockHelper.Run((lockedDoc, db) =>
            {
                var session = PtLayoutManager.CreateTableAtPoint(db, result.Value);
                lockedDoc.Editor.WriteMessage($"\nТаблица \"{session.Name}\" создана.");
            });

            RefreshTables();
        }

        private void BtnAdd_OnClick(object sender, RoutedEventArgs e)
        {
            Guid? parentId = (cmbParent.SelectedItem as ParentItem)?.Id;
            TryAddObject(parentId, null);
        }

        private void BtnTreeAdd_OnClick(object sender, RoutedEventArgs e)
        {
            TryAddObject(GetSelectedObjectIdFromTree(), null);
        }

        private void BtnTreeDelete_OnClick(object sender, RoutedEventArgs e)
        {
            var objectId = GetSelectedObjectIdFromTree();
            if (!objectId.HasValue)
            {
                MessageBox.Show("Выберите объект в дереве.", "Удаление",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var obj = PtObjectRepository.Get(objectId.Value);
            if (obj == null)
                return;

            if (MessageBox.Show($"Удалить объект {obj.Label}?", "Удаление",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            DocumentLockHelper.Run((lockedDoc, db) =>
            {
                PtLayoutManager.DeleteObject(db, obj);
                lockedDoc.Editor.Regen();
            });

            ObjectSelectionService.ClearSelection();
            RefreshTables();
        }

        private void BtnTreeMoveUp_OnClick(object sender, RoutedEventArgs e) =>
            MoveSelectedTreeNode(-1);

        private void BtnTreeMoveDown_OnClick(object sender, RoutedEventArgs e) =>
            MoveSelectedTreeNode(1);

        private void TreeObjects_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _treeDragStart = e.GetPosition(null);
            _draggedTreeNode = GetTreeViewItemFromSource(e.OriginalSource as DependencyObject);
        }

        private void TreeObjects_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedTreeNode == null)
                return;

            var pos = e.GetPosition(null);
            if (Math.Abs(pos.X - _treeDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(pos.Y - _treeDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            if (!IsObjectTreeNode(_draggedTreeNode))
                return;

            var data = new DataObject(TreeDragFormat, _draggedTreeNode);
            DragDrop.DoDragDrop(treeObjects, data, DragDropEffects.Move);
        }

        private void TreeObjects_OnDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(TreeDragFormat))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            var dragged = e.Data.GetData(TreeDragFormat) as TreeViewItem;
            var target = GetTreeViewItemFromSource(e.OriginalSource as DependencyObject);
            if (dragged == null || target == null || dragged == target || IsDescendant(dragged, target))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void TreeObjects_OnDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(TreeDragFormat))
                return;

            var dragged = e.Data.GetData(TreeDragFormat) as TreeViewItem;
            var target = GetTreeViewItemFromSource(e.OriginalSource as DependencyObject);
            if (dragged == null || target == null || dragged == target || IsDescendant(dragged, target))
                return;

            if (!TryMoveTreeNode(dragged, target))
                return;

            e.Handled = true;
            ApplyTreeOrderToTable();
            RefreshDevicesGrid();
        }

        private void MoveSelectedTreeNode(int direction)
        {
            if (!(treeObjects.SelectedItem is TreeViewItem node) || !IsObjectTreeNode(node))
                return;

            var parent = ItemsControl.ItemsControlFromItemContainer(node);
            if (parent == null)
                return;

            var index = parent.Items.IndexOf(node);
            var newIndex = index + direction;
            if (newIndex < 0 || newIndex >= parent.Items.Count)
                return;

            parent.Items.Remove(node);
            parent.Items.Insert(newIndex, node);
            node.IsSelected = true;
            node.Focus();

            ApplyTreeOrderToTable();
            RefreshDevicesGrid();
        }

        private bool TryMoveTreeNode(TreeViewItem dragged, TreeViewItem target)
        {
            if (!IsObjectTreeNode(dragged))
                return false;

            var draggedObj = GetObjectFromTreeNode(dragged);
            if (draggedObj == null)
                return false;

            var draggedParent = ItemsControl.ItemsControlFromItemContainer(dragged);
            draggedParent?.Items.Remove(dragged);

            if (IsObjectTreeNode(target))
            {
                var targetParent = ItemsControl.ItemsControlFromItemContainer(target);
                if (targetParent == null)
                    return false;

                var targetIndex = targetParent.Items.IndexOf(target);
                targetParent.Items.Insert(targetIndex, dragged);
                draggedObj.BlockGroupId = GetBlockIdFromContainer(targetParent);
            }
            else if (IsBlockTreeNode(target))
            {
                target.Items.Add(dragged);
                target.IsExpanded = true;
                if (target.Tag is Guid blockId)
                    draggedObj.BlockGroupId = blockId;
            }
            else if (IsTableRootNode(target))
            {
                target.Items.Add(dragged);
                draggedObj.BlockGroupId = null;
            }
            else
            {
                return false;
            }

            dragged.IsSelected = true;
            return true;
        }

        private void ApplyTreeOrderToTable()
        {
            var table = CurrentEditTable ?? CurrentTable;
            if (table == null)
                return;

            var order = CollectObjectOrderFromTree();
            if (order.Count == 0)
                return;

            PtObjectRepository.SetColumnOrder(table.Id, order);

            DocumentLockHelper.Run((lockedDoc, db) =>
            {
                PtLayoutManager.SyncTableColumnOrder(db, table.Id);
                lockedDoc.Editor.Regen();
            });
        }

        private List<Guid> CollectObjectOrderFromTree()
        {
            var result = new List<Guid>();
            if (treeObjects.Items.Count > 0 && treeObjects.Items[0] is TreeViewItem root)
                CollectObjectOrder(root, result);
            return result;
        }

        private static void CollectObjectOrder(TreeViewItem node, ICollection<Guid> result)
        {
            foreach (TreeViewItem child in node.Items)
            {
                if (IsObjectTreeNode(child))
                {
                    result.Add((Guid)child.Tag);
                    CollectObjectOrder(child, result);
                }
                else if (IsBlockTreeNode(child) || IsTableRootNode(child))
                    CollectObjectOrder(child, result);
            }
        }

        private static bool IsObjectTreeNode(TreeViewItem node) =>
            node?.Tag is Guid id && PtObjectRepository.Get(id) != null;

        private static bool IsBlockTreeNode(TreeViewItem node) =>
            node?.Tag is Guid id && PtBlockRepository.Get(id) != null;

        private static bool IsTableRootNode(TreeViewItem node) =>
            node?.Tag is Guid id && PtTableRepository.Get(id) != null;

        private static PtObject GetObjectFromTreeNode(TreeViewItem node) =>
            node?.Tag is Guid id ? PtObjectRepository.Get(id) : null;

        private static Guid? GetBlockIdFromContainer(ItemsControl container)
        {
            if (container is TreeViewItem item && item.Tag is Guid id && PtBlockRepository.Get(id) != null)
                return id;
            return null;
        }

        private static TreeViewItem GetTreeViewItemFromSource(DependencyObject source)
        {
            while (source != null)
            {
                if (source is TreeViewItem item)
                    return item;
                source = VisualTreeHelper.GetParent(source);
            }
            return null;
        }

        private static bool IsDescendant(TreeViewItem parent, TreeViewItem candidate)
        {
            foreach (TreeViewItem child in parent.Items)
            {
                if (child == candidate || IsDescendant(child, candidate))
                    return true;
            }
            return false;
        }

        private void TreeObjects_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_suppressTreeSelect)
                return;

            var objectId = GetSelectedObjectIdFromTree();
            if (!objectId.HasValue)
                return;

            var obj = PtObjectRepository.Get(objectId.Value);
            if (obj == null)
                return;

            if (tabMain.SelectedItem is TabItem tab && (tab.Header as string) == "Таблицы")
                HighlightGridRow(objectId.Value);

            SelectLinkComboItem(cmbLinkTo, objectId.Value);

            try
            {
                ObjectSelectionService.ActivateOnDrawing(obj);
            }
            catch
            {
                // best-effort
            }
        }

        private void BtnNewBlock_OnClick(object sender, RoutedEventArgs e)
        {
            var table = CurrentEditTable;
            if (table == null)
            {
                MessageBox.Show("Выберите таблицу.", "Блок", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var name = PromptInput("Новый блок", "Имя блока:", "Блок");
            if (string.IsNullOrWhiteSpace(name))
                return;

            PtBlockRepository.Create(table.Id, name);
            RefreshLinksTab();
        }

        private void BtnAddToBlock_OnClick(object sender, RoutedEventArgs e)
        {
            var objectId = GetSelectedObjectIdFromTree();
            if (!objectId.HasValue)
            {
                MessageBox.Show("Выберите объект в дереве.", "Блок",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!(cmbBlocks.SelectedItem is PtObjectBlock block))
            {
                MessageBox.Show("Выберите блок.", "Блок", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var obj = PtObjectRepository.Get(objectId.Value);
            if (obj == null)
                return;

            obj.BlockGroupId = block.Id;
            DocumentLockHelper.Run((lockedDoc, db) =>
            {
                PtLayoutManager.SyncObjectToDrawing(db, obj);
                lockedDoc.Editor.Regen();
            });
            RefreshTables();
        }

        private void BtnLinkFromSelected_OnClick(object sender, RoutedEventArgs e)
        {
            var objectId = GetSelectedObjectIdFromTree();
            if (!objectId.HasValue)
                return;

            SelectLinkComboItem(cmbLinkFrom, objectId.Value);
        }

        private void BtnLinkToSelected_OnClick(object sender, RoutedEventArgs e)
        {
            var objectId = GetSelectedObjectIdFromTree();
            if (!objectId.HasValue)
                return;

            SelectLinkComboItem(cmbLinkTo, objectId.Value);
        }

        private void BtnCreateLink_OnClick(object sender, RoutedEventArgs e)
        {
            var table = CurrentEditTable ?? CurrentTable;
            if (table == null)
                return;

            var fromItem = cmbLinkFrom.SelectedItem as ParentItem;
            var toItem = cmbLinkTo.SelectedItem as ParentItem;
            if (fromItem?.Id == null || toItem?.Id == null)
                return;

            if (fromItem.Id == toItem.Id)
                return;

            try
            {
                DocumentLockHelper.Run((lockedDoc, db) =>
                {
                    PtLayoutManager.CreateObjectLink(db, table.Id, fromItem.Id.Value, toItem.Id.Value);
                    lockedDoc.Editor.Regen();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Связи", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RefreshLinksTab();
        }

        private void DgvDevices_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (!(e.Row.Item is DeviceGridRow row))
                return;

            var obj = PtObjectRepository.Get(row.InstanceId);
            if (obj == null)
                return;

            var columnHeader = e.Column.Header as string;

            dgvDevices.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (columnHeader == "Подпись")
                {
                    obj.Label = row.Label?.Trim() ?? obj.Label;
                    row.Label = obj.Label;
                }
                else
                {
                    obj.Code = row.Code ?? obj.Code;
                    obj.Number = row.Number ?? obj.Number;
                    obj.FullName = row.FullName ?? obj.FullName;
                    obj.FdCode = row.FdCode ?? obj.FdCode;
                    obj.JsCode = row.JsCode ?? obj.JsCode;
                }

                DocumentLockHelper.Run((lockedDoc, db) =>
                {
                    PtLayoutManager.SyncObjectToDrawing(db, obj);
                    lockedDoc.Editor.Regen();
                });
                RefreshLinksTab();
            }));
        }

        private void HideForDrawingAction()
        {
            if (Visibility == System.Windows.Visibility.Visible)
                Hide();
        }

        private void RestoreAfterDrawingAction()
        {
            if (Visibility != System.Windows.Visibility.Visible)
                Show();
        }

        private void BtnClose_OnClick(object sender, RoutedEventArgs e) => Hide();

        private static string PromptInput(string title, string label, string defaultValue)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 320,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };
            var stack = new StackPanel { Margin = new Thickness(12) };
            stack.Children.Add(new TextBlock { Text = label });
            var input = new TextBox { Text = defaultValue, Margin = new Thickness(0, 8, 0, 12) };
            stack.Children.Add(input);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "OK", Width = 70, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancel = new Button { Content = "Отмена", Width = 70, IsCancel = true };
            string result = null;
            ok.Click += (_, __) => { result = input.Text; dialog.DialogResult = true; };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            stack.Children.Add(buttons);
            dialog.Content = stack;
            return dialog.ShowDialog() == true ? result : null;
        }
    }
}
