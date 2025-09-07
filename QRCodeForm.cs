using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LabelEditorApp
{
    public class QRCodeForm : Form
    {
        public string QRText { get; private set; }
        private TextBox txtQR;
        private Button btnOK;
        private Button btnCancel;

        public QRCodeForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "Enter QR Code Text";
            this.Size = new System.Drawing.Size(400, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Label lbl = new Label() { Text = "QR Code Text:", Location = new System.Drawing.Point(10, 20), AutoSize = true };
            this.Controls.Add(lbl);

            txtQR = new TextBox() { Location = new System.Drawing.Point(110, 15), Width = 250 };
            this.Controls.Add(txtQR);

            btnOK = new Button() { Text = "OK", Location = new System.Drawing.Point(110, 50), DialogResult = DialogResult.OK };
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);

            btnCancel = new Button() { Text = "Cancel", Location = new System.Drawing.Point(210, 50), DialogResult = DialogResult.Cancel };
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            QRText = txtQR.Text;
        }
    }
}
