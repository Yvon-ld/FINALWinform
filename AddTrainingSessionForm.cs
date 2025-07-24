using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BasketballAnalyzer.Models;

namespace BasketballAnalyzer.Forms
{
    public partial class AddTrainingSessionForm : Form
    {
        public TrainingSession NewSession { get; private set; }
        public List<ShotRecord> NewShots { get; private set; } = new List<ShotRecord>();
        private int playerId;

        public AddTrainingSessionForm(int playerId)
        {
            this.playerId = playerId;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "添加训练数据";
            Size = new Size(500, 500);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;

            var dateLabel = new Label { Text = "训练日期：", Left = 30, Top = 30, Width = 80 };
            var datePicker = new DateTimePicker { Left = 120, Top = 25, Width = 200, Name = "DatePicker" };

            var durationLabel = new Label { Text = "时长(分钟)：", Left = 30, Top = 70, Width = 80 };
            var durationBox = new NumericUpDown { Left = 120, Top = 65, Width = 200, Minimum = 10, Maximum = 300, Name = "DurationBox" };

            var fatigueLabel = new Label { Text = "疲劳程度(1-10)：", Left = 30, Top = 110, Width = 100 };
            var fatigueBox = new NumericUpDown { Left = 140, Top = 105, Width = 180, Minimum = 1, Maximum = 10, Name = "FatigueBox" };

            var notesLabel = new Label { Text = "备注：", Left = 30, Top = 150, Width = 80 };
            var notesBox = new TextBox { Left = 120, Top = 145, Width = 300, Height = 40, Multiline = true, Name = "NotesBox" };

            var shotsLabel = new Label { Text = "投篮明细（可选）：", Left = 30, Top = 200, Width = 120 };
            var addShotButton = new Button { Text = "添加投篮", Left = 160, Top = 195, Width = 100, BackColor = Color.Orange, ForeColor = Color.White };
            var shotsList = new ListBox { Left = 30, Top = 230, Width = 420, Height = 120, Name = "ShotsList" };

            addShotButton.Click += (s, e) =>
            {
                var shotForm = new AddShotRecordForm(playerId);
                if (shotForm.ShowDialog() == DialogResult.OK)
                {
                    NewShots.Add(shotForm.NewShot);
                    shotsList.Items.Add($"[{shotForm.NewShot.ShotTime:HH:mm}] X:{shotForm.NewShot.CourtX:F1} Y:{shotForm.NewShot.CourtY:F1} {(shotForm.NewShot.ShotResult ? "命中" : "未中")}");
                }
            };

            var okButton = new Button { Text = "保存", Left = 120, Top = 370, Width = 100, DialogResult = DialogResult.OK, BackColor = Color.Green, ForeColor = Color.White };
            var cancelButton = new Button { Text = "取消", Left = 260, Top = 370, Width = 100, DialogResult = DialogResult.Cancel, BackColor = Color.Gray, ForeColor = Color.White };

            okButton.Click += (s, e) =>
            {
                NewSession = new TrainingSession
                {
                    PlayerID = playerId,
                    SessionDate = datePicker.Value,
                    Duration = (int)durationBox.Value,
                    FatigueLevel = (int)fatigueBox.Value,
                    Notes = notesBox.Text
                };
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.AddRange(new Control[] { dateLabel, datePicker, durationLabel, durationBox, fatigueLabel, fatigueBox, notesLabel, notesBox, shotsLabel, addShotButton, shotsList, okButton, cancelButton });
        }
    }

    // 支持输入姿势参数的投篮录入窗体
    public class AddShotRecordForm : Form
    {
        public ShotRecord NewShot { get; private set; }
        private int playerId;

