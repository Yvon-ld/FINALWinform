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
            Text = "添加新球员";
            Size = new System.Drawing.Size(350, 300);
            StartPosition = FormStartPosition.CenterParent;

            var nameLabel = new Label { Text = "姓名：", Left = 20, Top = 20, Width = 60 };
            var nameBox = new TextBox { Left = 90, Top = 20, Width = 200, Name = "NameBox" };

            var ageLabel = new Label { Text = "年龄：", Left = 20, Top = 60, Width = 60 };
            var ageBox = new NumericUpDown { Left = 90, Top = 60, Width = 200, Name = "AgeBox", Minimum = 10, Maximum = 60 };

            var heightLabel = new Label { Text = "身高(cm)：", Left = 20, Top = 100, Width = 60 };
            var heightBox = new NumericUpDown { Left = 90, Top = 100, Width = 200, Name = "HeightBox", Minimum = 100, Maximum = 250, DecimalPlaces = 1, Increment = 0.1M };

            var weightLabel = new Label { Text = "体重(kg)：", Left = 20, Top = 140, Width = 60 };
            var weightBox = new NumericUpDown { Left = 90, Top = 140, Width = 200, Name = "WeightBox", Minimum = 30, Maximum = 150, DecimalPlaces = 1, Increment = 0.1M };

            var positionLabel = new Label { Text = "位置：", Left = 20, Top = 180, Width = 60 };
            var positionBox = new ComboBox { Left = 90, Top = 180, Width = 200, Name = "PositionBox", DropDownStyle = ComboBoxStyle.DropDownList };
            positionBox.Items.AddRange(new string[] { "控球后卫", "得分后卫", "小前锋", "大前锋", "中锋" });

            var okButton = new Button { Text = "确定", Left = 90, Top = 220, Width = 80, DialogResult = DialogResult.OK };
            var cancelButton = new Button { Text = "取消", Left = 210, Top = 220, Width = 80, DialogResult = DialogResult.Cancel };

            okButton.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text))
                {
                    MessageBox.Show("请输入球员姓名！");
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