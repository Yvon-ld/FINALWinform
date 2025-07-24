using System;
using System.Windows.Forms;
using BasketballAnalyzer.Models;

namespace BasketballAnalyzer.Forms
{
    public partial class AddPlayerForm : Form
    {
        public Player NewPlayer { get; private set; }

        public AddPlayerForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "�������Ա";
            Size = new System.Drawing.Size(350, 300);
            StartPosition = FormStartPosition.CenterParent;

            var nameLabel = new Label { Text = "������", Left = 20, Top = 20, Width = 60 };
            var nameBox = new TextBox { Left = 90, Top = 20, Width = 200, Name = "NameBox" };

            var ageLabel = new Label { Text = "���䣺", Left = 20, Top = 60, Width = 60 };
            var ageBox = new NumericUpDown { Left = 90, Top = 60, Width = 200, Name = "AgeBox", Minimum = 10, Maximum = 60 };

            var heightLabel = new Label { Text = "���(cm)��", Left = 20, Top = 100, Width = 60 };
            var heightBox = new NumericUpDown { Left = 90, Top = 100, Width = 200, Name = "HeightBox", Minimum = 100, Maximum = 250, DecimalPlaces = 1, Increment = 0.1M };

            var weightLabel = new Label { Text = "����(kg)��", Left = 20, Top = 140, Width = 60 };
            var weightBox = new NumericUpDown { Left = 90, Top = 140, Width = 200, Name = "WeightBox", Minimum = 30, Maximum = 150, DecimalPlaces = 1, Increment = 0.1M };

            var positionLabel = new Label { Text = "λ�ã�", Left = 20, Top = 180, Width = 60 };
            var positionBox = new ComboBox { Left = 90, Top = 180, Width = 200, Name = "PositionBox", DropDownStyle = ComboBoxStyle.DropDownList };
            positionBox.Items.AddRange(new string[] { "�������", "�÷ֺ���", "Сǰ��", "��ǰ��", "�з�" });

            var okButton = new Button { Text = "ȷ��", Left = 90, Top = 220, Width = 80, DialogResult = DialogResult.OK };
            var cancelButton = new Button { Text = "ȡ��", Left = 210, Top = 220, Width = 80, DialogResult = DialogResult.Cancel };

            okButton.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    MessageBox.Show("��������Ա������");
                    return;
                }
                NewPlayer = new Player
                {
                    PlayerName = nameBox.Text.Trim(),
                    Age = (int)ageBox.Value,
                    Height = (double)heightBox.Value,
                    Weight = (double)weightBox.Value,
                    Position = positionBox.SelectedItem?.ToString() ?? ""
                };
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.AddRange(new Control[] { nameLabel, nameBox, ageLabel, ageBox, heightLabel, heightBox, weightLabel, weightBox, positionLabel, positionBox, okButton, cancelButton });
        }
    }
}