        public AddShotRecordForm(int playerId)
        {
            this.playerId = playerId;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "添加投篮记录";
            Size = new Size(400, 500);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;

            int y = 20;
            int dy = 40;

            var timeLabel = new Label { Text = "时间：", Left = 20, Top = y, Width = 60 };
            var timePicker = new DateTimePicker { Left = 120, Top = y - 5, Width = 200, Format = DateTimePickerFormat.Time, ShowUpDown = true };
            y += dy;

            var xLabel = new Label { Text = "X坐标：", Left = 20, Top = y, Width = 60 };
            var xBox = new NumericUpDown { Left = 120, Top = y - 5, Width = 200, Minimum = 0, Maximum = 28, DecimalPlaces = 1, Increment = 0.1M };
            y += dy;

            var yLabel = new Label { Text = "Y坐标：", Left = 20, Top = y, Width = 60 };
            var yBox = new NumericUpDown { Left = 120, Top = y - 5, Width = 200, Minimum = 0, Maximum = 15, DecimalPlaces = 1, Increment = 0.1M };
            y += dy;

            var resultLabel = new Label { Text = "是否命中：", Left = 20, Top = y, Width = 80 };
            var resultBox = new CheckBox { Left = 120, Top = y - 5, Width = 80, Text = "命中" };
            y += dy;

            var typeLabel = new Label { Text = "投篮类型：", Left = 20, Top = y, Width = 80 };
            var typeBox = new ComboBox { Left = 120, Top = y - 5, Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            typeBox.Items.AddRange(new string[] { "三分", "两分", "罚球" });
            y += dy;

            // 新增姿势参数
            var kneeLabel = new Label { Text = "膝盖弯曲角度：", Left = 20, Top = y, Width = 100 };
            var kneeBox = new NumericUpDown { Left = 120, Top = y - 5, Width = 200, Minimum = 100, Maximum = 180, DecimalPlaces = 1, Increment = 0.1M };
            y += dy;

            var shotAngleLabel = new Label { Text = "出手角度：", Left = 20, Top = y, Width = 100 };
            var shotAngleBox = new NumericUpDown { Left = 120, Top = y - 5, Width = 200, Minimum = 0, Maximum = 90, DecimalPlaces = 1, Increment = 0.1M };
            y += dy;

            var elbowLabel = new Label { Text = "肘部角度：", Left = 20, Top = y, Width = 100 };
            var elbowBox = new NumericUpDown { Left = 120, Top = y - 5, Width = 200, Minimum = 0, Maximum = 180, DecimalPlaces = 1, Increment = 0.1M };
            y += dy;

            var wristLabel = new Label { Text = "手腕下压角度：", Left = 20, Top = y, Width = 100 };
            var wristBox = new NumericUpDown { Left = 120, Top = y - 5, Width = 200, Minimum = 0, Maximum = 90, DecimalPlaces = 1, Increment = 0.1M };
            y += dy;

            var okButton = new Button { Text = "保存", Left = 80, Top = y, Width = 100, DialogResult = DialogResult.OK, BackColor = Color.Green, ForeColor = Color.White };
            var cancelButton = new Button { Text = "取消", Left = 220, Top = y, Width = 100, DialogResult = DialogResult.Cancel, BackColor = Color.Gray, ForeColor = Color.White };

            okButton.Click += (s, e) =>
            {
                NewShot = new ShotRecord
                {
                    PlayerID = playerId,
                    ShotTime = DateTime.Today.Add(timePicker.Value.TimeOfDay),
                    CourtX = (double)xBox.Value,
                    CourtY = (double)yBox.Value,
                    ShotResult = resultBox.Checked,
                    ShotType = typeBox.SelectedItem?.ToString() ?? "两分",
                    KneeFlexion = (double)kneeBox.Value,
                    ShotAngle = (double)shotAngleBox.Value,
                    ElbowAngle = (double)elbowBox.Value,
                    WristSnap = (double)wristBox.Value
                };
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.AddRange(new Control[]
            {
                timeLabel, timePicker, xLabel, xBox, yLabel, yBox, resultLabel, resultBox, typeLabel, typeBox,
                kneeLabel, kneeBox, shotAngleLabel, shotAngleBox, elbowLabel, elbowBox, wristLabel, wristBox,
                okButton, cancelButton
            });
        }
    }
}