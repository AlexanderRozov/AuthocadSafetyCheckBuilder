using Demo.Models;
using Demo.Services;
using System;
using System.Windows.Forms;

namespace Demo.ui
{
    public partial class ContextForm : Form
    {
        public PlaceDeviceRequest SelectedDevice { get; private set; }

        public ContextForm()
        {
            InitializeComponent();
            LoadDeviceTypes();
        }

        private void LoadDeviceTypes()
        {
            var devices = DeviceCatalog.GetAll();
            cmbDeviceType.DataSource = devices;
            cmbDeviceType.DisplayMember = "Name";
            cmbDeviceType.SelectedIndex = 0;
            UpdatePreview();
        }

        private DeviceType CurrentDeviceType =>
            cmbDeviceType.SelectedItem as DeviceType;

        private void UpdatePreview()
        {
            var device = CurrentDeviceType;
            if (device == null)
            {
                lblPreviewValue.Text = string.Empty;
                return;
            }

            var number = txtNumber.Text.Trim();
            if (string.IsNullOrEmpty(number))
                lblPreviewValue.Text = device.Code;
            else
                lblPreviewValue.Text = $"{device.Code}-{number}";
        }

        private void cmbDeviceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            var device = CurrentDeviceType;
            if (device != null)
                txtNumber.Text = PtObjectRepository.GetSuggestedNumber(device.Code);

            UpdatePreview();
        }

        private void txtNumber_TextChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            var device = CurrentDeviceType;
            var number = txtNumber.Text.Trim();

            if (device == null)
            {
                MessageBox.Show("Выберите тип объекта.", "Поставить объект ПТ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(number))
            {
                MessageBox.Show("Введите номер объекта.", "Поставить объект ПТ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SelectedDevice = new PlaceDeviceRequest
            {
                DeviceType = device,
                Number = number
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
