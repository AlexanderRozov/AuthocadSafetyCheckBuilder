using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Demo.Models;
using Demo.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Demo.ui
{
    public partial class PtMainForm : Form
    {
        private bool _gridUpdating;
        private bool _suppressTreeSelect;

        public PtMainForm()
        {
            InitializeComponent();
            LoadCatalogs();
            RefreshTables();
        }

        private void LoadCatalogs()
        {
            cmbDeviceType.DataSource = DeviceCatalog.GetAll();
            cmbDeviceType.DisplayMember = "Name";

            cmbBlock.DataSource = BlockCatalog.GetAll();
            cmbBlock.DisplayMember = "Name";

            cmbDeviceType.SelectedIndex = 0;
            cmbBlock.SelectedIndex = 0;
            UpdateNumberAndPreview();
        }

        private DeviceType CurrentDeviceType =>
            cmbDeviceType.SelectedItem as DeviceType;

        private BlockTemplate CurrentBlock =>
            cmbBlock.SelectedItem as BlockTemplate;

        private PtTableSession CurrentTable =>
            cmbTable.SelectedItem as PtTableSession;

        private PtTableSession CurrentEditTable =>
            cmbTableEdit.SelectedItem as PtTableSession ?? PtTableRepository.ActiveTable;

        private void UpdateNumberAndPreview()
        {
            var device = CurrentDeviceType;
            if (device == null)
            {
                lblNumberValue.Text = string.Empty;
                lblPreviewValue.Text = string.Empty;
                return;
            }

            lblNumberValue.Text = PtObjectRepository.PeekNextNumber(device.Code);
            lblPreviewValue.Text = $"{device.Code}-{lblNumberValue.Text}";
        }

        private void RefreshTables()
        {
            var tables = PtTableRepository.All.ToList();
            var active = PtTableRepository.ActiveTable;

            cmbTable.DataSource = null;
            cmbTable.DataSource = tables;
            cmbTable.DisplayMember = "Name";

            cmbTableEdit.DataSource = null;
            cmbTableEdit.DataSource = tables.ToList();
            cmbTableEdit.DisplayMember = "Name";

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
            var blocks = table != null
                ? PtBlockRepository.GetByTable(table.Id).ToList()
                : new List<PtObjectBlock>();

            cmbBlocks.DataSource = null;
            cmbBlocks.DataSource = blocks;
            cmbBlocks.DisplayMember = "Name";
        }

        private static void SelectComboItem(ComboBox combo, Guid id)
        {
            for (var i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is PtTableSession t && t.Id == id)
                {
                    combo.SelectedIndex = i;
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

            cmbParent.DataSource = items;
            cmbParent.DisplayMember = "Label";
            cmbParent.ValueMember = "Id";
        }

        private void RefreshLinksTab()
        {
            treeObjects.Nodes.Clear();
            lstLinks.Items.Clear();

            var table = CurrentEditTable;
            if (table == null)
                return;

            var root = treeObjects.Nodes.Add(table.Name);
            root.Tag = table.Id;

            var blockNodes = new Dictionary<Guid, TreeNode>();
            foreach (var block in PtBlockRepository.GetByTable(table.Id))
            {
                var blockNode = root.Nodes.Add($"[Блок] {block.Name}");
                blockNode.Tag = block.Id;
                blockNodes[block.Id] = blockNode;
            }

            var objects = PtObjectRepository.GetByTable(table.Id).ToList();
            var nodeMap = new Dictionary<Guid, TreeNode>();

            foreach (var obj in objects)
            {
                TreeNode parentForObject = root;
                if (obj.BlockGroupId.HasValue && blockNodes.TryGetValue(obj.BlockGroupId.Value, out var blockNode))
                    parentForObject = blockNode;

                var node = parentForObject.Nodes.Add(obj.Label);
                node.Tag = obj.InstanceId;
                nodeMap[obj.InstanceId] = node;
            }

            foreach (var obj in objects.Where(o => o.ParentObjectId.HasValue))
            {
                if (!nodeMap.TryGetValue(obj.ParentObjectId.Value, out var parentNode))
                    continue;
                if (!nodeMap.TryGetValue(obj.InstanceId, out var childNode))
                    continue;

                childNode.Remove();
                parentNode.Nodes.Add(childNode);
            }

            root.Expand();

            _suppressTreeSelect = true;
            treeObjects.SelectedNode = root;
            _suppressTreeSelect = false;

            RefreshBlockCombo();

            var linkItems = new List<ParentItem> { new ParentItem(null, "(выберите)") };
            linkItems.AddRange(objects.Select(o => new ParentItem(o.InstanceId, o.Label)));

            cmbLinkFrom.DataSource = linkItems.ToList();
            cmbLinkFrom.DisplayMember = "Label";
            cmbLinkTo.DataSource = linkItems.ToList();
            cmbLinkTo.DisplayMember = "Label";

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
            _gridUpdating = true;
            dgvDevices.Rows.Clear();

            if (table == null)
            {
                _gridUpdating = false;
                return;
            }

            foreach (var obj in PtObjectRepository.GetByTable(table.Id))
            {
                dgvDevices.Rows.Add(
                    obj.Label,
                    obj.Code,
                    obj.Number,
                    obj.FullName,
                    obj.FdCode,
                    obj.JsCode,
                    PtObjectRepository.GetBlockName(obj));
                dgvDevices.Rows[dgvDevices.Rows.Count - 1].Tag = obj.InstanceId;
            }

            _gridUpdating = false;
        }

        private void HighlightGridRow(Guid objectId)
        {
            foreach (DataGridViewRow row in dgvDevices.Rows)
            {
                if (row.Tag is Guid id && id == objectId)
                {
                    row.Selected = true;
                    dgvDevices.CurrentCell = row.Cells[0];
                    return;
                }
            }
        }

        private Guid? GetSelectedObjectIdFromTree()
        {
            var node = treeObjects.SelectedNode;
            if (node?.Tag is Guid id)
            {
                var obj = PtObjectRepository.Get(id);
                if (obj != null)
                    return id;
            }
            return null;
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
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var device = CurrentDeviceType;
            if (device == null)
                return false;

            Hide();
            var insertionPoint = ObjectSelectionService.PickInsertionPoint(doc.Editor);
            Show();
            BringToFront();

            if (!insertionPoint.HasValue)
                return false;

            var number = PtObjectRepository.GetNextNumber(device.Code);
            var request = new PlaceDeviceRequest
            {
                TableId = table.Id,
                DeviceType = device,
                BlockTemplate = CurrentBlock,
                Number = number,
                FontSize = (double)numFontSize.Value,
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
                    ObjectSelectionService.ActivateOnDrawing(ptObject);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            UpdateNumberAndPreview();
            RefreshTables();
            return true;
        }

        private void cmbTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTable.SelectedItem is PtTableSession table)
                PtTableRepository.SetActive(table.Id);

            RefreshParentCombo();
            RefreshBlockCombo();
        }

        private void cmbTableEdit_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshDevicesGrid();
            RefreshLinksTab();
        }

        private void cmbDeviceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateNumberAndPreview();
        }

        private void tabMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabMain.SelectedTab == tabLinks || tabMain.SelectedTab == tabTables)
                RefreshTables();
        }

        private void btnNewTable_Click(object sender, EventArgs e)
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            Hide();
            var result = doc.Editor.GetPoint("\nУкажите точку вставки таблицы:");
            Show();

            if (result.Status != PromptStatus.OK)
                return;

            DocumentLockHelper.Run((lockedDoc, db) =>
            {
                var session = PtLayoutManager.CreateTableAtPoint(db, result.Value);
                lockedDoc.Editor.WriteMessage($"\nТаблица \"{session.Name}\" создана.");
            });

            RefreshTables();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            Guid? parentId = null;
            if (cmbParent.SelectedItem is ParentItem parentItem)
                parentId = parentItem.Id;

            TryAddObject(parentId, null);
        }

        private void btnTreeAdd_Click(object sender, EventArgs e)
        {
            var parentId = GetSelectedObjectIdFromTree();
            tabMain.SelectedTab = tabPlace;
            TryAddObject(parentId, null);
        }

        private void btnTreeDelete_Click(object sender, EventArgs e)
        {
            var objectId = GetSelectedObjectIdFromTree();
            if (!objectId.HasValue)
            {
                MessageBox.Show("Выберите объект в дереве.", "Удаление",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var obj = PtObjectRepository.Get(objectId.Value);
            if (obj == null)
                return;

            if (MessageBox.Show($"Удалить объект {obj.Label}?", "Удаление",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            DocumentLockHelper.Run((lockedDoc, db) =>
            {
                PtLayoutManager.DeleteObject(db, obj);
                lockedDoc.Editor.Regen();
            });

            ObjectSelectionService.ClearSelection();
            RefreshTables();
        }

        private void treeObjects_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (_suppressTreeSelect)
                return;

            var objectId = GetSelectedObjectIdFromTree();
            if (!objectId.HasValue)
                return;

            var obj = PtObjectRepository.Get(objectId.Value);
            if (obj == null)
                return;

            tabMain.SelectedTab = tabTables;
            HighlightGridRow(objectId.Value);

            try
            {
                ObjectSelectionService.ActivateOnDrawing(obj);
            }
            catch
            {
                // selection on drawing is best-effort
            }
        }

        private void btnNewBlock_Click(object sender, EventArgs e)
        {
            var table = CurrentEditTable;
            if (table == null)
            {
                MessageBox.Show("Выберите таблицу.", "Блок", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var name = PromptInput("Новый блок", "Имя блока:", "Блок");
            if (string.IsNullOrWhiteSpace(name))
                return;

            PtBlockRepository.Create(table.Id, name);
            RefreshLinksTab();
        }

        private void btnAddToBlock_Click(object sender, EventArgs e)
        {
            var objectId = GetSelectedObjectIdFromTree();
            if (!objectId.HasValue)
            {
                MessageBox.Show("Выберите объект в дереве.", "Блок",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!(cmbBlocks.SelectedItem is PtObjectBlock block))
            {
                MessageBox.Show("Выберите блок.", "Блок",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void btnCreateLink_Click(object sender, EventArgs e)
        {
            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            var table = CurrentEditTable;
            if (table == null)
                return;

            var fromItem = cmbLinkFrom.SelectedItem as ParentItem;
            var toItem = cmbLinkTo.SelectedItem as ParentItem;
            if (fromItem?.Id == null || toItem?.Id == null)
            {
                MessageBox.Show("Выберите объекты для связи.", "Связи",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (fromItem.Id == toItem.Id)
                return;

            var fromObj = PtObjectRepository.Get(fromItem.Id.Value);
            var toObj = PtObjectRepository.Get(toItem.Id.Value);
            if (fromObj == null || toObj == null)
                return;

            DocumentLockHelper.Run((lockedDoc, db) =>
            {
                using (var tr = db.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                    var ms = (BlockTableRecord)tr.GetObject(
                        bt[BlockTableRecord.ModelSpace],
                        OpenMode.ForWrite);

                    var link = PtObjectRepository.AddLink(table.Id, fromItem.Id.Value, toItem.Id.Value);
                    toObj.ParentObjectId = fromItem.Id;
                    link.ArrowId = DrawingService.DrawArrow(tr, ms, fromObj.Center, toObj.Center);
                    tr.Commit();
                }

                lockedDoc.Editor.Regen();
            });

            RefreshLinksTab();
        }

        private void dgvDevices_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_gridUpdating || e.RowIndex < 0)
                return;

            var row = dgvDevices.Rows[e.RowIndex];
            if (!(row.Tag is Guid objectId))
                return;

            var obj = PtObjectRepository.Get(objectId);
            if (obj == null)
                return;

            obj.Code = row.Cells[1].Value?.ToString() ?? obj.Code;
            obj.Number = row.Cells[2].Value?.ToString() ?? obj.Number;
            obj.FullName = row.Cells[3].Value?.ToString() ?? obj.FullName;
            obj.FdCode = row.Cells[4].Value?.ToString() ?? obj.FdCode;
            obj.JsCode = row.Cells[5].Value?.ToString() ?? obj.JsCode;
            obj.Label = $"{obj.Code}-{obj.Number}";

            DocumentLockHelper.Run((lockedDoc, db) =>
            {
                PtLayoutManager.SyncObjectToDrawing(db, obj);
                lockedDoc.Editor.Regen();
            });

            row.Cells[0].Value = obj.Label;
            RefreshLinksTab();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private static string PromptInput(string title, string label, string defaultValue)
        {
            using (var form = new Form())
            using (var txt = new TextBox())
            using (var lbl = new Label())
            using (var btnOk = new Button())
            using (var btnCancel = new Button())
            {
                form.Text = title;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.ClientSize = new System.Drawing.Size(300, 100);
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                lbl.Text = label;
                lbl.SetBounds(10, 10, 280, 20);
                txt.Text = defaultValue;
                txt.SetBounds(10, 35, 280, 20);
                btnOk.Text = "OK";
                btnOk.DialogResult = DialogResult.OK;
                btnOk.SetBounds(130, 65, 80, 25);
                btnCancel.Text = "Отмена";
                btnCancel.DialogResult = DialogResult.Cancel;
                btnCancel.SetBounds(220, 65, 70, 25);

                form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                return form.ShowDialog() == DialogResult.OK ? txt.Text : null;
            }
        }

        private class ParentItem
        {
            public Guid? Id { get; }
            public string Label { get; }

            public ParentItem(Guid? id, string label)
            {
                Id = id;
                Label = label;
            }
        }
    }
}
