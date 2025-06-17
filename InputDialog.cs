using System.Windows.Forms;
using System.Drawing;
using System;

namespace WindowManager
{
    public class InputDialog : Form
    {
        private TextBox textBox;
        private Button okButton;
        private Button cancelButton;

        public string InputText => textBox.Text;

        public InputDialog(string prompt, string title = "")
        {
            this.Text = title;
            this.ClientSize = new Size(300, 120);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            var promptLabel = new Label()
            {
                Text = prompt,
                Location = new Point(10, 10),
                AutoSize = true
            };

            textBox = new TextBox()
            {
                Location = new Point(10, 40),
                Width = 280,
                TabIndex = 0
            };

            okButton = new Button()
            {
                Text = "OK",
                Location = new Point(130, 70),
                Width = 75,
                DialogResult = DialogResult.OK,
                TabIndex = 1
            };

            cancelButton = new Button()
            {
                Text = "Cancel",
                Location = new Point(215, 70),
                Width = 75,
                DialogResult = DialogResult.Cancel,
                TabIndex = 2
            };

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;

            this.Controls.Add(promptLabel);
            this.Controls.Add(textBox);
            this.Controls.Add(okButton);
            this.Controls.Add(cancelButton);
        }
    }
}
