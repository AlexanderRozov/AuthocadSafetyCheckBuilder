using Demo.Models;
using Demo.Services;
using Demo.Services.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FormsKeys = System.Windows.Forms.Keys;

namespace Demo.ui
{
    public partial class PtMainWindow : Window
    {
        private bool _suppressTableSync;
        private Point _treeDragStart;
        private TreeViewItem _draggedTreeNode;
        private const string TreeDragFormat = "PtTreeObject";

        public PtMainWindow()
        {
            InitializeComponent();
            PreviewKeyDown += PtMainWindow_OnPreviewKeyDown;
            Closing += (_, e) =>
            {
                e.Cancel = true;
                Hide();
            };
            PtDocumentRegistry.DocumentDataLoaded += OnDocumentDataLoaded;
            LoadCatalogs();
            InitDetectorTab();
            InitTemplatesTab();
            InitHotkeysTab();
            RefreshTables();
            ApplyPendingHotkeySelection();
        }

        private void PtMainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!HotkeyServices.InputController.IsArmed)
                return;

            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt || e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftShift || e.Key == Key.RightShift)
                return;

            var wpfKey = e.Key == Key.System ? e.SystemKey : e.Key;
            var virtualKey = System.Windows.Input.KeyInterop.VirtualKeyFromKey(wpfKey);
            if (HotkeyServices.InputController.TryProcessKey((FormsKeys)virtualKey))
                e.Handled = true;
        }

        public void ReloadFromDocument()
        {
            RefreshTables();
            RefreshTemplatesTab();
            ApplyPendingHotkeySelection();
        }

        public void AfterDrawingInteraction(bool success, bool refreshTemplates = false)
        {
            if (Visibility != System.Windows.Visibility.Visible)
                Show();

            if (success)
            {
                UpdateNumberAndPreview();
                RefreshTables();
                if (refreshTemplates)
                    RefreshTemplatesTab();
            }
        }

        private static void SendPickCommand(string commandName) =>
            PtInteractionScheduler.RunCommandWhenIdle(commandName);

        private void OnDocumentDataLoaded(Autodesk.AutoCAD.ApplicationServices.Document doc)
        {
            Dispatcher.BeginInvoke(new Action(RefreshTables));
        }

        private void LoadCatalogs()
        {
            cmbDeviceType.ItemsSource = PtServiceRegistry.DeviceCatalog.GetAll();
            RefreshBlockCatalog();
            cmbDeviceType.SelectedIndex = 0;
            UpdateNumberAndPreview();
        }

        internal void RefreshBlockCatalog()
        {
            var items = BlockCatalog.GetAllIncludingPrecreated();
            var selected = cmbBlock.SelectedItem as BlockTemplate;
            cmbBlock.ItemsSource = items;
            if (selected != null)
            {
                var match = items.FirstOrDefault(i => i.Id == selected.Id);
                if (match != null)
                    cmbBlock.SelectedItem = match;
                else if (items.Count > 0)
                    cmbBlock.SelectedIndex = 0;
            }
            else if (items.Count > 0)
            {
                cmbBlock.SelectedIndex = 0;
            }
        }

        private DeviceType CurrentDeviceType => cmbDeviceType.SelectedItem as DeviceType;
        private BlockTemplate CurrentBlock => cmbBlock.SelectedItem as BlockTemplate;
        private PtTableSession CurrentTable => cmbTable.SelectedItem as PtTableSession;
        private PtTableSession CurrentEditTable =>
            cmbTableEdit.SelectedItem as PtTableSession ?? PtTableRepository.ActiveTable;

        private PtTableSession GetLinksTable() =>
            CurrentEditTable ?? CurrentTable ?? PtTableRepository.ActiveTable;

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

            _suppressTableSync = true;
            try
            {
                cmbTable.ItemsSource = tables;
                cmbTableEdit.ItemsSource = tables.ToList();

                if (active != null)
                {
                    SelectComboItem(cmbTable, active.Id);
                    SelectComboItem(cmbTableEdit, active.Id);
                }
            }
            finally
            {
                _suppressTableSync = false;
            }

            RefreshBlockCombo();
            RefreshParentCombo();
            RefreshLinksTab();
            RefreshDevicesGrid();
            RefreshDetectorTab();
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

            var table = GetLinksTable();
            RefreshLinkCombos(table);

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

            foreach (var obj in OrderObjectsForTree(objects))
            {
                ItemsControl parent = root;

                if (obj.ParentObjectId.HasValue &&
                    nodeMap.TryGetValue(obj.ParentObjectId.Value, out var parentNode))
                {
                    parent = parentNode;
                }
                else if (obj.BlockGroupId.HasValue &&
                         blockNodes.TryGetValue(obj.BlockGroupId.Value, out var blockNode))
                {
                    parent = blockNode;
                }

                var label = string.IsNullOrWhiteSpace(obj.Label)
                    ? $"{obj.Code}-{obj.Number}"
                    : obj.Label;
                var node = new TreeViewItem { Header = label, Tag = obj.InstanceId };
                parent.Items.Add(node);
                nodeMap[obj.InstanceId] = node;
            }

            treeObjects.Items.Add(root);

            foreach (var link in PtObjectRepository.AllLinks.Where(l => l.TableId == table.Id))
            {
                var from = PtObjectRepository.Get(link.FromObjectId);
                var to = PtObjectRepository.Get(link.ToObjectId);
                if (from != null && to != null)
                {
                    var fromLabel = string.IsNullOrWhiteSpace(from.Label) ? from.Code : from.Label;
                    var toLabel = string.IsNullOrWhiteSpace(to.Label) ? to.Code : to.Label;
                    lstLinks.Items.Add($"{fromLabel} → {toLabel}");
                }
            }
        }

        private static List<PtObject> OrderObjectsForTree(IList<PtObject> objects)
        {
            var byId = objects.ToDictionary(o => o.InstanceId);
            var result = new List<PtObject>();
            var added = new HashSet<Guid>();
            var remaining = objects.ToList();

            while (remaining.Count > 0)
            {
                var progressed = false;
                foreach (var obj in remaining.ToList())
                {
                    var parentReady = !obj.ParentObjectId.HasValue ||
                                      !byId.ContainsKey(obj.ParentObjectId.Value) ||
                                      added.Contains(obj.ParentObjectId.Value);

                    if (!parentReady)
                        continue;

                    result.Add(obj);
                    added.Add(obj.InstanceId);
                    remaining.Remove(obj);
                    progressed = true;
                }

                if (!progressed)
                {
                    result.AddRange(remaining);
                    break;
                }
            }

            return result;
        }

        private void RefreshLinkCombos(PtTableSession table)
        {
            var linkItems = new List<ParentItem> { new ParentItem(null, "(выберите)") };

            if (table != null)
            {
                foreach (var obj in PtObjectRepository.GetByTable(table.Id))
                {
                    var label = string.IsNullOrWhiteSpace(obj.Label)
                        ? $"{obj.Code}-{obj.Number}"
                        : obj.Label;
                    linkItems.Add(new ParentItem(obj.InstanceId, label));
                }
            }

            cmbLinkFrom.ItemsSource = linkItems;
            cmbLinkTo.ItemsSource = linkItems.ToList();

            if (cmbLinkFrom.SelectedItem == null && linkItems.Count > 0)
                cmbLinkFrom.SelectedIndex = 0;
            if (cmbLinkTo.SelectedItem == null && linkItems.Count > 0)
                cmbLinkTo.SelectedIndex = 0;
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

            var request = new PlaceDeviceRequest
            {
                TableId = table.Id,
                DeviceType = device,
                BlockTemplate = CurrentBlock,
                Number = PtObjectRepository.PeekNextNumber(device.Code),
                FontSize = GetFontSize(),
                CustomLabel = txtLabel.Text,
                ParentObjectId = parentId,
                BlockGroupId = blockGroupId
            };

            PtInteractionSession.BeginAddDevice(request);
            SendPickCommand("PTPICKADD");
            return true;
        }

        private void CmbTable_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressTableSync)
                return;

            if (cmbTable.SelectedItem is PtTableSession table)
            {
                PtTableRepository.SetActive(table.Id);
                _suppressTableSync = true;
                SelectComboItem(cmbTableEdit, table.Id);
                _suppressTableSync = false;
            }

            RefreshParentCombo();
            RefreshBlockCombo();
            RefreshLinksTab();
        }

        private void CmbTableEdit_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressTableSync)
                return;

            if (cmbTableEdit.SelectedItem is PtTableSession table)
            {
                PtTableRepository.SetActive(table.Id);
                _suppressTableSync = true;
                SelectComboItem(cmbTable, table.Id);
                _suppressTableSync = false;
            }

            RefreshDevicesGrid();
            RefreshLinksTab();
        }

        private void CmbDeviceType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateNumberAndPreview();
        }

        private void TabMain_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || !(e.AddedItems[0] is TabItem tab))
                return;

            var header = tab.Header as string;
            if (header == "Связи")
                Dispatcher.BeginInvoke(new Action(RefreshLinksTab));
            else if (header == "Таблицы")
                Dispatcher.BeginInvoke(new Action(RefreshDevicesGrid));
            else if (header == "Расставить извещатели")
                Dispatcher.BeginInvoke(new Action(RefreshDetectorTab));
        }

        private void BtnNewTable_OnClick(object sender, RoutedEventArgs e)
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            PtInteractionSession.BeginCreateTable();
            SendPickCommand("PTPICKNEWTABLE");
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
            var objectId = GetSelectedObjectIdFromTree();
            if (!objectId.HasValue)
                return;

            var obj = PtObjectRepository.Get(objectId.Value);
            if (obj == null)
                return;

            if (tabMain.SelectedItem is TabItem tab && (tab.Header as string) == "Таблицы")
                HighlightGridRow(objectId.Value);

            SelectLinkComboItem(cmbLinkTo, objectId.Value);
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
            PtDocumentRegistry.Save(Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument);
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

            if (PtObjectRepository.WouldCreateParentCycle(fromItem.Id.Value, toItem.Id.Value))
            {
                MessageBox.Show("Нельзя создать циклическую связь (объект уже является родителем).",
                    "Связи", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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

            Dispatcher.BeginInvoke(new Action(RefreshLinksTab));
        }

        private void DgvDevices_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel)
                return;

            if (!(e.Row.Item is DeviceGridRow row))
                return;

            var columnHeader = e.Column.Header as string;
            var editedText = (e.EditingElement as TextBox)?.Text;

            dgvDevices.Dispatcher.BeginInvoke(new Action(() =>
            {
                dgvDevices.CommitEdit(DataGridEditingUnit.Cell, true);
                dgvDevices.CommitEdit(DataGridEditingUnit.Row, true);

                var obj = PtObjectRepository.Get(row.InstanceId);
                if (obj == null)
                    return;

                ApplyGridRowEdit(row, obj, columnHeader, editedText);

                try
                {
                    DocumentLockHelper.Run((lockedDoc, db) =>
                    {
                        PtLayoutManager.SyncObjectToDrawing(db, obj);
                        lockedDoc.Editor.Regen();
                    });
                    RefreshLinksTab();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Синхронизация", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }), DispatcherPriority.Background);
        }

        private static void ApplyGridRowEdit(
            DeviceGridRow row,
            PtObject obj,
            string columnHeader,
            string editedText)
        {
            if (!string.IsNullOrEmpty(editedText))
            {
                switch (columnHeader)
                {
                    case "Подпись":
                        row.Label = editedText.Trim();
                        break;
                    case "Код":
                        row.Code = editedText.Trim();
                        break;
                    case "Номер":
                        row.Number = editedText.Trim();
                        break;
                    case "Наименование":
                        row.FullName = editedText.Trim();
                        break;
                    case "FD":
                        row.FdCode = editedText.Trim();
                        break;
                    case "JS05":
                        row.JsCode = editedText.Trim();
                        break;
                }
            }

            obj.Label = row.Label ?? obj.Label;
            obj.Code = row.Code ?? obj.Code;
            obj.Number = row.Number ?? obj.Number;
            obj.FullName = row.FullName ?? obj.FullName;
            obj.FdCode = row.FdCode ?? obj.FdCode;
            obj.JsCode = row.JsCode ?? obj.JsCode;

            if (columnHeader == "Код" || columnHeader == "Номер")
            {
                obj.Label = $"{obj.Code}-{obj.Number}";
                row.Label = obj.Label;
            }
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
