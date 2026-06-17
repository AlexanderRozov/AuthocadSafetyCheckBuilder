namespace Demo.ui
{
    partial class PtMainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabPlace = new System.Windows.Forms.TabPage();
            this.lblTable = new System.Windows.Forms.Label();
            this.cmbTable = new System.Windows.Forms.ComboBox();
            this.btnNewTable = new System.Windows.Forms.Button();
            this.lblType = new System.Windows.Forms.Label();
            this.cmbDeviceType = new System.Windows.Forms.ComboBox();
            this.lblBlock = new System.Windows.Forms.Label();
            this.cmbBlock = new System.Windows.Forms.ComboBox();
            this.lblFontSize = new System.Windows.Forms.Label();
            this.numFontSize = new System.Windows.Forms.NumericUpDown();
            this.lblNumber = new System.Windows.Forms.Label();
            this.lblNumberValue = new System.Windows.Forms.Label();
            this.lblPreview = new System.Windows.Forms.Label();
            this.lblPreviewValue = new System.Windows.Forms.Label();
            this.lblParent = new System.Windows.Forms.Label();
            this.cmbParent = new System.Windows.Forms.ComboBox();
            this.btnAdd = new System.Windows.Forms.Button();
            this.tabLinks = new System.Windows.Forms.TabPage();
            this.treeObjects = new System.Windows.Forms.TreeView();
            this.btnTreeAdd = new System.Windows.Forms.Button();
            this.btnTreeDelete = new System.Windows.Forms.Button();
            this.btnNewBlock = new System.Windows.Forms.Button();
            this.cmbBlocks = new System.Windows.Forms.ComboBox();
            this.btnAddToBlock = new System.Windows.Forms.Button();
            this.lblLinkFrom = new System.Windows.Forms.Label();
            this.cmbLinkFrom = new System.Windows.Forms.ComboBox();
            this.lblLinkTo = new System.Windows.Forms.Label();
            this.cmbLinkTo = new System.Windows.Forms.ComboBox();
            this.btnCreateLink = new System.Windows.Forms.Button();
            this.lstLinks = new System.Windows.Forms.ListBox();
            this.tabTables = new System.Windows.Forms.TabPage();
            this.lblTableSelect = new System.Windows.Forms.Label();
            this.cmbTableEdit = new System.Windows.Forms.ComboBox();
            this.dgvDevices = new System.Windows.Forms.DataGridView();
            this.colLabel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colFd = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colJs = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colBlock = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnClose = new System.Windows.Forms.Button();
            this.tabMain.SuspendLayout();
            this.tabPlace.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFontSize)).BeginInit();
            this.tabLinks.SuspendLayout();
            this.tabTables.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDevices)).BeginInit();
            this.SuspendLayout();
            // 
            // tabMain
            // 
            this.tabMain.Controls.Add(this.tabPlace);
            this.tabMain.Controls.Add(this.tabLinks);
            this.tabMain.Controls.Add(this.tabTables);
            this.tabMain.Location = new System.Drawing.Point(12, 12);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(520, 420);
            this.tabMain.TabIndex = 0;
            this.tabMain.SelectedIndexChanged += new System.EventHandler(this.tabMain_SelectedIndexChanged);
            // 
            // tabPlace
            // 
            this.tabPlace.Controls.Add(this.lblTable);
            this.tabPlace.Controls.Add(this.cmbTable);
            this.tabPlace.Controls.Add(this.btnNewTable);
            this.tabPlace.Controls.Add(this.lblType);
            this.tabPlace.Controls.Add(this.cmbDeviceType);
            this.tabPlace.Controls.Add(this.lblBlock);
            this.tabPlace.Controls.Add(this.cmbBlock);
            this.tabPlace.Controls.Add(this.lblFontSize);
            this.tabPlace.Controls.Add(this.numFontSize);
            this.tabPlace.Controls.Add(this.lblNumber);
            this.tabPlace.Controls.Add(this.lblNumberValue);
            this.tabPlace.Controls.Add(this.lblPreview);
            this.tabPlace.Controls.Add(this.lblPreviewValue);
            this.tabPlace.Controls.Add(this.lblParent);
            this.tabPlace.Controls.Add(this.cmbParent);
            this.tabPlace.Controls.Add(this.btnAdd);
            this.tabPlace.Location = new System.Drawing.Point(4, 22);
            this.tabPlace.Name = "tabPlace";
            this.tabPlace.Padding = new System.Windows.Forms.Padding(3);
            this.tabPlace.Size = new System.Drawing.Size(512, 394);
            this.tabPlace.TabIndex = 0;
            this.tabPlace.Text = "Размещение";
            this.tabPlace.UseVisualStyleBackColor = true;
            // 
            // lblTable
            // 
            this.lblTable.AutoSize = true;
            this.lblTable.Location = new System.Drawing.Point(10, 12);
            this.lblTable.Name = "lblTable";
            this.lblTable.Size = new System.Drawing.Size(54, 13);
            this.lblTable.TabIndex = 0;
            this.lblTable.Text = "Таблица:";
            // 
            // cmbTable
            // 
            this.cmbTable.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTable.FormattingEnabled = true;
            this.cmbTable.Location = new System.Drawing.Point(13, 28);
            this.cmbTable.Name = "cmbTable";
            this.cmbTable.Size = new System.Drawing.Size(300, 21);
            this.cmbTable.TabIndex = 1;
            this.cmbTable.SelectedIndexChanged += new System.EventHandler(this.cmbTable_SelectedIndexChanged);
            // 
            // btnNewTable
            // 
            this.btnNewTable.Location = new System.Drawing.Point(319, 26);
            this.btnNewTable.Name = "btnNewTable";
            this.btnNewTable.Size = new System.Drawing.Size(120, 23);
            this.btnNewTable.TabIndex = 2;
            this.btnNewTable.Text = "Новая таблица...";
            this.btnNewTable.UseVisualStyleBackColor = true;
            this.btnNewTable.Click += new System.EventHandler(this.btnNewTable_Click);
            // 
            // lblType
            // 
            this.lblType.AutoSize = true;
            this.lblType.Location = new System.Drawing.Point(10, 58);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(83, 13);
            this.lblType.TabIndex = 3;
            this.lblType.Text = "Тип объекта:";
            // 
            // cmbDeviceType
            // 
            this.cmbDeviceType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDeviceType.FormattingEnabled = true;
            this.cmbDeviceType.Location = new System.Drawing.Point(13, 74);
            this.cmbDeviceType.Name = "cmbDeviceType";
            this.cmbDeviceType.Size = new System.Drawing.Size(426, 21);
            this.cmbDeviceType.TabIndex = 4;
            this.cmbDeviceType.SelectedIndexChanged += new System.EventHandler(this.cmbDeviceType_SelectedIndexChanged);
            // 
            // lblBlock
            // 
            this.lblBlock.AutoSize = true;
            this.lblBlock.Location = new System.Drawing.Point(10, 104);
            this.lblBlock.Name = "lblBlock";
            this.lblBlock.Size = new System.Drawing.Size(41, 13);
            this.lblBlock.TabIndex = 5;
            this.lblBlock.Text = "Блок:";
            // 
            // cmbBlock
            // 
            this.cmbBlock.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBlock.FormattingEnabled = true;
            this.cmbBlock.Location = new System.Drawing.Point(13, 120);
            this.cmbBlock.Name = "cmbBlock";
            this.cmbBlock.Size = new System.Drawing.Size(426, 21);
            this.cmbBlock.TabIndex = 6;
            this.cmbBlock.SelectedIndexChanged += new System.EventHandler(this.cmbDeviceType_SelectedIndexChanged);
            // 
            // lblFontSize
            // 
            this.lblFontSize.AutoSize = true;
            this.lblFontSize.Location = new System.Drawing.Point(10, 150);
            this.lblFontSize.Name = "lblFontSize";
            this.lblFontSize.Size = new System.Drawing.Size(90, 13);
            this.lblFontSize.TabIndex = 7;
            this.lblFontSize.Text = "Размер шрифта:";
            // 
            // numFontSize
            // 
            this.numFontSize.DecimalPlaces = 1;
            this.numFontSize.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.numFontSize.Location = new System.Drawing.Point(13, 166);
            this.numFontSize.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numFontSize.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numFontSize.Name = "numFontSize";
            this.numFontSize.Size = new System.Drawing.Size(80, 20);
            this.numFontSize.TabIndex = 8;
            this.numFontSize.Value = new decimal(new int[] { 5, 0, 0, 0 });
            this.numFontSize.ValueChanged += new System.EventHandler(this.cmbDeviceType_SelectedIndexChanged);
            // 
            // lblNumber
            // 
            this.lblNumber.AutoSize = true;
            this.lblNumber.Location = new System.Drawing.Point(110, 150);
            this.lblNumber.Name = "lblNumber";
            this.lblNumber.Size = new System.Drawing.Size(44, 13);
            this.lblNumber.TabIndex = 9;
            this.lblNumber.Text = "Номер:";
            // 
            // lblNumberValue
            // 
            this.lblNumberValue.AutoSize = true;
            this.lblNumberValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblNumberValue.Location = new System.Drawing.Point(110, 166);
            this.lblNumberValue.Name = "lblNumberValue";
            this.lblNumberValue.Size = new System.Drawing.Size(32, 16);
            this.lblNumberValue.TabIndex = 10;
            this.lblNumberValue.Text = "001";
            // 
            // lblPreview
            // 
            this.lblPreview.AutoSize = true;
            this.lblPreview.Location = new System.Drawing.Point(10, 196);
            this.lblPreview.Name = "lblPreview";
            this.lblPreview.Size = new System.Drawing.Size(61, 13);
            this.lblPreview.TabIndex = 11;
            this.lblPreview.Text = "Подпись:";
            // 
            // lblPreviewValue
            // 
            this.lblPreviewValue.AutoSize = true;
            this.lblPreviewValue.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblPreviewValue.Location = new System.Drawing.Point(10, 212);
            this.lblPreviewValue.Name = "lblPreviewValue";
            this.lblPreviewValue.Size = new System.Drawing.Size(0, 16);
            this.lblPreviewValue.TabIndex = 12;
            // 
            // lblParent
            // 
            this.lblParent.AutoSize = true;
            this.lblParent.Location = new System.Drawing.Point(10, 240);
            this.lblParent.Name = "lblParent";
            this.lblParent.Size = new System.Drawing.Size(99, 13);
            this.lblParent.TabIndex = 13;
            this.lblParent.Text = "Связать с (род.):";
            // 
            // cmbParent
            // 
            this.cmbParent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbParent.FormattingEnabled = true;
            this.cmbParent.Location = new System.Drawing.Point(13, 256);
            this.cmbParent.Name = "cmbParent";
            this.cmbParent.Size = new System.Drawing.Size(426, 21);
            this.cmbParent.TabIndex = 14;
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(364, 300);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(120, 30);
            this.btnAdd.TabIndex = 15;
            this.btnAdd.Text = "Добавить";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // tabLinks
            // 
            this.tabLinks.Controls.Add(this.btnTreeAdd);
            this.tabLinks.Controls.Add(this.btnTreeDelete);
            this.tabLinks.Controls.Add(this.btnNewBlock);
            this.tabLinks.Controls.Add(this.cmbBlocks);
            this.tabLinks.Controls.Add(this.btnAddToBlock);
            this.tabLinks.Controls.Add(this.treeObjects);
            this.tabLinks.Controls.Add(this.lblLinkFrom);
            this.tabLinks.Controls.Add(this.cmbLinkFrom);
            this.tabLinks.Controls.Add(this.lblLinkTo);
            this.tabLinks.Controls.Add(this.cmbLinkTo);
            this.tabLinks.Controls.Add(this.btnCreateLink);
            this.tabLinks.Controls.Add(this.lstLinks);
            this.tabLinks.Location = new System.Drawing.Point(4, 22);
            this.tabLinks.Name = "tabLinks";
            this.tabLinks.Padding = new System.Windows.Forms.Padding(3);
            this.tabLinks.Size = new System.Drawing.Size(512, 394);
            this.tabLinks.TabIndex = 1;
            this.tabLinks.Text = "Связи";
            this.tabLinks.UseVisualStyleBackColor = true;
            // 
            // treeObjects
            // 
            this.treeObjects.Location = new System.Drawing.Point(10, 40);
            this.treeObjects.Name = "treeObjects";
            this.treeObjects.Size = new System.Drawing.Size(490, 150);
            this.treeObjects.TabIndex = 3;
            this.treeObjects.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeObjects_AfterSelect);
            // 
            // btnTreeAdd
            // 
            this.btnTreeAdd.Location = new System.Drawing.Point(10, 10);
            this.btnTreeAdd.Name = "btnTreeAdd";
            this.btnTreeAdd.Size = new System.Drawing.Size(90, 25);
            this.btnTreeAdd.TabIndex = 0;
            this.btnTreeAdd.Text = "Добавить";
            this.btnTreeAdd.UseVisualStyleBackColor = true;
            this.btnTreeAdd.Click += new System.EventHandler(this.btnTreeAdd_Click);
            // 
            // btnTreeDelete
            // 
            this.btnTreeDelete.Location = new System.Drawing.Point(106, 10);
            this.btnTreeDelete.Name = "btnTreeDelete";
            this.btnTreeDelete.Size = new System.Drawing.Size(90, 25);
            this.btnTreeDelete.TabIndex = 1;
            this.btnTreeDelete.Text = "Удалить";
            this.btnTreeDelete.UseVisualStyleBackColor = true;
            this.btnTreeDelete.Click += new System.EventHandler(this.btnTreeDelete_Click);
            // 
            // btnNewBlock
            // 
            this.btnNewBlock.Location = new System.Drawing.Point(220, 10);
            this.btnNewBlock.Name = "btnNewBlock";
            this.btnNewBlock.Size = new System.Drawing.Size(90, 25);
            this.btnNewBlock.TabIndex = 2;
            this.btnNewBlock.Text = "Новый блок";
            this.btnNewBlock.UseVisualStyleBackColor = true;
            this.btnNewBlock.Click += new System.EventHandler(this.btnNewBlock_Click);
            // 
            // cmbBlocks
            // 
            this.cmbBlocks.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBlocks.FormattingEnabled = true;
            this.cmbBlocks.Location = new System.Drawing.Point(316, 12);
            this.cmbBlocks.Name = "cmbBlocks";
            this.cmbBlocks.Size = new System.Drawing.Size(110, 21);
            this.cmbBlocks.TabIndex = 4;
            // 
            // btnAddToBlock
            // 
            this.btnAddToBlock.Location = new System.Drawing.Point(432, 10);
            this.btnAddToBlock.Name = "btnAddToBlock";
            this.btnAddToBlock.Size = new System.Drawing.Size(68, 25);
            this.btnAddToBlock.TabIndex = 5;
            this.btnAddToBlock.Text = "В блок";
            this.btnAddToBlock.UseVisualStyleBackColor = true;
            this.btnAddToBlock.Click += new System.EventHandler(this.btnAddToBlock_Click);
            // 
            // lblLinkFrom
            // 
            this.lblLinkFrom.AutoSize = true;
            this.lblLinkFrom.Location = new System.Drawing.Point(10, 200);
            this.lblLinkFrom.Name = "lblLinkFrom";
            this.lblLinkFrom.Size = new System.Drawing.Size(33, 13);
            this.lblLinkFrom.TabIndex = 1;
            this.lblLinkFrom.Text = "От:";
            // 
            // cmbLinkFrom
            // 
            this.cmbLinkFrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLinkFrom.FormattingEnabled = true;
            this.cmbLinkFrom.Location = new System.Drawing.Point(50, 197);
            this.cmbLinkFrom.Name = "cmbLinkFrom";
            this.cmbLinkFrom.Size = new System.Drawing.Size(200, 21);
            this.cmbLinkFrom.TabIndex = 2;
            // 
            // lblLinkTo
            // 
            this.lblLinkTo.AutoSize = true;
            this.lblLinkTo.Location = new System.Drawing.Point(260, 200);
            this.lblLinkTo.Name = "lblLinkTo";
            this.lblLinkTo.Size = new System.Drawing.Size(25, 13);
            this.lblLinkTo.TabIndex = 3;
            this.lblLinkTo.Text = "К:";
            // 
            // cmbLinkTo
            // 
            this.cmbLinkTo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLinkTo.FormattingEnabled = true;
            this.cmbLinkTo.Location = new System.Drawing.Point(290, 197);
            this.cmbLinkTo.Name = "cmbLinkTo";
            this.cmbLinkTo.Size = new System.Drawing.Size(200, 21);
            this.cmbLinkTo.TabIndex = 4;
            // 
            // btnCreateLink
            // 
            this.btnCreateLink.Location = new System.Drawing.Point(380, 230);
            this.btnCreateLink.Name = "btnCreateLink";
            this.btnCreateLink.Size = new System.Drawing.Size(120, 25);
            this.btnCreateLink.TabIndex = 5;
            this.btnCreateLink.Text = "Создать связь";
            this.btnCreateLink.UseVisualStyleBackColor = true;
            this.btnCreateLink.Click += new System.EventHandler(this.btnCreateLink_Click);
            // 
            // lstLinks
            // 
            this.lstLinks.FormattingEnabled = true;
            this.lstLinks.Location = new System.Drawing.Point(10, 265);
            this.lstLinks.Name = "lstLinks";
            this.lstLinks.Size = new System.Drawing.Size(490, 121);
            this.lstLinks.TabIndex = 6;
            // 
            // tabTables
            // 
            this.tabTables.Controls.Add(this.lblTableSelect);
            this.tabTables.Controls.Add(this.cmbTableEdit);
            this.tabTables.Controls.Add(this.dgvDevices);
            this.tabTables.Location = new System.Drawing.Point(4, 22);
            this.tabTables.Name = "tabTables";
            this.tabTables.Size = new System.Drawing.Size(512, 394);
            this.tabTables.TabIndex = 2;
            this.tabTables.Text = "Таблицы";
            this.tabTables.UseVisualStyleBackColor = true;
            // 
            // lblTableSelect
            // 
            this.lblTableSelect.AutoSize = true;
            this.lblTableSelect.Location = new System.Drawing.Point(10, 12);
            this.lblTableSelect.Name = "lblTableSelect";
            this.lblTableSelect.Size = new System.Drawing.Size(54, 13);
            this.lblTableSelect.TabIndex = 0;
            this.lblTableSelect.Text = "Таблица:";
            // 
            // cmbTableEdit
            // 
            this.cmbTableEdit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTableEdit.FormattingEnabled = true;
            this.cmbTableEdit.Location = new System.Drawing.Point(13, 28);
            this.cmbTableEdit.Name = "cmbTableEdit";
            this.cmbTableEdit.Size = new System.Drawing.Size(300, 21);
            this.cmbTableEdit.TabIndex = 1;
            this.cmbTableEdit.SelectedIndexChanged += new System.EventHandler(this.cmbTableEdit_SelectedIndexChanged);
            // 
            // dgvDevices
            // 
            this.dgvDevices.AllowUserToAddRows = false;
            this.dgvDevices.AllowUserToDeleteRows = false;
            this.dgvDevices.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDevices.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colLabel,
            this.colCode,
            this.colNumber,
            this.colName,
            this.colFd,
            this.colJs,
            this.colBlock});
            this.dgvDevices.Location = new System.Drawing.Point(10, 60);
            this.dgvDevices.Name = "dgvDevices";
            this.dgvDevices.Size = new System.Drawing.Size(490, 320);
            this.dgvDevices.TabIndex = 2;
            this.dgvDevices.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvDevices_CellEndEdit);
            // 
            // colLabel
            // 
            this.colLabel.HeaderText = "Подпись";
            this.colLabel.Name = "colLabel";
            this.colLabel.ReadOnly = true;
            this.colLabel.Width = 80;
            // 
            // colCode
            // 
            this.colCode.HeaderText = "Код";
            this.colCode.Name = "colCode";
            this.colCode.Width = 60;
            // 
            // colNumber
            // 
            this.colNumber.HeaderText = "Номер";
            this.colNumber.Name = "colNumber";
            this.colNumber.Width = 60;
            // 
            // colName
            // 
            this.colName.HeaderText = "Наименование";
            this.colName.Name = "colName";
            this.colName.Width = 120;
            // 
            // colFd
            // 
            this.colFd.HeaderText = "FD";
            this.colFd.Name = "colFd";
            this.colFd.Width = 70;
            // 
            // colJs
            // 
            this.colJs.HeaderText = "JS05";
            this.colJs.Name = "colJs";
            this.colJs.Width = 90;
            // 
            // colBlock
            // 
            this.colBlock.HeaderText = "Блок";
            this.colBlock.Name = "colBlock";
            this.colBlock.ReadOnly = true;
            this.colBlock.Width = 80;
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(432, 438);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 25);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // PtMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(544, 475);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.tabMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "PtMainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ПТ — объекты и таблицы";
            this.tabMain.ResumeLayout(false);
            this.tabPlace.ResumeLayout(false);
            this.tabPlace.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numFontSize)).EndInit();
            this.tabLinks.ResumeLayout(false);
            this.tabLinks.PerformLayout();
            this.tabTables.ResumeLayout(false);
            this.tabTables.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDevices)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabPlace;
        private System.Windows.Forms.TabPage tabLinks;
        private System.Windows.Forms.TabPage tabTables;
        private System.Windows.Forms.Label lblTable;
        private System.Windows.Forms.ComboBox cmbTable;
        private System.Windows.Forms.Button btnNewTable;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.ComboBox cmbDeviceType;
        private System.Windows.Forms.Label lblBlock;
        private System.Windows.Forms.ComboBox cmbBlock;
        private System.Windows.Forms.Label lblFontSize;
        private System.Windows.Forms.NumericUpDown numFontSize;
        private System.Windows.Forms.Label lblNumber;
        private System.Windows.Forms.Label lblNumberValue;
        private System.Windows.Forms.Label lblPreview;
        private System.Windows.Forms.Label lblPreviewValue;
        private System.Windows.Forms.Label lblParent;
        private System.Windows.Forms.ComboBox cmbParent;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.TreeView treeObjects;
        private System.Windows.Forms.Label lblLinkFrom;
        private System.Windows.Forms.ComboBox cmbLinkFrom;
        private System.Windows.Forms.Label lblLinkTo;
        private System.Windows.Forms.ComboBox cmbLinkTo;
        private System.Windows.Forms.Button btnCreateLink;
        private System.Windows.Forms.ListBox lstLinks;
        private System.Windows.Forms.Label lblTableSelect;
        private System.Windows.Forms.ComboBox cmbTableEdit;
        private System.Windows.Forms.DataGridView dgvDevices;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLabel;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCode;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colFd;
        private System.Windows.Forms.DataGridViewTextBoxColumn colJs;
        private System.Windows.Forms.DataGridViewTextBoxColumn colBlock;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnTreeAdd;
        private System.Windows.Forms.Button btnTreeDelete;
        private System.Windows.Forms.Button btnNewBlock;
        private System.Windows.Forms.ComboBox cmbBlocks;
        private System.Windows.Forms.Button btnAddToBlock;
    }
}
