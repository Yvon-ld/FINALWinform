using BasketballAnalyzer.Models;
using BasketballAnalyzer.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace BasketballAnalyzer.Forms
{
    public partial class MainForm : Form
    {
        private DatabaseService databaseService;
        private AnalysisService analysisService;
        private Player? currentPlayer;

        private TabControl mainTabControl;
        private ComboBox playerComboBox;
        private ToolStripStatusLabel statusLabel;
        private ComboBox fatigueSessionComboBox;

        private Button generateAIButton;
        private Button exportButton;
        private bool aiRecommendationsReady = false;
        private List<TrainingRecommendation> aiRecommendationsCache = new List<TrainingRecommendation>();
        public MainForm()
        {
            InitializeComponent();
            InitializeServices();
            LoadPlayers();
        }

        private void InitializeComponent()
        {
            // 设置窗体属性
            Text = "篮球数据分析系统";
            Size = new Size(1200, 800);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 600);

            // 创建主布局
            CreateMainLayout();
        }

        private void CreateMainLayout()
        {
            // 顶部工具栏
            var toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(240, 240, 240)
            };
            // 用GDI+画一个篮球icon
            var iconBmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(iconBmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                var orange = Color.FromArgb(255, 140, 0);
                g.FillEllipse(new SolidBrush(orange), 2, 2, 28, 28);
                g.DrawEllipse(Pens.Brown, 2, 2, 28, 28);
                g.DrawArc(Pens.Brown, 2, 2, 28, 28, 45, 90);
                g.DrawArc(Pens.Brown, 2, 2, 28, 28, 225, 90);
                g.DrawLine(Pens.Brown, 16, 2, 16, 30);
                g.DrawLine(Pens.Brown, 2, 16, 30, 16);
            }
            var icon = new ToolStripLabel();
            icon.Image = iconBmp;
            toolbar.Items.Add(icon);
            // 球员选择
            var playerLabel = new ToolStripLabel("当前球员：");
            playerComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150
            };
            playerComboBox.SelectedIndexChanged += PlayerComboBox_SelectedIndexChanged;

            var playerComboBoxHost = new ToolStripControlHost(playerComboBox);

            // 添加新球员按钮
            var addPlayerButton = new ToolStripButton("添加球员")
            {
                BackColor = Color.LightBlue
            };
            addPlayerButton.Click += AddPlayerButton_Click;

            // 刷新按钮
            var refreshButton = new ToolStripButton("刷新数据")
            {
                BackColor = Color.Orange,
                ForeColor = Color.White
            };
            refreshButton.Click += RefreshButton_Click;

            toolbar.Items.AddRange(new ToolStripItem[]
            {
                playerLabel,
                playerComboBoxHost,
                new ToolStripSeparator(),
                addPlayerButton,
                refreshButton
            });

            var addDataButton = new ToolStripButton("添加训练数据")
            {
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White
            };
            addDataButton.Click += AddDataButton_Click;

            toolbar.Items.Add(addDataButton);
            // 主Tab控件
            mainTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 9F)
            };

            // 创建各个Tab页面
            CreateDashboardTab();
            CreateHeatMapTab();
            CreatePostureAnalysisTab();
            CreateFatigueAnalysisTab();
            CreateRecommendationsTab();
            CreateSummaryTab();

            // 状态栏
            var statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(240, 240, 240)
            };
            statusLabel = new ToolStripStatusLabel("准备就绪");
            statusStrip.Items.Add(statusLabel);

            // 添加控件到窗体
            Controls.Add(mainTabControl);
            Controls.Add(toolbar);
            Controls.Add(statusStrip);
            // 在MainForm的CreateMainLayout方法中添加：
            mainTabControl.ItemSize = new Size(120, 30);
            mainTabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            mainTabControl.DrawItem += (s, e) =>
            {
                var tabPage = mainTabControl.TabPages[e.Index];
                var tabRect = mainTabControl.GetTabRect(e.Index);

                // 选中状态
                if (mainTabControl.SelectedIndex == e.Index)
                {
                    e.Graphics.FillRectangle(Brushes.White, tabRect);
                    TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font,
                        tabRect, Color.FromArgb(255, 140, 0),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                else
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(240, 240, 240)), tabRect);
                    TextRenderer.DrawText(e.Graphics, tabPage.Text, tabPage.Font,
                        tabRect, Color.Gray,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };
        }

        private void CreateDashboardTab()
        {
            var tabPage = new TabPage("数据概览");

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            // 创建统计信息面板
            var statsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 200,
                ColumnCount = 4,
                RowCount = 2,
                BackColor = Color.FromArgb(250, 250, 250),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };

            // 设置列宽比例
            for (int i = 0; i < 4; i++)
            {
                statsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }
            statsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            statsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // 添加统计卡片
            var statCards = new[]
            {
                ("总投篮数", "0", Color.Blue),
                ("总命中率", "0%", Color.Green),
                ("三分命中率", "0%", Color.Orange),
                ("两分命中率", "0%", Color.Red),
                ("训练场次", "0", Color.Purple),
                ("平均训练时长", "0分钟", Color.Brown),
                ("平均疲劳程度", "0", Color.DarkGreen),
                ("最佳投篮区域", "暂无", Color.DarkBlue)
            };

            for (int i = 0; i < statCards.Length; i++)
            {
                var card = CreateStatCard(statCards[i].Item1, statCards[i].Item2, statCards[i].Item3);
                card.Tag = statCards[i].Item1; // 用于后续更新
                statsPanel.Controls.Add(card, i % 4, i / 4);
            }

            // 最近训练记录列表
            var recentTrainingGroup = new GroupBox
            {
                Text = "最近训练记录",
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                Margin = new Padding(0, 10, 0, 0)
            };

            var trainingListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Font = new Font("Microsoft YaHei", 8F)
            };

            trainingListView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "日期", Width = 120 },
                new ColumnHeader { Text = "时长", Width = 80 },
                new ColumnHeader { Text = "投篮数", Width = 80 },
                new ColumnHeader { Text = "命中数", Width = 80 },
                new ColumnHeader { Text = "命中率", Width = 80 },
                new ColumnHeader { Text = "疲劳程度", Width = 80 },
                new ColumnHeader { Text = "备注", Width = 200 }
            });

            trainingListView.Tag = "TrainingList"; // 用于后续更新
            recentTrainingGroup.Controls.Add(trainingListView);

            panel.Controls.Add(recentTrainingGroup);
            panel.Controls.Add(statsPanel);
            tabPage.Controls.Add(panel);
            mainTabControl.TabPages.Add(tabPage);
        }

        private Panel CreateStatCard(string title, string value, Color accentColor)
        {
            var card = new Panel
            {
                Margin = new Padding(5),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(10)
            };

            // 添加顶部色条
            var accentBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 5,
                BackColor = accentColor
            };

            // 添加阴影效果
            card.Paint += (s, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, card.ClientRectangle,
                    Color.White, 1, ButtonBorderStyle.Solid,
                    Color.White, 1, ButtonBorderStyle.Solid,
                    Color.White, 1, ButtonBorderStyle.Solid,
                    accentColor, 2, ButtonBorderStyle.Solid);

                // 柔和阴影
                using (var shadow = new GraphicsPath())
                {
                    shadow.AddRectangle(new Rectangle(2, 2, card.Width - 4, card.Height - 4));
                    using (var shadowBrush = new PathGradientBrush(shadow))
                    {
                        shadowBrush.CenterColor = Color.FromArgb(50, 0, 0, 0);
                        shadowBrush.SurroundColors = new[] { Color.Transparent };
                        e.Graphics.FillRectangle(shadowBrush, 5, 5, card.Width - 4, card.Height - 4);
                    }
                }
            };

            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei", 8F, FontStyle.Bold),
                ForeColor = Color.Gray,
                Height = 25
            };

            var valueLabel = new Label
            {
                Text = value,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei", 14F, FontStyle.Bold),
                ForeColor = accentColor
            };
            valueLabel.Tag = "ValueLabel";

            card.Controls.Add(valueLabel);
            card.Controls.Add(titleLabel);
            card.Controls.Add(accentBar);

            return card;
        }

        private void CreateHeatMapTab()
        {
            var tabPage = new TabPage("投篮热力图");

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            // 控制面板
            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(245, 245, 245)
            };

            var sessionLabel = new Label
            {
                Text = "选择训练场次：",
                Location = new Point(10, 15),
                AutoSize = true
            };

            var sessionComboBox = new ComboBox
            {
                Location = new Point(120, 12),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            sessionComboBox.Tag = "SessionComboBox";

            var generateButton = new Button
            {
                Text = "生成热力图",
                Location = new Point(340, 10),
                Size = new Size(100, 30),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat
            };
            generateButton.Click += GenerateHeatMapButton_Click;

            controlPanel.Controls.AddRange(new Control[] { sessionLabel, sessionComboBox, generateButton });

            // 热力图显示区域
            var heatMapPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            heatMapPanel.Tag = "HeatMapPanel";
            heatMapPanel.Paint += HeatMapPanel_Paint;
            heatMapPanel.MouseMove += HeatMapPanel_MouseMove;
            heatMapPanel.MouseClick += HeatMapPanel_MouseClick;

            panel.Controls.Add(heatMapPanel);
            panel.Controls.Add(controlPanel);
            tabPage.Controls.Add(panel);
            mainTabControl.TabPages.Add(tabPage);
        }

        private void CreatePostureAnalysisTab()
        {
            var tabPage = new TabPage("姿势分析");

            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 300
            };

            // 上半部分：最优姿势分析
            var analysisGroup = new GroupBox
            {
                Text = "最优姿势分析",
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };

            var analysisListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            analysisListView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "姿势要素", Width = 120 },
                new ColumnHeader { Text = "最优值", Width = 100 },
                new ColumnHeader { Text = "当前平均值", Width = 100 },
                new ColumnHeader { Text = "改进潜力", Width = 100 },
                new ColumnHeader { Text = "建议", Width = 300 }
            });
            analysisListView.Tag = "PostureAnalysisList";

            analysisGroup.Controls.Add(analysisListView);

            // 下半部分：2D姿势展示
            var postureGroup = new GroupBox
            {
                Text = "最优姿势展示",
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };

            var posturePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            posturePanel.Tag = "PosturePanel";
            posturePanel.Paint += PosturePanel_Paint;

            postureGroup.Controls.Add(posturePanel);

            splitContainer.Panel1.Controls.Add(analysisGroup);
            splitContainer.Panel2.Controls.Add(postureGroup);
            tabPage.Controls.Add(splitContainer);
            mainTabControl.TabPages.Add(tabPage);
        }

        private void CreateFatigueAnalysisTab()
        {
            var tabPage = new TabPage("疲劳度分析");

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            // 创建图表面板
            var sessionLabel = new Label
            {
                Text = "选择训练场次：",
                Location = new Point(10, 10),
                AutoSize = true
            };
            fatigueSessionComboBox = new ComboBox
            {
                Location = new Point(120, 7),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            fatigueSessionComboBox.SelectedIndexChanged += FatigueSessionComboBox_SelectedIndexChanged;

            panel.Controls.Add(sessionLabel);
            panel.Controls.Add(fatigueSessionComboBox);

            var plotView = new PlotView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Top = 40
            };
            plotView.Tag = "FatiguePlotView";

            panel.Controls.Add(plotView);
            tabPage.Controls.Add(panel);
            mainTabControl.TabPages.Add(tabPage);
        }

        private void CreateRecommendationsTab()
        {
            var tabPage = new TabPage("训练建议");

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            // 建议列表
            var recommendationsGroup = new GroupBox
            {
                Text = "个性化训练建议",
                Dock = DockStyle.Top,
                Height = 300,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };

            var recommendationsListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            recommendationsListView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "类型", Width = 100 },
                new ColumnHeader { Text = "优先级", Width = 80 },
                new ColumnHeader { Text = "建议内容", Width = 700 },
                new ColumnHeader { Text = "状态", Width = 80 },
                new ColumnHeader { Text = "创建时间", Width = 120 }
            });
            recommendationsListView.Tag = "RecommendationsList";

            recommendationsGroup.Controls.Add(recommendationsListView);

            generateAIButton = new Button
            {
                Text = "生成AI智能建议",
                Size = new Size(180, 40),
                Location = new Point(10, 320),
                BackColor = Color.Orange,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };
            generateAIButton.Click += GenerateAIButton_Click;
            // 导出报告按钮
            exportButton = new Button
            {
                Text = "导出PDF报告",
                Size = new Size(150, 40),
                Location = new Point(210, 320),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold),
                Enabled = false
            };
            exportButton.Click += ExportPdfButton_Click;
            panel.Controls.Add(generateAIButton);
            panel.Controls.Add(exportButton);
            panel.Controls.Add(recommendationsGroup);
            tabPage.Controls.Add(panel);
            mainTabControl.TabPages.Add(tabPage);
        }

        private void CreateSummaryTab()
        {
            var tabPage = new TabPage("上传总评");

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            // 上传按钮
            var uploadButton = new Button
            {
                Text = "上传本次总评",
                Size = new Size(180, 40),
                Location = new Point(10, 10),
                BackColor = Color.Orange,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold)
            };
            uploadButton.Click += UploadSummaryButton_Click;

            var summaryListView = new ListView
            {
                Location = new Point(10, 60),
                Size = new Size(panel.Width - 20, panel.Height - 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };

            summaryListView.Columns.AddRange(new[]
            {
                new ColumnHeader { Text = "日期", Width = 120 },
                new ColumnHeader { Text = "总评内容", Width = 1000 }
            });
            summaryListView.Tag = "SummaryList";

            panel.Controls.Add(uploadButton);
            panel.Controls.Add(summaryListView);
            tabPage.Controls.Add(panel);
            mainTabControl.TabPages.Add(tabPage);
            
        }

        private void InitializeServices()
        {
            try
            {
                // 数据库连接字符串 - SQL Server Express
                string connectionString = "Server=.\\SQLEXPRESS;Database=BasketballAnalyzer;Integrated Security=true;TrustServerCertificate=true;";
                databaseService = new DatabaseService(connectionString);
                analysisService = new AnalysisService(databaseService);

                statusLabel.Text = "数据库连接成功";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据库连接失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "数据库连接失败";
            }
        }

        private void LoadPlayers()
        {
            try
            {
                var players = databaseService.GetAllPlayers();
                playerComboBox.DataSource = players;
                playerComboBox.DisplayMember = "PlayerName";
                playerComboBox.ValueMember = "PlayerID";

                if (players.Any())
                {
                    currentPlayer = players.First();
                    LoadPlayerData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载球员数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPlayerData()
        {
            if (currentPlayer == null) return;

            try
            {
                statusLabel.Text = "正在加载数据...";

                // 加载统计数据
                LoadStatistics();

                // 加载训练记录
                LoadTrainingSessions();

                // 加载姿势分析
                LoadPostureAnalysis();

                // 加载疲劳度分析
                LoadFatigueAnalysis();

                // 加载训练建议
                LoadRecommendations();
                LoadSummaries();

                statusLabel.Text = $"已加载 {currentPlayer.PlayerName} 的数据";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载数据失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "数据加载失败";
            }
        }

        private void LoadStatistics()
        {
            if (currentPlayer == null) return;

            var stats = analysisService.GetPlayerStatistics(currentPlayer.PlayerID);

            // 更新统计卡片
            foreach (Control control in mainTabControl.TabPages[0].Controls[0].Controls)
            {
                if (control is TableLayoutPanel statsPanel)
                {
                    foreach (Control card in statsPanel.Controls)
                    {
                        if (card is Panel cardPanel && card.Tag is string title)
                        {
                            var valueLabel = cardPanel.Controls.OfType<Label>().FirstOrDefault(l => l.Tag?.ToString() == "ValueLabel");
                            if (valueLabel != null && stats.ContainsKey(title))
                            {
                                var value = stats[title];
                                if (value is double doubleValue)
                                {
                                    if (title.Contains("命中率"))
                                        valueLabel.Text = $"{doubleValue:F1}%";
                                    else if (title.Contains("时长"))
                                        valueLabel.Text = $"{doubleValue:F0}分钟";
                                    else
                                        valueLabel.Text = doubleValue.ToString("F1");
                                }
                                else
                                {
                                    valueLabel.Text = value.ToString();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void LoadTrainingSessions()
        {
            if (currentPlayer == null) return;

            var sessions = databaseService.GetTrainingSessions(currentPlayer.PlayerID);

            // 更新训练记录列表
            var trainingListView = FindControlByTag("TrainingList") as ListView;
            if (trainingListView != null)
            {
                trainingListView.Items.Clear();

                foreach (var session in sessions.Take(10)) // 只显示最近10次
                {
                    var item = new ListViewItem(session.SessionDate.ToString("yyyy-MM-dd"));
                    item.SubItems.Add($"{session.Duration}分钟");
                    item.SubItems.Add(session.TotalShots.ToString());
                    item.SubItems.Add(session.MadeShots.ToString());
                    item.SubItems.Add($"{session.ShootingPercentage:F1}%");
                    item.SubItems.Add(session.FatigueLevel?.ToString() ?? "N/A");
                    item.SubItems.Add(session.Notes ?? "");

                    trainingListView.Items.Add(item);
                }
            }

            // 更新热力图Tab中的场次选择
            var sessionComboBox = FindControlByTag("SessionComboBox") as ComboBox;
            if (sessionComboBox != null)
            {
                sessionComboBox.DataSource = sessions;
                sessionComboBox.DisplayMember = "SessionDate";
                sessionComboBox.ValueMember = "SessionID";
            }
        }

        private void LoadPostureAnalysis()
        {
            if (currentPlayer == null) return;

            var optimizations = analysisService.AnalyzeOptimalPosture(currentPlayer.PlayerID);

            var analysisListView = FindControlByTag("PostureAnalysisList") as ListView;
            if (analysisListView != null)
            {
                analysisListView.Items.Clear();

                foreach (var opt in optimizations)
                {
                    var item = new ListViewItem(opt.PostureAspect);
                    item.SubItems.Add(opt.OptimalValue.ToString("F1"));
                    item.SubItems.Add(opt.CurrentAverage.ToString("F1"));
                    item.SubItems.Add($"{opt.ExpectedImprovement:F1}%");
                    item.SubItems.Add(opt.Recommendation);

                    analysisListView.Items.Add(item);
                }
            }
        }

        private void LoadFatigueAnalysis()
        {
            if (currentPlayer == null) return;

            var sessions = databaseService.GetTrainingSessions(currentPlayer.PlayerID);
            fatigueSessionComboBox.DataSource = sessions;
            fatigueSessionComboBox.DisplayMember = "SessionDate";
            fatigueSessionComboBox.ValueMember = "SessionID";

            if (sessions.Any())
                LoadFatigueAnalysis(sessions.First().SessionID);
        }

        private void FatigueSessionComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (fatigueSessionComboBox.SelectedItem is TrainingSession session)
            {
                LoadFatigueAnalysis(session.SessionID);
            }
        }

        private void LoadFatigueAnalysis(int? sessionId)
        {
            if (currentPlayer == null) return;

            var timeSeriesData = analysisService.GetFatigueAnalysis(currentPlayer.PlayerID, sessionId);

            var plotView = FindControlByTag("FatiguePlotView") as PlotView;
            if (plotView != null)
            {
                var plotModel = new PlotModel { Title = "疲劳度对投篮命中率的影响" };

                plotModel.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "时间",
                    StringFormat = "MM-dd HH:mm"
                });
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "投篮命中率 (%)",
                    Minimum = 0,
                    Maximum = 100
                });
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Right,
                    Title = "疲劳程度",
                    Minimum = 0,
                    Maximum = 10,
                    Key = "FatigueAxis"
                });

                var shootingPercentageSeries = new LineSeries
                {
                    Title = "投篮命中率",
                    Color = OxyColors.Blue,
                    StrokeThickness = 2
                };

                var fatigueSeries = new LineSeries
                {
                    Title = "疲劳程度",
                    Color = OxyColors.Red,
                    StrokeThickness = 2,
                    YAxisKey = "FatigueAxis"
                };

                foreach (var point in timeSeriesData)
                {
                    var timeValue = DateTimeAxis.ToDouble(point.Time);
                    shootingPercentageSeries.Points.Add(new DataPoint(timeValue, point.ShootingPercentage));
                    fatigueSeries.Points.Add(new DataPoint(timeValue, point.FatigueLevel));
                }

                plotModel.Series.Add(shootingPercentageSeries);
                plotModel.Series.Add(fatigueSeries);

                plotView.Model = plotModel;
            }
        }

        private async void LoadRecommendations()
        {
            if (currentPlayer == null) return;

            try
            {
                statusLabel.Text = "正在生成智能训练建议...";

                // 使用新的智能建议生成方法
                var recommendations = await analysisService.GenerateIntelligentRecommendations(currentPlayer.PlayerID);

                var recommendationsListView = FindControlByTag("RecommendationsList") as ListView;
                if (recommendationsListView != null)
                {
                    recommendationsListView.Items.Clear();
                    foreach (var rec in recommendations)
                    {
                        var item = new ListViewItem(rec.RecommendationType);
                        item.SubItems.Add(rec.Priority.ToString());
                        item.SubItems.Add(rec.Description);
                        item.SubItems.Add(rec.Status);
                        item.SubItems.Add(rec.CreatedDate.ToString("yyyy-MM-dd"));
                        recommendationsListView.Items.Add(item);
                    }
                }

                statusLabel.Text = "智能建议生成完成";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取训练建议失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "建议生成失败";
            }
        }

        private void LoadSummaries()
        {
            if (currentPlayer == null) return;
            var summaries = databaseService.GetSummaries(currentPlayer.PlayerID);
            var summaryListView = FindControlByTag("SummaryList") as ListView;
            if (summaryListView != null)
            {
                summaryListView.Items.Clear();
                foreach (var s in summaries)
                {
                    var item = new ListViewItem(s.Date.ToString("yyyy-MM-dd HH:mm"));
                    item.SubItems.Add(s.Content);
                    summaryListView.Items.Add(item);
                }
            }
        }

        private void UploadSummaryButton_Click(object? sender, EventArgs e)
        {
            if (currentPlayer == null)
            {
                MessageBox.Show("请先选择球员", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string summary = analysisService.GenerateSummaryForCurrentSession(currentPlayer.PlayerID);
            string style = analysisService.AnalyzeShootingStyle(currentPlayer.PlayerID);

            string fullSummary = summary + "\n" + style;

            databaseService.AddSummary(currentPlayer.PlayerID, DateTime.Now, fullSummary);

            LoadSummaries();
            MessageBox.Show("总评上传成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        } 
        private Control? FindControlByTag(string tag)
        {
            return FindControlByTag(this, tag);
        }

        private Control? FindControlByTag(Control parent, string tag)
        {
            foreach (Control control in parent.Controls)
            {
                if (control.Tag?.ToString() == tag)
                    return control;

                var found = FindControlByTag(control, tag);
                if (found != null)
                    return found;
            }
            return null;
        }

        // 事件处理方法
        private void PlayerComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (playerComboBox.SelectedItem is Player player)
            {
                currentPlayer = player;
                LoadPlayerData();
            }
        }

        private void AddPlayerButton_Click(object? sender, EventArgs e)
        {
            using (var addForm = new AddPlayerForm())
            {
                if (addForm.ShowDialog() == DialogResult.OK && addForm.NewPlayer != null)
                {
                    var allPlayers = databaseService.GetAllPlayers();
                    if (allPlayers.Any(p => p.PlayerName == addForm.NewPlayer.PlayerName))
                    {
                        MessageBox.Show("球员已存在，不能重复添加！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    try
                    {
                        databaseService.AddPlayer(addForm.NewPlayer);
                        LoadPlayers();
                        MessageBox.Show("添加球员成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"添加球员失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void AddDataButton_Click(object? sender, EventArgs e)
        {
            if (currentPlayer == null)
            {
                MessageBox.Show("请先选择球员", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var addSessionForm = new AddTrainingSessionForm(currentPlayer.PlayerID);
            if (addSessionForm.ShowDialog() == DialogResult.OK && addSessionForm.NewSession != null)
            {
                var session = addSessionForm.NewSession;
                session.TotalShots = addSessionForm.NewShots.Count;
                session.MadeShots = addSessionForm.NewShots.Count(s => s.ShotResult);
                int sessionId = databaseService.AddTrainingSession(session);
                foreach (var shot in addSessionForm.NewShots)
                {
                    shot.SessionID = sessionId;
                    databaseService.AddShotRecord(shot);
                }
                LoadPlayerData();
                MessageBox.Show("训练数据添加成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void RefreshButton_Click(object? sender, EventArgs e)
        {
            LoadPlayerData();
        }

        private void GenerateHeatMapButton_Click(object? sender, EventArgs e)
        {
            var heatMapPanel = FindControlByTag("HeatMapPanel");
            heatMapPanel?.Invalidate(); // 重绘热力图
        }

        private void HeatMapPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (currentPlayer == null) return;
            var panel = sender as Panel;
            if (panel == null) return;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            // 专业篮球场
            DrawProfessionalBasketballCourt(g, panel.ClientRectangle);
            // 专业热力图
            DrawProfessionalHeatMap(g, panel.ClientRectangle);
            // 图例
            DrawHeatMapLegend(g, panel.ClientRectangle);
        }

        private void DrawProfessionalBasketballCourt(Graphics g, Rectangle bounds)
        {
            // 设置篮球场尺寸
            int courtWidth = Math.Min(bounds.Width, bounds.Height * 2); // 保持2:1的宽高比
            int courtHeight = courtWidth / 2;
            int courtX = bounds.X + (bounds.Width - courtWidth) / 2;
            int courtY = bounds.Y + (bounds.Height - courtHeight) / 2;

            // 绘制篮球场背景
            using (var courtBrush = new SolidBrush(Color.FromArgb(240, 248, 255))) // 浅蓝色背景
                g.FillRectangle(courtBrush, courtX, courtY, courtWidth, courtHeight);

            using (var borderPen = new Pen(Color.Black, 3))
                g.DrawRectangle(borderPen, courtX, courtY, courtWidth, courtHeight);

            // 计算关键点
            float centerX = courtX + courtWidth / 2f;
            float baseY = courtY + courtHeight; // 底线位置

            // 尺寸比例（基于球场宽度）
            float threePointRadius = courtWidth * 0.3f;  // 三分线半径
            float keyWidth = courtWidth * 0.25f;         // 禁区宽度
            float keyHeight = courtHeight * 0.5f;        // 禁区高度
            float freeThrowCircleRadius = courtWidth * 0.06f; // 罚球圈半径
            float restrictedAreaRadius = courtWidth * 0.05f;  // 合理冲撞区半径
            float cornerOffset = courtWidth * 0.1f;       // 底角偏移量

            using (var linePen = new Pen(Color.Black, 2))
            {
                // 绘制三分线
                // 弧顶部分（180度半圆）
                g.DrawArc(linePen,
                    courtX + cornerOffset + 50,
                    baseY - threePointRadius * 2 + 200,
                    courtWidth - cornerOffset * 2 - 100,
                    threePointRadius * 2 - 100,
                    0,
                    -180);

                // 两侧直线部分
                float threePointY = baseY - threePointRadius;
                g.DrawLine(linePen,
                    courtX + cornerOffset + 50,
                    baseY,
                    courtX + cornerOffset + 50,
                    threePointY + 150);

                g.DrawLine(linePen,
                    courtX + courtWidth - cornerOffset - 50,
                    baseY,
                    courtX + courtWidth - cornerOffset - 50,
                    threePointY + 150);

                // 绘制禁区
                float keyTop = baseY - keyHeight;
                float keyLeft = centerX - keyWidth / 2;
                float keyRight = centerX + keyWidth / 2;

                // 两侧边线
                g.DrawLine(linePen, keyLeft, keyTop, keyLeft, baseY);
                g.DrawLine(linePen, keyRight, keyTop, keyRight, baseY);

                // 罚球线
                g.DrawLine(linePen, keyLeft, keyTop, keyRight, keyTop);

                // 绘制罚球圈
                float circleY = keyTop - freeThrowCircleRadius;
                g.DrawArc(linePen,
                    centerX - freeThrowCircleRadius,
                    circleY,
                    freeThrowCircleRadius * 2,
                    freeThrowCircleRadius * 2,
                    180,
                    180);
                using (var dashedPen = new Pen(Color.Black, 2))
                {
                    // 设置虚线样式
                    dashedPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

                    // 绘制下半弧
                    g.DrawArc(dashedPen,
                              centerX - freeThrowCircleRadius,
                              circleY,
                              freeThrowCircleRadius * 2,
                              freeThrowCircleRadius * 2,
                              0,    // 起始角度：0°（右侧）
                              180); // 扫掠角度：180°（顺时针到左侧）
                }
                // 绘制合理冲撞区（小弧线）
                g.DrawArc(linePen,
                    centerX - restrictedAreaRadius,
                    baseY - restrictedAreaRadius * 2 + 20,
                    restrictedAreaRadius * 2,
                    restrictedAreaRadius * 2,
                    0,
                    -180);
            }

            // 绘制篮筐
            float basketOffsetY = courtHeight * 0.07f;
            int basketRadius = (int)(courtWidth * 0.015f);
            int basketX = (int)(centerX - basketRadius);
            int basketY = (int)(baseY - basketRadius - basketOffsetY);

            using (var brush = new SolidBrush(Color.Orange))
                g.FillEllipse(brush, basketX, basketY, basketRadius * 2, basketRadius * 2);

            using (var pen = new Pen(Color.Black, 2))
                g.DrawEllipse(pen, basketX, basketY, basketRadius * 2, basketRadius * 2);

            //绘制篮筐支架
            using (var backboardPen = new Pen(Color.Black, 3))
            {
                // 篮板（在篮筐后方）
                float backboardWidth = courtWidth * 0.08f;
                float backboardHeight = courtHeight * 0.05f;
                float backboardY = baseY - restrictedAreaRadius * 2 - backboardHeight;

                //g.DrawRectangle(backboardPen,
                //    centerX - backboardWidth / 2,
                //    backboardY,
                //    backboardWidth,
                //    backboardHeight);

                // 支架连接线
                g.DrawLine(backboardPen,
                    centerX, baseY,
                    centerX, basketY + basketRadius * 2);
            }
        }

        // ... existing code ...
        // 专业热力图绘制
        //private void DrawProfessionalHeatMap(Graphics g, Rectangle bounds)
        //{
        //    if (currentPlayer == null) return;
        //    try
        //    {
        //        var heatMapData = analysisService.GetHeatMapData(currentPlayer.PlayerID);
        //        int courtWidth = bounds.Width - 100, courtHeight = bounds.Height - 100;
        //        int courtX = bounds.X + 50, courtY = bounds.Y + 50;
        //        var zones = GetProfessionalHeatZones(courtX, courtY, courtWidth, courtHeight);

        //        // 先绘制篮球场
        //        DrawProfessionalBasketballCourt(g, bounds);

        //        foreach (var zone in zones)
        //        {
        //            if (zone.Points.Length < 3) continue;

        //            var zoneData = heatMapData.FirstOrDefault(z => z.Zone.ZoneName == zone.Name);
        //            if (zoneData == null || zoneData.TotalShots == 0) continue;

        //            var heatValue = Math.Min(zoneData.ShootingPercentage + (zoneData.TotalShots * 0.5), 100);
        //            var alpha = (int)(heatValue * 2.55);
        //            var heatColor = Color.FromArgb(Math.Min(alpha, 255), Color.Red);

        //            using (var brush = new SolidBrush(heatColor))
        //            {
        //                g.FillPolygon(brush, zone.Points);
        //            }

        //            using (var pen = new Pen(Color.White, 1))
        //            {
        //                g.DrawPolygon(pen, zone.Points);
        //            }

        //            // 计算区域中心点
        //            float centerX = zone.Points.Average(p => p.X);
        //            float centerY = zone.Points.Average(p => p.Y);

        //            var text = $"{zone.Name}\n{zoneData.ShootingPercentage:F1}%";
        //            var textBrush = heatValue > 50 ? Brushes.White : Brushes.Black;
        //            var textFont = new Font("Microsoft YaHei", 8, FontStyle.Bold);

        //            g.DrawString(text, textFont, textBrush, centerX, centerY,
        //                         new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        g.DrawString($"热力图加载失败：{ex.Message}", new Font("Microsoft YaHei", 10), Brushes.Red, bounds.Location);
        //    }
        //}
        private void DrawProfessionalHeatMap(Graphics g, Rectangle bounds)
        {
            if (currentPlayer == null) return;
            try
            {
                var heatMapData = analysisService.GetHeatMapData(currentPlayer.PlayerID);
                int courtWidth = bounds.Width - 100, courtHeight = bounds.Height - 100;
                int courtX = bounds.X + 50, courtY = bounds.Y + 50;
                var zones = GetProfessionalHeatZones(courtX, courtY, courtWidth, courtHeight);

                // 先绘制篮球场
                DrawProfessionalBasketballCourt(g, bounds);

                foreach (var zone in zones)
                {
                    var zoneData = heatMapData.FirstOrDefault(z => z.Zone.ZoneName == zone.Name);
                    if (zoneData == null || zoneData.TotalShots == 0) continue;

                    var heatValue = Math.Min(zoneData.ShootingPercentage + (zoneData.TotalShots * 0.5), 100);
                    var alpha = (int)(heatValue * 2.55);
                    var heatColor = Color.FromArgb(Math.Min(alpha, 255), Color.Red);

                    using (var brush = new SolidBrush(heatColor))
                    {
                        g.FillPath(brush, zone.Path);
                    }

                    using (var pen = new Pen(Color.White, 1))
                    {
                        g.DrawPath(pen, zone.Path);
                    }

                    // 计算区域中心点
                    var center = GetPathCenter(zone.Path);
                    var text = $"{zone.Name}\n{zoneData.ShootingPercentage:F1}%";
                    var textBrush = heatValue > 50 ? Brushes.White : Brushes.Black;
                    var textFont = new Font("Microsoft YaHei", 8, FontStyle.Bold);

                    g.DrawString(text, textFont, textBrush, center.X, center.Y,
                                 new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
            }
            catch (Exception ex)
            {
                g.DrawString($"热力图加载失败：{ex.Message}", new Font("Microsoft YaHei", 10), Brushes.Red, bounds.Location);
            }
        }

        private PointF GetPathCenter(GraphicsPath path)
        {
            var bounds = path.GetBounds();
            return new PointF(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        }
        // 区域定义
        //private List<HeatZoneInfo> GetProfessionalHeatZones(int courtX, int courtY, int courtWidth, int courtHeight)
        //{
        //    var zones = new List<HeatZoneInfo>();
        //    float centerX = courtX + courtWidth / 2f;
        //    float baseY = courtY + courtHeight; // 底线位置
        //    float basketY = baseY - courtHeight * 0.07f; // 篮筐位置

        //    // 关键尺寸比例
        //    float threePointRadius = courtWidth * 0.3f;  // 三分线半径
        //    float keyWidth = courtWidth * 0.25f;         // 禁区宽度
        //    float keyHeight = courtHeight * 0.5f;        // 禁区高度
        //    float restrictedAreaRadius = courtWidth * 0.05f;  // 合理冲撞区半径
        //    float cornerOffset = courtWidth * 0.1f;       // 底角偏移量

        //    // 三分线外区域（5个区域）
        //    // 左侧底角三分
        //    zones.Add(new HeatZoneInfo
        //    {
        //        Name = "左侧底角三分",
        //        Points = new PointF[]
        //        {
        //    new PointF(courtX + cornerOffset + 9, baseY+32),
        //    new PointF(courtX + cornerOffset + 9, basketY - threePointRadius/2),
        //    new PointF(centerX - courtHeight-70 , basketY - threePointRadius/2),
        //    new PointF(centerX - courtHeight-70 , baseY+32)
        //        }
        //    });

        //    // 右侧底角三分
        //    zones.Add(new HeatZoneInfo
        //    {
        //        Name = "右侧底角三分",
        //        Points = new PointF[]
        //        {
        //    new PointF(courtX + courtWidth - cornerOffset - 10, baseY+32),
        //    new PointF(courtX + courtWidth - cornerOffset - 10, basketY - threePointRadius/2),
        //    new PointF(courtWidth+99, basketY - threePointRadius/2),
        //    new PointF(courtWidth+99, baseY+32)
        //        }
        //    });

        //    // 左侧45度三分
        //    zones.Add(new HeatZoneInfo
        //    {
        //        Name = "左侧45度三分",
        //        Points = new PointF[]
        //        {
        //    new PointF(courtX + cornerOffset, basketY - threePointRadius),
        //    new PointF(centerX - keyWidth/2, basketY - threePointRadius),
        //    new PointF(centerX - threePointRadius/2, basketY - threePointRadius * 1.2f),
        //    new PointF(courtX + cornerOffset, basketY - threePointRadius * 1.2f)
        //        }
        //    });

        //    // 右侧45度三分
        //    zones.Add(new HeatZoneInfo
        //    {
        //        Name = "右侧45度三分",
        //        Points = new PointF[]
        //        {
        //    new PointF(courtX + courtWidth - cornerOffset, basketY - threePointRadius),
        //    new PointF(centerX + keyWidth/2, basketY - threePointRadius),
        //    new PointF(centerX + threePointRadius/2, basketY - threePointRadius * 1.2f),
        //    new PointF(courtX + courtWidth - cornerOffset, basketY - threePointRadius * 1.2f)
        //        }
        //    });

        //    // 弧顶三分
        //    zones.Add(new HeatZoneInfo
        //    {
        //        Name = "弧顶三分",
        //        Points = new PointF[]
        //        {
        //    new PointF(centerX - keyWidth/2, basketY - threePointRadius),
        //    new PointF(centerX + keyWidth/2, basketY - threePointRadius),
        //    new PointF(centerX + threePointRadius/2, basketY - threePointRadius * 1.2f),
        //    new PointF(centerX - threePointRadius/2, basketY - threePointRadius * 1.2f)
        //        }
        //    });

        //    // 三分线内区域（6个区域）
        //    // 禁区（油漆区）
        //    float paintTop = basketY - keyHeight;
        //    zones.Add(new HeatZoneInfo
        //    {
        //        Name = "禁区",
        //        Points = new PointF[]
        //        {
        //    new PointF(centerX - keyWidth/2, baseY),
        //    new PointF(centerX + keyWidth/2, baseY),
        //    new PointF(centerX + keyWidth/2, paintTop),
        //    new PointF(centerX - keyWidth/2, paintTop)
        //        }
        //    });

        //    // 罚球线附近
        //    float ftTop = paintTop - courtHeight * 0.1f;
        //    zones.Add(new HeatZoneInfo
        //    {
        //        Name = "罚球线附近",
        //        Points = new PointF[]
        //        {
        //    new PointF(centerX - keyWidth/2, paintTop),
        //    new PointF(centerX + keyWidth/2, paintTop),
        //    new PointF(centerX + keyWidth/2, ftTop),
        //    new PointF(centerX - keyWidth/2, ftTop)
        //        }
        //    });

        //    // 左侧45度中距离
        //    zones.Add(new HeatZoneInfo
        //    {
        //        Name = "左侧45度中距离",
        //        Points = new PointF[]
        //        {
        //    new PointF(courtX + cornerOffset, basketY - threePointRadius),
        //    new PointF(centerX - keyWidth/2, basketY - threePointRadius),
        //    new PointF(centerX - keyWidth/2, paintTop),
        //    new PointF(courtX + cornerOffset, paintTop)
        //        }
        //    });

        //    // 右侧45度中距离
        //    zones.Add(new HeatZoneInfo
        //    {
        //        Name = "右侧45度中距离",
        //        Points = new PointF[]
        //        {
        //    new PointF(courtX + courtWidth - cornerOffset, basketY - threePointRadius),
        //    new PointF(centerX + keyWidth/2, basketY - threePointRadius),
        //    new PointF(centerX + keyWidth/2, paintTop),
        //    new PointF(courtX + courtWidth - cornerOffset, paintTop)
        //        }
        //    });

        //    // 禁区左侧
        //    float restrictedLeft = centerX - restrictedAreaRadius;
        //    float restrictedRight = centerX + restrictedAreaRadius;
        //    zones.Add(new HeatZoneInfo
        //    {
        //        Name = "禁区左侧",
        //        Points = new PointF[]
        //        {
        //    new PointF(centerX - keyWidth/2, baseY),
        //    new PointF(restrictedLeft, baseY),
        //    new PointF(restrictedLeft, basketY),
        //    new PointF(centerX - keyWidth/2, basketY)
        //        }
        //    });

        //    // 禁区右侧
        //    zones.Add(new HeatZoneInfo
        //    {
        //        Name = "禁区右侧",
        //        Points = new PointF[]
        //        {
        //    new PointF(restrictedRight, baseY),
        //    new PointF(centerX + keyWidth/2, baseY),
        //    new PointF(centerX + keyWidth/2, basketY),
        //    new PointF(restrictedRight, basketY)
        //        }
        //    });

        //    return zones;
        //}
        private List<HeatZoneInfo> GetProfessionalHeatZones(int courtX, int courtY, int courtWidth, int courtHeight)
        {
            var zones = new List<HeatZoneInfo>();
            float centerX = courtX + courtWidth / 2f;
            float baseY = courtY + courtHeight; // 底线位置
            float basketY = baseY - courtHeight * 0.07f; // 篮筐位置

            // 关键尺寸比例
            float threePointRadius = courtWidth * 0.3f;
            float keyWidth = courtWidth * 0.25f;
            float keyHeight = courtHeight * 0.5f;
            float restrictedAreaRadius = courtWidth * 0.05f;
            float cornerOffset = courtWidth * 0.1f;
            float courtTop = courtY - 35; // 球场顶部边界

            // 1. 弧顶三分 (曲边梯形)
            var arcTopPath = new GraphicsPath();
            float arcTopY = basketY - threePointRadius - 40; // 三分线顶部Y坐标
            float arcTopLeftX = centerX - threePointRadius + 50; // 三分线左侧X坐标
            float arcTopRightX = centerX + threePointRadius - 50; // 三分线右侧X坐标

            // 添加三分线弧（上半圆）
            arcTopPath.AddArc(
                centerX - threePointRadius - 110,
                basketY - threePointRadius * 3 / 2 + 53,
                threePointRadius * 2 + 220,
                threePointRadius * 2,
                -43,
                -94
            );

            // 连接到顶部边界形成封闭区域
            arcTopPath.AddLine(arcTopLeftX, arcTopY, arcTopLeftX, courtTop);
            arcTopPath.AddLine(arcTopLeftX, courtTop, arcTopRightX, courtTop);
            arcTopPath.AddLine(arcTopRightX, courtTop, arcTopRightX, arcTopY);
            arcTopPath.CloseFigure();

            zones.Add(new HeatZoneInfo
            {
                Name = "弧顶三分",
                Path = arcTopPath
            });

            // 2. 左侧45度三分 (不规则多边形)
            var leftWingPath = new GraphicsPath();
            float leftWingX = centerX - courtHeight - 70; // 左侧边线X坐标
            float wingHeight = basketY - threePointRadius * 0.8f - 100; // 45度三分高度

            leftWingPath.StartFigure();
            leftWingPath.AddLine(leftWingX, basketY - threePointRadius / 2, leftWingX, courtTop);//左边线
            leftWingPath.AddLine(leftWingX, courtTop, arcTopLeftX, courtTop);//顶线
            leftWingPath.AddLine(arcTopLeftX, courtTop, arcTopLeftX, arcTopY + 5);//右边边线
            leftWingPath.AddLine(courtX + cornerOffset + 9, basketY - threePointRadius / 2, leftWingX + 5, basketY - threePointRadius / 2);//底边边线
            // 沿三分线弧绘制45度角
            //leftWingPath.AddArc(
            //    centerX - threePointRadius,
            //    basketY - threePointRadius * 2,
            //    threePointRadius * 2,
            //    threePointRadius * 2,
            //    180,
            //    -45
            //);
            leftWingPath.CloseFigure();

            zones.Add(new HeatZoneInfo
            {
                Name = "左侧45度三分",
                Path = leftWingPath
            });

            // 3. 右侧45度三分 (镜像左侧)
            var rightWingPath = new GraphicsPath();
            float rightWingX = courtWidth + 99;

            rightWingPath.StartFigure();
            rightWingPath.AddLine(rightWingX, basketY - threePointRadius / 2, rightWingX, courtTop);//右边边线
            rightWingPath.AddLine(rightWingX, courtTop, arcTopRightX, courtTop);//顶线
            rightWingPath.AddLine(arcTopRightX, courtTop, arcTopRightX, arcTopY + 5);//左边边线
            rightWingPath.AddLine(courtX + courtWidth - cornerOffset - 10, basketY - threePointRadius / 2, rightWingX - 5, basketY - threePointRadius / 2);//底边边线
            rightWingPath.CloseFigure();

            zones.Add(new HeatZoneInfo
            {
                Name = "右侧45度三分",
                Path = rightWingPath
            });

            // 4. 正面中距离 (曲边梯形)
            var midRangePath = new GraphicsPath();
            midRangePath.StartFigure();
            midRangePath.AddLine(centerX + keyWidth / 2, baseY - keyHeight, arcTopRightX, arcTopY);//右
            midRangePath.AddArc(
                centerX - threePointRadius - 110,
                basketY - threePointRadius * 3 / 2 + 53,
                threePointRadius * 2 + 220,
                threePointRadius * 2,
                -43,
                -94
            );
            midRangePath.AddLine(arcTopLeftX, arcTopY, centerX - keyWidth / 2, baseY - keyHeight);//左
            midRangePath.AddLine(centerX - keyWidth / 2, baseY - keyHeight, centerX + keyWidth / 2, baseY - keyHeight);//左
            midRangePath.CloseFigure();
            midRangePath.StartFigure();
            //midRangePath.AddLine(centerX - keyWidth / 2, basketY - threePointRadius / 2,
            //                     centerX - keyWidth / 2, baseY - keyHeight);
            //midRangePath.AddLine(centerX - keyWidth / 2, baseY - keyHeight,
            //                     centerX + keyWidth / 2, baseY - keyHeight);
            //midRangePath.AddLine(centerX + keyWidth / 2, baseY - keyHeight,
            //                     centerX + keyWidth / 2, basketY - threePointRadius / 2);
            //midRangePath.CloseFigure();
            zones.Add(new HeatZoneInfo
            {
                Name = "正面中距离",
                Path = midRangePath
            });

            // 5. 左侧45度中距离 (扇形)
            var leftMidPath = new GraphicsPath();
            //float leftMidAngle = 45f; // 45度扇形
            //leftMidPath.AddPie(
            //    centerX - threePointRadius / 2,
            //    basketY - threePointRadius / 2,
            //    threePointRadius,
            //    threePointRadius,
            //    135,
            //    leftMidAngle
            //);
            leftMidPath.StartFigure();
            leftMidPath.AddLine(centerX - keyWidth / 2 - 10, baseY - keyHeight, arcTopLeftX, arcTopY + 5);//顶线
            leftMidPath.AddLine(arcTopLeftX, arcTopY + 5, courtX + cornerOffset + 9, basketY - threePointRadius / 2);//左边边线
            //rightMidPath.AddLine(centerX + keyWidth / 2 + 10, basketY - threePointRadius / 2, centerX + keyWidth / 2 + 10, baseY - keyHeight);//左边边线
            leftMidPath.AddLine(courtX + cornerOffset + 9, basketY - threePointRadius / 2, centerX - keyWidth / 2 - 10, basketY - threePointRadius / 2);//底边边线
            leftMidPath.CloseFigure();
            zones.Add(new HeatZoneInfo
            {
                Name = "左侧45度中距离",
                Path = leftMidPath
            });

            // 6. 右侧45度中距离 (镜像左侧)
            var rightMidPath = new GraphicsPath();
            //rightMidPath.AddPie(
            //    centerX - threePointRadius / 2,
            //    basketY - threePointRadius / 2,
            //    threePointRadius,
            //    threePointRadius,
            //    0,
            //    -leftMidAngle
            //);
            rightMidPath.StartFigure();
            rightMidPath.AddLine(centerX + keyWidth / 2 + 10, baseY - keyHeight, arcTopRightX, arcTopY + 5);//顶线
            rightMidPath.AddLine(arcTopRightX, arcTopY + 5, courtX + courtWidth - cornerOffset - 10, basketY - threePointRadius / 2);//右边边线
            //rightMidPath.AddLine(centerX + keyWidth / 2 + 10, basketY - threePointRadius / 2, centerX + keyWidth / 2 + 10, baseY - keyHeight);//左边边线
            rightMidPath.AddLine(courtX + courtWidth - cornerOffset - 10, basketY - threePointRadius / 2, centerX + keyWidth / 2 + 10, basketY - threePointRadius / 2);//底边边线
            rightMidPath.CloseFigure();
            zones.Add(new HeatZoneInfo
            {
                Name = "右侧45度中距离",
                Path = rightMidPath
            });

            // 7. 篮下 (矩形)
            var paintPath = new GraphicsPath();
            float paintTop = basketY - keyHeight;
            paintPath.AddRectangle(new RectangleF(
            centerX - keyWidth / 2 - 10,
            basketY - threePointRadius / 2,
            keyWidth + 20,  // 增加宽度确保可见
            keyHeight
            ));
            zones.Add(new HeatZoneInfo
            {
                Name = "篮下",
                Path = paintPath
            });

            // 8. 罚球线附近 (矩形)
            var ftPath = new GraphicsPath();
            float ftTop = paintTop - courtHeight * 0.1f;
            ftPath.AddRectangle(new RectangleF(
                centerX - keyWidth / 2,
                ftTop,
                keyWidth,
                courtHeight * 0.1f
            ));

            zones.Add(new HeatZoneInfo
            {
                Name = "罚球线附近",
                Path = ftPath
            });

            // 9. 左侧底角三分 (多边形)
            var leftCornerPath = new GraphicsPath();
            leftCornerPath.AddPolygon(new PointF[] {
            new PointF(courtX + cornerOffset + 9, baseY+32),
            new PointF(courtX + cornerOffset + 9, basketY - threePointRadius/2),
            new PointF(centerX - courtHeight-70 , basketY - threePointRadius/2),//最左
            new PointF(centerX - courtHeight-70 , baseY+32)
    });

            zones.Add(new HeatZoneInfo
            {
                Name = "左侧底角三分",
                Path = leftCornerPath
            });

            // 10. 右侧底角三分 (镜像左侧)
            var rightCornerPath = new GraphicsPath();
            rightCornerPath.AddPolygon(new PointF[] {
            new PointF(courtX + courtWidth - cornerOffset - 10, baseY+32),
            new PointF(courtX + courtWidth - cornerOffset - 10, basketY - threePointRadius/2),
            new PointF(courtWidth+99, basketY - threePointRadius/2),
            new PointF(courtWidth+99, baseY+32)//最右
    });

            zones.Add(new HeatZoneInfo
            {
                Name = "右侧底角三分",
                Path = rightCornerPath
            });
            var leftMidRangePath = new GraphicsPath();
            leftMidRangePath.AddPolygon(new PointF[] {
        new PointF(courtX + cornerOffset + 9, baseY + 32), // 左下角
        new PointF(centerX - courtHeight - 70, baseY + 32), // 右下角
        new PointF(centerX - courtHeight - 70, basketY - threePointRadius / 2 + 50), // 右上角
        new PointF(courtX + cornerOffset + 9, basketY - threePointRadius / 2 + 50) // 左上角
    });
            zones.Add(new HeatZoneInfo
            {
                Name = "左侧中距离",
                Path = leftMidRangePath
            });

            // 12. 右侧中距离 (底线附近)
            var rightMidRangePath = new GraphicsPath();
            rightMidRangePath.AddPolygon(new PointF[] {
        new PointF(courtX + courtWidth - cornerOffset - 10, baseY + 32), // 右下角
        new PointF(courtWidth + 99, baseY + 32), // 左下角
        new PointF(courtWidth + 99, basketY - threePointRadius / 2 + 50), // 左上角
        new PointF(courtX + courtWidth - cornerOffset - 10, basketY - threePointRadius / 2 + 50) // 右上角
    });
            zones.Add(new HeatZoneInfo
            {
                Name = "右侧中距离",
                Path = rightMidRangePath
            });
            return zones;
        }

        //private class HeatZoneInfo
        //{
        //    public string Name { get; set; } = string.Empty;
        //    public PointF[] Points { get; set; } = Array.Empty<PointF>();
        //}
        private class HeatZoneInfo
        {
            public string Name { get; set; } = string.Empty;
            public GraphicsPath Path { get; set; } = new GraphicsPath();
        }
        // 图例
        private void DrawHeatMapLegend(Graphics g, Rectangle bounds)
        {
            int legendX = bounds.X + 10, legendY = bounds.Y + 10, legendWidth = 150, legendHeight = 120;
            using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
                g.FillRectangle(brush, legendX, legendY, legendWidth, legendHeight);
            using (var pen = new Pen(Color.Black, 1))
                g.DrawRectangle(pen, legendX, legendY, legendWidth, legendHeight);
            g.DrawString("热力图图例", new Font("Microsoft YaHei", 10, FontStyle.Bold), Brushes.Black, legendX + 55, legendY + 5);
            int gradientY = legendY + 30;
            for (int i = 0; i < 100; i++)
            {
                int alpha = (int)(i * 2.55);
                var color = Color.FromArgb(Math.Min(alpha, 255), Color.Red);
                using (var brush = new SolidBrush(color))
                    g.FillRectangle(brush, legendX + 10 + i, gradientY, 1, 20);
            }
            g.DrawString("0%", new Font("Microsoft YaHei", 8), Brushes.Black, legendX + 5, gradientY + 25);
            g.DrawString("100%", new Font("Microsoft YaHei", 8), Brushes.Black, legendX + 85, gradientY + 25);
            g.DrawString("颜色越深表示命中率越高", new Font("Microsoft YaHei", 8), Brushes.Black, legendX + 5, gradientY + 50);
        }
        //private class HeatZoneInfo
        //{
        //    public string Name { get; set; } = string.Empty;
        //    public Rectangle Bounds { get; set; }
        //    public bool IsArc { get; set; }
        //    public float StartAngle { get; set; }
        //    public float SweepAngle { get; set; }
        //}
        // ... existing code ...
        // ... existing code ...
        // 区域交互
        private ToolTip? toolTip;
        private void HeatMapPanel_MouseMove(object? sender, MouseEventArgs e)
        {
            var panel = sender as Panel;
            if (panel == null || currentPlayer == null) return;
            int courtWidth = panel.Width - 100, courtHeight = panel.Height - 100, courtX = 50, courtY = 50;
            var zones = GetProfessionalHeatZones(courtX, courtY, courtWidth, courtHeight);
            var heatMapData = analysisService.GetHeatMapData(currentPlayer.PlayerID);
            foreach (var zone in zones)
            {
                if (IsPointInZone(e.Location, zone))
                {
                    var zoneData = heatMapData.FirstOrDefault(z => z.Zone.ZoneName == zone.Name);
                    if (zoneData != null)
                    {
                        var tooltipText = $"{zone.Name}\n投篮数: {zoneData.TotalShots}\n命中数: {zoneData.MadeShots}\n命中率: {zoneData.ShootingPercentage:F1}%\n热力值: {zoneData.HeatValue:F1}";
                        ShowToolTip(panel, tooltipText, e.Location);
                        return;
                    }
                }
            }
            HideToolTip();
        }
        private void HeatMapPanel_MouseClick(object? sender, MouseEventArgs e)
        {
            var panel = sender as Panel;
            if (panel == null || currentPlayer == null) return;
            int courtWidth = panel.Width - 100, courtHeight = panel.Height - 100, courtX = 50, courtY = 50;
            var zones = GetProfessionalHeatZones(courtX, courtY, courtWidth, courtHeight);
            var heatMapData = analysisService.GetHeatMapData(currentPlayer.PlayerID);
            foreach (var zone in zones)
            {
                if (IsPointInZone(e.Location, zone))
                {
                    var zoneData = heatMapData.FirstOrDefault(z => z.Zone.ZoneName == zone.Name);
                    if (zoneData != null)
                    {
                        ShowZoneDetails(zone, zoneData);
                        return;
                    }
                }
            }
        }
        //private bool IsPointInZone(Point point, HeatZoneInfo zone)
        //{
        //    if (zone.Points.Length < 3) return false;

        //    using var path = new GraphicsPath();
        //    path.AddPolygon(zone.Points);
        //    return path.IsVisible(point);
        //}//original
        private bool IsPointInZone(Point point, HeatZoneInfo zone)
        {
            return zone.Path.IsVisible(point);
        }
        private void ShowZoneDetails(HeatZoneInfo zone, AnalysisService.ZoneStatistics zoneData)
        {
            var details = $"区域详情：{zone.Name}\n\n" +
                         $"投篮统计：\n" +
                         $"• 总投篮数：{zoneData.TotalShots}\n" +
                         $"• 命中数：{zoneData.MadeShots}\n" +
                         $"• 命中率：{zoneData.ShootingPercentage:F1}%\n" +
                         $"• 热力值：{zoneData.HeatValue:F1}\n\n" +
                         $"建议：\n" +
                         $"• 在此区域多练习以提高命中率\n" +
                         $"• 分析投篮姿势和力度\n" +
                         $"• 结合疲劳度分析训练效果";
            MessageBox.Show(details, "区域详情", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void ShowToolTip(Control control, string text, Point location)
        {
            if (toolTip == null)
                toolTip = new ToolTip { AutoPopDelay = 3000, InitialDelay = 500, ReshowDelay = 100, ShowAlways = true };
            toolTip.SetToolTip(control, text);
        }
        private void HideToolTip()
        {
            toolTip?.RemoveAll();
        }
        // ... existing code ...

        private void PosturePanel_Paint(object? sender, PaintEventArgs e)
        {
            if (currentPlayer == null) return;

            var panel = sender as Panel;
            if (panel == null) return;

            var g = e.Graphics;

            // 绘制简化的2D投篮姿势
            DrawOptimalPosture(g, panel.ClientRectangle);
        }

        private void DrawOptimalPosture(Graphics g, Rectangle bounds)
        {
            try
            {
                var optimizations = analysisService.AnalyzeOptimalPosture(currentPlayer.PlayerID);

                // 获取最优参数（如无数据则用默认值）
                double kneeFlexion = optimizations.FirstOrDefault(x => x.PostureAspect == "膝盖弯曲角度")?.OptimalValue ?? 140;
                double shotAngle = optimizations.FirstOrDefault(x => x.PostureAspect == "出手角度")?.OptimalValue ?? 45;
                double shoulderAngle = optimizations.FirstOrDefault(x => x.PostureAspect == "肩膀角度")?.OptimalValue ?? 90;
                double hipAngle = optimizations.FirstOrDefault(x => x.PostureAspect == "髋部角度")?.OptimalValue ?? 90;
                double elbowAngle = optimizations.FirstOrDefault(x => x.PostureAspect == "肘部角度")?.OptimalValue ?? 90;
                double wristSnap = optimizations.FirstOrDefault(x => x.PostureAspect == "手腕下压角度")?.OptimalValue ?? 15;

                // 坐标基准
                int centerX = bounds.Width / 2;
                int centerY = bounds.Height / 2 + 40; // 稍微下移，留空间画手臂

                Pen bodyPen = new Pen(Color.Blue, 3);

                // 头部
                int headRadius = 18;
                g.DrawEllipse(bodyPen, centerX - headRadius, centerY - 120, headRadius * 2, headRadius * 2);

                // 躯干
                int bodyLength = 60;
                g.DrawLine(bodyPen, centerX, centerY - 100 + headRadius, centerX, centerY - 100 + headRadius + bodyLength);

                // 髋部点
                int hipY = centerY - 100 + headRadius + bodyLength;

                // 髋部角度影响腿的分叉
                double hipRad = Math.PI * hipAngle / 180.0;
                int hipOffset = (int)(20 * Math.Sin(hipRad - Math.PI / 2));

                // 左腿（膝盖弯曲）
                double kneeRad = Math.PI * kneeFlexion / 180.0;
                int leftLegX = centerX - 18 + hipOffset;
                int leftLegY = hipY + (int)(40 * Math.Cos(kneeRad - Math.PI / 2));
                g.DrawLine(bodyPen, centerX, hipY, leftLegX, leftLegY);

                // 右腿（膝盖弯曲，反向）
                int rightLegX = centerX + 18 - hipOffset;
                int rightLegY = hipY + (int)(40 * Math.Cos(kneeRad - Math.PI / 2));
                g.DrawLine(bodyPen, centerX, hipY, rightLegX, rightLegY);

                // 肩部点
                int shoulderY = centerY - 100 + headRadius + 10;

                // 肩膀角度影响手臂分叉
                double shoulderRad = Math.PI * shoulderAngle / 180.0;
                int shoulderOffset = (int)(30 * Math.Sin(shoulderRad - Math.PI / 2));

                // 左臂（肘部角度）
                //double leftElbowRad = Math.PI * elbowAngle / 180.0;
                //int leftElbowX = centerX - 25 + shoulderOffset;
                //int leftElbowY = shoulderY + (int)(25 * Math.Cos(leftElbowRad - Math.PI / 2));
                //g.DrawLine(bodyPen, centerX, shoulderY, leftElbowX, leftElbowY);

                // 右臂（出手角度、肘部、手腕）
                double rightShoulderRad = Math.PI * shotAngle / 180.0;
                int rightElbowX = centerX + 25 - shoulderOffset;
                int rightElbowY = shoulderY - (int)(25 * Math.Sin(rightShoulderRad));
                g.DrawLine(bodyPen, centerX, shoulderY, rightElbowX, rightElbowY);

                // 右前臂（肘部到手腕）
                double rightForearmRad = Math.PI * (shotAngle - elbowAngle / 2) / 180.0;
                int rightWristX = rightElbowX + (int)(30 * Math.Cos(rightForearmRad));
                int rightWristY = rightElbowY - (int)(30 * Math.Sin(rightForearmRad));
                g.DrawLine(bodyPen, rightElbowX, rightElbowY, rightWristX, rightWristY);

                // 右手（手腕下压角度）
                double wristRad = Math.PI * (shotAngle - elbowAngle / 2 - wristSnap) / 180.0;
                int rightHandX = rightWristX + (int)(15 * Math.Cos(wristRad));
                int rightHandY = rightWristY - (int)(15 * Math.Sin(wristRad));
                g.DrawLine(bodyPen, rightWristX, rightWristY, rightHandX, rightHandY);

                // 篮球
                g.FillEllipse(Brushes.Orange, rightHandX - 6, rightHandY - 6, 12, 12);

                // 显示参数
                var font = new Font("Arial", 10);
                g.DrawString($"膝盖弯曲: {kneeFlexion:F1}°", font, Brushes.Black, 10, 10);
                g.DrawString($"出手角度: {shotAngle:F1}°", font, Brushes.Black, 10, 30);
                g.DrawString($"肩膀角度: {shoulderAngle:F1}°", font, Brushes.Black, 10, 50);
                g.DrawString($"髋部角度: {hipAngle:F1}°", font, Brushes.Black, 10, 70);
                g.DrawString($"肘部角度: {elbowAngle:F1}°", font, Brushes.Black, 10, 90);
                g.DrawString($"手腕下压: {wristSnap:F1}°", font, Brushes.Black, 10, 110);

                bodyPen.Dispose();
            }
            catch (Exception ex)
            {
                g.DrawString($"姿势显示失败：{ex.Message}", new Font("Arial", 10), Brushes.Red, bounds.Location);
            }
        }

        private void ExportPdfButton_Click(object? sender, EventArgs e)
        {
            if (!aiRecommendationsReady)
            {
                MessageBox.Show("请先生成AI智能建议！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (currentPlayer == null)
            {
                MessageBox.Show("请先选择球员", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "PDF文件|*.pdf";
                saveFileDialog.Title = "导出球员报告";
                saveFileDialog.FileName = $"{currentPlayer.PlayerName}_篮球分析报告_{DateTime.Now:yyyyMMdd}.pdf";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        statusLabel.Text = "正在生成PDF报告...";

                        // 创建报告服务实例
                        var pdfService = new PdfReportService();
                        pdfService.GeneratePlayerReport(currentPlayer, saveFileDialog.FileName, aiRecommendationsCache, analysisService);

                        statusLabel.Text = "PDF报告生成成功";
                        MessageBox.Show($"报告已成功导出至: {saveFileDialog.FileName}", "导出成功",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // 可选：自动打开PDF
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"导出报告时出错: {ex.Message}", "错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        statusLabel.Text = "报告导出失败";
                    }
                }
            }
        }

        //private void ShareHeatMapButton_Click(object? sender, EventArgs e)
        //{
        //    // TODO: 实现社区分享功能
        //    MessageBox.Show("社区分享功能待实现", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //}
        private async void GenerateAIButton_Click(object? sender, EventArgs e)
        {
            if (currentPlayer == null)
            {
                MessageBox.Show("请先选择球员", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                statusLabel.Text = "正在生成AI智能建议...";
                generateAIButton.Enabled = false;
                aiRecommendationsCache = await analysisService.GenerateIntelligentRecommendations(currentPlayer.PlayerID);

                var recommendationsListView = FindControlByTag("RecommendationsList") as ListView;
                if (recommendationsListView != null)
                {
                    recommendationsListView.Items.Clear();
                    foreach (var rec in aiRecommendationsCache)
                    {
                        var item = new ListViewItem(rec.RecommendationType);
                        item.SubItems.Add(rec.Priority.ToString());
                        item.SubItems.Add(rec.Description);
                        item.SubItems.Add(rec.Status);
                        item.SubItems.Add(rec.CreatedDate.ToString("yyyy-MM-dd"));
                        recommendationsListView.Items.Add(item);
                    }
                }
                aiRecommendationsReady = true;
                exportButton.Enabled = true;
                statusLabel.Text = "AI智能建议生成完成";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取AI建议失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "AI建议生成失败";
            }
            finally
            {
                generateAIButton.Enabled = true;
            }
        }
    }
}

