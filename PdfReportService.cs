using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BasketballAnalyzer.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace BasketballAnalyzer.Services
{
    public class PdfReportService
    {
        // 中文字体设置
        private static readonly string FontPath = @"C:\Windows\Fonts\msyh.ttc,0"; // 微软雅黑
        private static readonly BaseFont BaseChineseFont = BaseFont.CreateFont(FontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
        private static readonly Font ChineseTitleFont = new Font(BaseChineseFont, 18, Font.BOLD, BaseColor.DARK_GRAY);
        private static readonly Font ChineseSectionFont = new Font(BaseChineseFont, 14, Font.BOLD, BaseColor.BLUE);
        private static readonly Font ChineseNormalFont = new Font(BaseChineseFont, 10, Font.NORMAL, BaseColor.BLACK);
        private static readonly Font ChineseGrayFont = new Font(BaseChineseFont, 10, Font.NORMAL, BaseColor.GRAY);
        private static readonly Font ChineseHeaderFont = new Font(BaseChineseFont, 10, Font.BOLD, BaseColor.WHITE);

        public void GeneratePlayerReport(
            Player player,
            string filePath,
            List<TrainingRecommendation> aiRecommendations,
            AnalysisService analysisService
        )
        {
            try
            {
                var document = new Document(PageSize.A4, 50, 50, 25, 25);
                var writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                document.Open();

                var title = new Paragraph($"篮球数据分析报告 - {player.PlayerName}", ChineseTitleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(title);

                var dateText = new Paragraph($"报告生成时间：{DateTime.Now:yyyy年MM月dd日 HH:mm}", ChineseGrayFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 20
                };
                document.Add(dateText);

                AddPlayerInfoSection(document, player);
                AddStatisticsSection(document, player, analysisService);
                AddPostureAnalysisSection(document, player, analysisService);
                AddRecommendationsSection(document, player, analysisService);
                AddIntelligentRecommendationsSection(document, aiRecommendations);
                AddHeatMapSection(document, player, analysisService);

                document.Close();
            }
            catch (Exception ex)
            {
                throw new Exception($"PDF报告生成失败：{ex.Message}", ex);
            }
        }

        private void AddPlayerInfoSection(Document document, Player player)
        {
            var sectionTitle = new Paragraph("球员信息", ChineseSectionFont)
            {
                SpacingBefore = 15,
                SpacingAfter = 10
            };
            document.Add(sectionTitle);

            var infoTable = new PdfPTable(2)
            {
                WidthPercentage = 100
            };
            infoTable.SetWidths(new float[] { 1, 2 });

            AddTableRow(infoTable, "姓名", player.PlayerName, ChineseNormalFont);
            AddTableRow(infoTable, "年龄", player.Age?.ToString() ?? "未知", ChineseNormalFont);
            AddTableRow(infoTable, "身高", player.Height?.ToString("F1") + "cm" ?? "未知", ChineseNormalFont);
            AddTableRow(infoTable, "体重", player.Weight?.ToString("F1") + "kg" ?? "未知", ChineseNormalFont);
            AddTableRow(infoTable, "位置", player.Position ?? "未知", ChineseNormalFont);
            AddTableRow(infoTable, "注册时间", player.CreatedDate.ToString("yyyy-MM-dd"), ChineseNormalFont);

            document.Add(infoTable);
        }

        private void AddStatisticsSection(Document document, Player player, AnalysisService analysisService)
        {
            var sectionTitle = new Paragraph("数据统计", ChineseSectionFont)
            {
                SpacingBefore = 20,
                SpacingAfter = 10
            };
            document.Add(sectionTitle);

            var stats = analysisService.GetPlayerStatistics(player.PlayerID);

            var statsTable = new PdfPTable(2)
            {
                WidthPercentage = 100
            };
            statsTable.SetWidths(new float[] { 1, 1 });

            foreach (var stat in stats)
            {
                string value = stat.Value is double doubleValue ?
                    (stat.Key.Contains("命中率") ? $"{doubleValue:F1}%" :
                     stat.Key.Contains("时长") ? $"{doubleValue:F0}分钟" :
                     doubleValue.ToString("F1")) :
                    stat.Value.ToString();
                AddTableRow(statsTable, stat.Key, value, ChineseNormalFont);
            }

            document.Add(statsTable);
        }

        private void AddPostureAnalysisSection(Document document, Player player, AnalysisService analysisService)
        {
            var sectionTitle = new Paragraph("姿势分析与优化建议", ChineseSectionFont)
            {
                SpacingBefore = 20,
                SpacingAfter = 10
            };
            document.Add(sectionTitle);

            var optimizations = analysisService.AnalyzeOptimalPosture(player.PlayerID);

            if (optimizations.Any())
            {
                var postureTable = new PdfPTable(4)
                {
                    WidthPercentage = 100
                };
                postureTable.SetWidths(new float[] { 2, 1, 1, 1 });

                AddTableHeader(postureTable, new[] { "姿势要素", "最优值", "当前值", "改进潜力" }, ChineseNormalFont);

                foreach (var opt in optimizations)
                {
                    AddTableRow(postureTable, new[]
                    {
                        opt.PostureAspect,
                        opt.OptimalValue.ToString("F1"),
                        opt.CurrentAverage.ToString("F1"),
                        $"{opt.ExpectedImprovement:F1}%"
                    }, ChineseNormalFont);
                }

                document.Add(postureTable);

                var recommendationTitle = new Paragraph("详细建议", new Font(BaseChineseFont, 12, Font.BOLD, BaseColor.BLACK))
                {
                    SpacingBefore = 15,
                    SpacingAfter = 5
                };
                document.Add(recommendationTitle);

                foreach (var opt in optimizations.Take(3))
                {
                    var recommendation = new Paragraph($"• {opt.Recommendation}", ChineseNormalFont)
                    {
                        SpacingAfter = 5
                    };
                    document.Add(recommendation);
                }
            }
            else
            {
                var noDataText = new Paragraph("暂无足够数据进行姿势分析", ChineseNormalFont);
                document.Add(noDataText);
            }
        }

        private void AddRecommendationsSection(Document document, Player player, AnalysisService analysisService)
        {
            var sectionTitle = new Paragraph("个性化训练建议", ChineseSectionFont)
            {
                SpacingBefore = 20,
                SpacingAfter = 10
            };
            document.Add(sectionTitle);

            try
            {
                var recommendations = analysisService.AnalyzeOptimalPosture(player.PlayerID);

                if (recommendations.Any())
                {
                    var counter = 1;
                    foreach (var rec in recommendations.Take(5))
                    {
                        var bullet = new Paragraph($"{counter}. {rec.PostureAspect}: {rec.Recommendation}", ChineseNormalFont)
                        {
                            SpacingAfter = 8,
                            IndentationLeft = 20
                        };
                        document.Add(bullet);
                        counter++;
                    }
                }
                else
                {
                    var noDataText = new Paragraph("暂无个性化建议，请继续积累训练数据", ChineseNormalFont);
                    document.Add(noDataText);
                }
            }
            catch (Exception ex)
            {
                var errorText = new Paragraph($"获取训练建议时出错：{ex.Message}", ChineseNormalFont);
                document.Add(errorText);
            }
        }

        private void AddHeatMapSection(Document document, Player player, AnalysisService analysisService)
        {
            var sectionTitle = new Paragraph("投篮热力图分析", ChineseSectionFont)
            {
                SpacingBefore = 20,
                SpacingAfter = 10
            };
            document.Add(sectionTitle);

            try
            {
                var heatMapData = analysisService.GetHeatMapData(player.PlayerID);
                var topZones = heatMapData
                    .Where(z => z.TotalShots > 0)
                    .OrderByDescending(z => z.ShootingPercentage)
                    .Take(5)
                    .ToList();

                if (topZones.Any())
                {
                    var heatMapTable = new PdfPTable(4)
                    {
                        WidthPercentage = 100
                    };
                    heatMapTable.SetWidths(new float[] { 2, 1, 1, 1 });

                    AddTableHeader(heatMapTable, new[] { "投篮区域", "投篮数", "命中数", "命中率" }, ChineseNormalFont);

                    foreach (var zone in topZones)
                    {
                        AddTableRow(heatMapTable, new[]
                        {
                            zone.Zone.ZoneName,
                            zone.TotalShots.ToString(),
                            zone.MadeShots.ToString(),
                            $"{zone.ShootingPercentage:F1}%"
                        }, ChineseNormalFont);
                    }

                    document.Add(heatMapTable);

                    var bestZone = topZones.First();
                    var analysisText = new Paragraph($"分析：您在{bestZone.Zone.ZoneName}的表现最佳，命中率达到{bestZone.ShootingPercentage:F1}%，建议多在此区域练习以保持状态。", ChineseNormalFont)
                    {
                        SpacingBefore = 10
                    };
                    document.Add(analysisText);
                }
                else
                {
                    var noDataText = new Paragraph("暂无投篮数据", ChineseNormalFont);
                    document.Add(noDataText);
                }
            }
            catch (Exception ex)
            {
                var errorText = new Paragraph($"获取热力图数据时出错：{ex.Message}", ChineseNormalFont);
                document.Add(errorText);
            }
        }

        private void AddTableRow(PdfPTable table, string key, string value, Font font)
        {
            var keyCell = new PdfPCell(new Phrase(key, font))
            {
                BackgroundColor = BaseColor.LIGHT_GRAY,
                Padding = 8
            };
            var valueCell = new PdfPCell(new Phrase(value, font))
            {
                Padding = 8
            };
            table.AddCell(keyCell);
            table.AddCell(valueCell);
        }

        private void AddTableRow(PdfPTable table, string[] values, Font font)
        {
            foreach (var value in values)
            {
                var cell = new PdfPCell(new Phrase(value, font))
                {
                    Padding = 8
                };
                table.AddCell(cell);
            }
        }

        private void AddTableHeader(PdfPTable table, string[] headers, Font font)
        {
            foreach (var header in headers)
            {
                var cell = new PdfPCell(new Phrase(header, ChineseHeaderFont))
                {
                    BackgroundColor = BaseColor.DARK_GRAY,
                    Padding = 8,
                    HorizontalAlignment = Element.ALIGN_CENTER
                };
                table.AddCell(cell);
            }
        }

        private void AddIntelligentRecommendationsSection(Document document, List<TrainingRecommendation> aiRecommendations)
        {
            var sectionTitle = new Paragraph("AI智能训练建议", ChineseSectionFont)
            {
                SpacingBefore = 20,
                SpacingAfter = 10
            };
            document.Add(sectionTitle);

            if (aiRecommendations != null && aiRecommendations.Any())
            {
                int counter = 1;
                foreach (var rec in aiRecommendations)
                {
                    var bullet = new Paragraph($"{counter}. {rec.Description}", ChineseNormalFont)
                    {
                        SpacingAfter = 8,
                        IndentationLeft = 20
                    };
                    document.Add(bullet);
                    counter++;
                }
            }
            else
            {
                var noDataText = new Paragraph("暂无AI生成建议", ChineseNormalFont);
                document.Add(noDataText);
            }
        }
    }
}
