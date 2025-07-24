using BasketballAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasketballAnalyzer.Services
{
    public class AnalysisService
    {
        private readonly DatabaseService databaseService;

        public AnalysisService(DatabaseService databaseService)
        {
            this.databaseService = databaseService;
        }

        public class ZoneStatistics
        {
            public HeatZone Zone { get; set; } = new HeatZone();
            public int TotalShots { get; set; }
            public int MadeShots { get; set; }
            public double ShootingPercentage => TotalShots > 0 ? (double)MadeShots / TotalShots * 100 : 0;
            public double HeatValue => Math.Min(ShootingPercentage + (TotalShots * 2), 100); // 综合命中率和投篮数量
        }

        public class PostureOptimization
        {
            public string PostureAspect { get; set; } = string.Empty;
            public double OptimalValue { get; set; }
            public double CurrentAverage { get; set; }
            public double ExpectedImprovement { get; set; }
            public string Recommendation { get; set; } = string.Empty;
        }

        public class TimeSeriesPoint
        {
            public DateTime Time { get; set; }
            public double ShootingPercentage { get; set; }
            public int FatigueLevel { get; set; }
            public int ShotCount { get; set; }
        }

        public List<ZoneStatistics> GetHeatMapData(int playerId, int? sessionId = null)
        {
            var shots = databaseService.GetShotRecords(playerId, sessionId);
            var zones = databaseService.GetHeatZones();
            
            var zoneStats = new List<ZoneStatistics>();
            
            foreach (var zone in zones)
            {
                var shotsInZone = shots.Where(s => 
                    s.CourtX >= zone.MinX && s.CourtX <= zone.MaxX &&
                    s.CourtY >= zone.MinY && s.CourtY <= zone.MaxY).ToList();
                
                var stats = new ZoneStatistics
                {
                    Zone = zone,
                    TotalShots = shotsInZone.Count,
                    MadeShots = shotsInZone.Count(s => s.ShotResult)
                };
                
                zoneStats.Add(stats);
            }
            
            return zoneStats;
        }

        public List<PostureOptimization> AnalyzeOptimalPosture(int playerId)
        {
            var shots = databaseService.GetShotRecords(playerId);
            var postureData = databaseService.GetPostureAnalysis(playerId);
            
            // 将投篮记录与姿势分析数据关联
            var combinedData = from shot in shots
                              join posture in postureData on shot.ShotID equals posture.ShotID
                              select new { shot, posture };
            
            var madeShots = combinedData.Where(x => x.shot.ShotResult).ToList();
            var missedShots = combinedData.Where(x => !x.shot.ShotResult).ToList();
            
            var optimizations = new List<PostureOptimization>();
            
            // 分析膝盖弯曲角度
            if (madeShots.Any(x => x.shot.KneeFlexion.HasValue))
            {
                var optimalKneeFlexion = madeShots
                    .Where(x => x.shot.KneeFlexion.HasValue)
                    .Average(x => x.shot.KneeFlexion!.Value);
                    
                var currentAverage = shots
                    .Where(x => x.KneeFlexion.HasValue)
                    .Average(x => x.KneeFlexion!.Value);
                
                optimizations.Add(new PostureOptimization
                {
                    PostureAspect = "膝盖弯曲角度",
                    OptimalValue = optimalKneeFlexion,
                    CurrentAverage = currentAverage,
                    ExpectedImprovement = CalculateImprovementPotential(madeShots.Count, shots.Count, optimalKneeFlexion, currentAverage),
                    Recommendation = $"建议将膝盖弯曲角度调整至 {optimalKneeFlexion:F1}° 左右"
                });
            }
            
            // 分析出手角度
            if (madeShots.Any(x => x.shot.ShotAngle.HasValue))
            {
                var optimalShotAngle = madeShots
                    .Where(x => x.shot.ShotAngle.HasValue)
                    .Average(x => x.shot.ShotAngle!.Value);
                    
                var currentAverage = shots
                    .Where(x => x.ShotAngle.HasValue)
                    .Average(x => x.ShotAngle!.Value);
                
                optimizations.Add(new PostureOptimization
                {
                    PostureAspect = "出手角度",
                    OptimalValue = optimalShotAngle,
                    CurrentAverage = currentAverage,
                    ExpectedImprovement = CalculateImprovementPotential(madeShots.Count, shots.Count, optimalShotAngle, currentAverage),
                    Recommendation = $"建议将出手角度调整至 {optimalShotAngle:F1}° 左右"
                });
            }
            
            // 分析出手力度
            if (madeShots.Any(x => x.shot.ShotForce.HasValue))
            {
                var optimalShotForce = madeShots
                    .Where(x => x.shot.ShotForce.HasValue)
                    .Average(x => x.shot.ShotForce!.Value);
                    
                var currentAverage = shots
                    .Where(x => x.ShotForce.HasValue)
                    .Average(x => x.ShotForce!.Value);
                
                optimizations.Add(new PostureOptimization
                {
                    PostureAspect = "出手力度",
                    OptimalValue = optimalShotForce,
                    CurrentAverage = currentAverage,
                    ExpectedImprovement = CalculateImprovementPotential(madeShots.Count, shots.Count, optimalShotForce, currentAverage),
                    Recommendation = $"建议将出手力度控制在 {optimalShotForce:F1} 左右"
                });
            }
            
            // 分析手腕下压角度
            if (madeShots.Any(x => x.shot.WristSnap.HasValue))
            {
                var optimalWristSnap = madeShots
                    .Where(x => x.shot.WristSnap.HasValue)
                    .Average(x => x.shot.WristSnap!.Value);
                    
                var currentAverage = shots
                    .Where(x => x.WristSnap.HasValue)
                    .Average(x => x.WristSnap!.Value);
                
                optimizations.Add(new PostureOptimization
                {
                    PostureAspect = "手腕下压角度",
                    OptimalValue = optimalWristSnap,
                    CurrentAverage = currentAverage,
                    ExpectedImprovement = CalculateImprovementPotential(madeShots.Count, shots.Count, optimalWristSnap, currentAverage),
                    Recommendation = $"建议将手腕下压角度调整至 {optimalWristSnap:F1}° 左右"
                });
            }
            // 分析肩膀角度
            if (madeShots.Any(x => x.posture.ShoulderAngle.HasValue))
            {
                var optimalShoulder = madeShots.Where(x => x.posture.ShoulderAngle.HasValue).Average(x => x.posture.ShoulderAngle!.Value);
                var currentAverage = postureData.Where(x => x.ShoulderAngle.HasValue).Average(x => x.ShoulderAngle!.Value);
                optimizations.Add(new PostureOptimization
                {
                    PostureAspect = "肩膀角度",
                    OptimalValue = optimalShoulder,
                    CurrentAverage = currentAverage,
                    ExpectedImprovement = CalculateImprovementPotential(madeShots.Count, shots.Count, optimalShoulder, currentAverage),
                    Recommendation = $"建议肩膀角度保持在 {optimalShoulder:F1}° 左右"
                });
            }

            // 分析髋部角度
            if (madeShots.Any(x => x.posture.HipAngle.HasValue))
            {
                var optimalHip = madeShots.Where(x => x.posture.HipAngle.HasValue).Average(x => x.posture.HipAngle!.Value);
                var currentAverage = postureData.Where(x => x.HipAngle.HasValue).Average(x => x.HipAngle!.Value);
                optimizations.Add(new PostureOptimization
                {
                    PostureAspect = "髋部角度",
                    OptimalValue = optimalHip,
                    CurrentAverage = currentAverage,
                    ExpectedImprovement = CalculateImprovementPotential(madeShots.Count, shots.Count, optimalHip, currentAverage),
                    Recommendation = $"建议髋部角度保持在 {optimalHip:F1}° 左右"
                });
            }

            // 分析肘部角度
            if (madeShots.Any(x => x.shot.ElbowAngle.HasValue))
            {
                var optimalElbow = madeShots.Where(x => x.shot.ElbowAngle.HasValue).Average(x => x.shot.ElbowAngle!.Value);
                var currentAverage = shots.Where(x => x.ElbowAngle.HasValue).Average(x => x.ElbowAngle!.Value);
                optimizations.Add(new PostureOptimization
                {
                    PostureAspect = "肘部角度",
                    OptimalValue = optimalElbow,
                    CurrentAverage = currentAverage,
                    ExpectedImprovement = CalculateImprovementPotential(madeShots.Count, shots.Count, optimalElbow, currentAverage),
                    Recommendation = $"建议肘部角度保持在 {optimalElbow:F1}° 左右"
                });
            }

            return optimizations.OrderByDescending(x => x.ExpectedImprovement).ToList();
        }

        public List<TimeSeriesPoint> GetFatigueAnalysis(int playerId, int? sessionId = null)
        {
            var sessions = sessionId.HasValue
                ? databaseService.GetTrainingSessions(playerId).Where(s => s.SessionID == sessionId.Value).ToList()
                : databaseService.GetTrainingSessions(playerId);

            var timeSeriesData = new List<TimeSeriesPoint>();

            foreach (var session in sessions.OrderBy(x => x.SessionDate))
            {
                var sessionShots = databaseService.GetShotRecords(playerId, session.SessionID);

                var duration = session.Duration ?? 90;
                var segmentSize = duration / 5;

                for (int i = 0; i < 5; i++)
                {
                    var segmentStart = session.SessionDate.AddMinutes(i * segmentSize);
                    var segmentEnd = session.SessionDate.AddMinutes((i + 1) * segmentSize);

                    var segmentShots = sessionShots.Where(s =>
                        s.ShotTime >= segmentStart && s.ShotTime < segmentEnd).ToList();

                    if (segmentShots.Any())
                    {
                        var shootingPercentage = (double)segmentShots.Count(s => s.ShotResult) / segmentShots.Count * 100;
                        var estimatedFatigue = session.FatigueLevel ?? 3 + i;

                        timeSeriesData.Add(new TimeSeriesPoint
                        {
                            Time = segmentStart,
                            ShootingPercentage = shootingPercentage,
                            FatigueLevel = Math.Min(estimatedFatigue, 10),
                            ShotCount = segmentShots.Count
                        });
                    }
                }
            }

            return timeSeriesData.OrderBy(x => x.Time).ToList();
        }
        public string GenerateSummaryForCurrentSession(int playerId)
        {
            var sessions = databaseService.GetTrainingSessions(playerId);
            var lastSession = sessions.OrderByDescending(s => s.SessionDate).FirstOrDefault();
            if (lastSession == null) return "暂无训练数据";

            var shots = databaseService.GetShotRecords(playerId, lastSession.SessionID);
            double shootingPct = shots.Count > 0 ? shots.Count(s => s.ShotResult) * 100.0 / shots.Count : 0;
            string fatigue = lastSession.FatigueLevel.HasValue ? lastSession.FatigueLevel.Value.ToString() : "未知";

            return $"训练日期：{lastSession.SessionDate:yyyy-MM-dd}\n" +
                   $"投篮数：{shots.Count}，命中率：{shootingPct:F1}%\n" +
                   $"疲劳程度：{fatigue}\n" +
                   $"备注：{lastSession.Notes ?? "无"}\n" +
                   $"本次训练表现{(shootingPct > 60 ? "优秀" : shootingPct > 40 ? "良好" : "需加强")}。";
        }

        public Dictionary<string, object> GetPlayerStatistics(int playerId)
        {
            var shots = databaseService.GetShotRecords(playerId);
            var sessions = databaseService.GetTrainingSessions(playerId);
            
            var stats = new Dictionary<string, object>();
            
            // 基本统计
            stats["总投篮数"] = shots.Count;
            stats["命中数"] = shots.Count(s => s.ShotResult);
            stats["总命中率"] = shots.Count > 0 ? (double)shots.Count(s => s.ShotResult) / shots.Count * 100 : 0;
            
            // 分类统计
            var threePointers = shots.Where(s => s.ShotType == "三分").ToList();
            var twoPointers = shots.Where(s => s.ShotType == "两分").ToList();
            
            stats["三分投篮数"] = threePointers.Count;
            stats["三分命中率"] = threePointers.Count > 0 ? (double)threePointers.Count(s => s.ShotResult) / threePointers.Count * 100 : 0;
            stats["两分投篮数"] = twoPointers.Count;
            stats["两分命中率"] = twoPointers.Count > 0 ? (double)twoPointers.Count(s => s.ShotResult) / twoPointers.Count * 100 : 0;
            
            // 训练统计
            stats["训练场次"] = sessions.Count;
            var validDurations = sessions.Where(s => s.Duration.HasValue).Select(s => s.Duration.Value);
            stats["平均训练时长"] = validDurations.Any() ? validDurations.Average() : 0;

            var validFatigues = sessions.Where(s => s.FatigueLevel.HasValue).Select(s => s.FatigueLevel.Value);
            stats["平均疲劳程度"] = validFatigues.Any() ? validFatigues.Average() : 0;

            return stats;
        }

        private double CalculateImprovementPotential(int madeShots, int totalShots, double optimalValue, double currentValue)
        {
            var currentPercentage = totalShots > 0 ? (double)madeShots / totalShots * 100 : 0;
            var valueDifference = Math.Abs(optimalValue - currentValue);
            
            // 基于当前命中率和数值差异计算改进潜力
            var improvementFactor = Math.Min(valueDifference / 10.0, 1.0); // 标准化到0-1
            var potentialImprovement = (100 - currentPercentage) * improvementFactor * 0.3; // 最多改进30%
            
            return potentialImprovement;
        }
        // 添加新方法
        public async Task<List<TrainingRecommendation>> GenerateIntelligentRecommendations(int playerId)
        {
            // 获取球员数据
            var stats = GetPlayerStatistics(playerId);
            var postureAnalysis = AnalyzeOptimalPosture(playerId);

            // 格式化数据用于提示
            var statsText = FormatStatistics(stats);
            var postureText = FormatPostureAnalysis(postureAnalysis);

            // 调用DeepSeek服务
            var deepSeekService = new DeepSeekService();
            var recommendationsText = await deepSeekService.GetTrainingRecommendationsAsync(playerId, statsText, postureText);

            // 解析并返回建议
            return ParseRecommendations(recommendationsText);
        }

        private string FormatStatistics(Dictionary<string, object> stats)
        {
            var sb = new StringBuilder();
            foreach (var stat in stats)
            {
                sb.AppendLine($"{stat.Key}: {stat.Value}");
            }
            return sb.ToString();
        }

        private string FormatPostureAnalysis(List<PostureOptimization> postureAnalysis)
        {
            var sb = new StringBuilder();
            foreach (var item in postureAnalysis)
            {
                sb.AppendLine($"{item.PostureAspect} - 最优值: {item.OptimalValue:F1}, 当前值: {item.CurrentAverage:F1}");
            }
            return sb.ToString();
        }

        private List<TrainingRecommendation> ParseRecommendations(string recommendationsText)
        {
            var recommendations = new List<TrainingRecommendation>();
            int priority = 1;

            // 简单分割建议文本
            var lines = recommendationsText.Split('\n');
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line) && line.Length > 10) // 过滤空行和过短行
                {
                    recommendations.Add(new TrainingRecommendation
                    {
                        RecommendationType = "智能建议",
                        Description = line.Trim(),
                        Priority = priority++,
                        Status = "Active",
                        CreatedDate = DateTime.Now
                    });
                }
            }

            return recommendations;
        }

        public string AnalyzeShootingStyle(int playerId)
        {
            var shots = databaseService.GetShotRecords(playerId);
            if (shots.Count == 0) return "暂无投篮数据，无法分析投篮风格。";

            int threeCount = shots.Count(s => s.ShotType == "三分");
            int twoCount = shots.Count(s => s.ShotType == "两分");
            double threePct = shots.Count > 0 ? threeCount * 100.0 / shots.Count : 0;
            double twoPct = shots.Count > 0 ? twoCount * 100.0 / shots.Count : 0;

            // 区域分布（假设ZoneName已标准化）
            var leftCorner = shots.Count(s => s.CourtX < 7 && s.CourtY > 10);
            var rightCorner = shots.Count(s => s.CourtX > 21 && s.CourtY > 10);
            var arcTop = shots.Count(s => s.CourtX > 10 && s.CourtX < 18 && s.CourtY < 7);

            string style = "";
            if (threePct > 50)
                style += "偏好三分投射，";
            else if (twoPct > 70)
                style += "偏好中近距离投篮，";
            else
                style += "投篮分布均衡，";

            if (leftCorner > rightCorner * 1.2)
                style += "左侧底角出手较多，";
            else if (rightCorner > leftCorner * 1.2)
                style += "右侧底角出手较多，";
            else if (arcTop > leftCorner + rightCorner)
                style += "弧顶出手较多，";

            double overallPct = shots.Count > 0 ? shots.Count(s => s.ShotResult) * 100.0 / shots.Count : 0;
            style += $"整体命中率为{overallPct:F1}%。";

            return "【投篮风格分析】" + style.Trim('，');
        }
    }
}