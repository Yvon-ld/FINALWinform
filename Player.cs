using System;

namespace BasketballAnalyzer.Models
{
    public class Player
    {
        public int PlayerID { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int? Age { get; set; }
        public double? Height { get; set; }
        public double? Weight { get; set; }
        public string? Position { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class ShotRecord
    {
        public int ShotID { get; set; }
        public int PlayerID { get; set; }
        public int SessionID { get; set; }
        public DateTime ShotTime { get; set; }
        public double CourtX { get; set; }
        public double CourtY { get; set; }
        public double? ShotAngle { get; set; }
        public double? ShotForce { get; set; }
        public double? KneeFlexion { get; set; }
        public double? ElbowAngle { get; set; }
        public double? WristSnap { get; set; }
        public double? ReleaseHeight { get; set; }
        public bool ShotResult { get; set; }
        public string? ShotType { get; set; }
        public double? DefenseDistance { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class TrainingSession
    {
        public int SessionID { get; set; }
        public int PlayerID { get; set; }
        public DateTime SessionDate { get; set; }
        public int? Duration { get; set; }
        public int TotalShots { get; set; }
        public int MadeShots { get; set; }
        public int? FatigueLevel { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }

        public double ShootingPercentage => TotalShots > 0 ? (double)MadeShots / TotalShots * 100 : 0;
    }

    public class PostureAnalysis
    {
        public int AnalysisID { get; set; }
        public int ShotID { get; set; }
        public double? ShoulderAngle { get; set; }
        public double? HipAngle { get; set; }
        public string? FootPosition { get; set; }
        public double? Balance { get; set; }
        public double? FollowThrough { get; set; }
        public double? OverallScore { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class HeatZone
    {
        public int ZoneID { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public double MinX { get; set; }
        public double MaxX { get; set; }
        public double MinY { get; set; }
        public double MaxY { get; set; }
        public string? ZoneType { get; set; }
    }

    public class CommunityPost
    {
        public int PostID { get; set; }
        public int PlayerID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public byte[]? HeatMapImage { get; set; }
        public int Likes { get; set; }
        public int Views { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class TrainingRecommendation
    {
        public int RecommendationID { get; set; }
        public int PlayerID { get; set; }
        public string RecommendationType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedDate { get; set; }
    }
}