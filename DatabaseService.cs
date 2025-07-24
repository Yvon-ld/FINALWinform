using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using BasketballAnalyzer.Models;

namespace BasketballAnalyzer.Services
{
    public class DatabaseService
    {
        private readonly string connectionString;

        public DatabaseService(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public List<Player> GetAllPlayers()
        {
            var players = new List<Player>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = new SqlCommand("SELECT * FROM Players ORDER BY PlayerName", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                players.Add(new Player
                {
                    PlayerID = reader.GetInt32("PlayerID"),
                    PlayerName = reader.GetString("PlayerName"),
                    Age = reader.IsDBNull("Age") ? null : reader.GetInt32("Age"),
                    Height = reader.IsDBNull("Height") ? null : reader.GetDouble("Height"),
                    Weight = reader.IsDBNull("Weight") ? null : reader.GetDouble("Weight"),
                    Position = reader.IsDBNull("Position") ? null : reader.GetString("Position"),
                    CreatedDate = reader.GetDateTime("CreatedDate"),
                    UpdatedDate = reader.GetDateTime("UpdatedDate")
                });
            }

            return players;
        }

        public List<ShotRecord> GetShotRecords(int playerId, int? sessionId = null)
        {
            var shots = new List<ShotRecord>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            string query = sessionId.HasValue
                ? "SELECT * FROM ShotRecords WHERE PlayerID = @PlayerID AND SessionID = @SessionID ORDER BY ShotTime"
                : "SELECT * FROM ShotRecords WHERE PlayerID = @PlayerID ORDER BY ShotTime";

            var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PlayerID", playerId);
            if (sessionId.HasValue)
                command.Parameters.AddWithValue("@SessionID", sessionId.Value);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                shots.Add(new ShotRecord
                {
                    ShotID = reader.GetInt32("ShotID"),
                    PlayerID = reader.GetInt32("PlayerID"),
                    SessionID = reader.GetInt32("SessionID"),
                    ShotTime = reader.GetDateTime("ShotTime"),
                    CourtX = reader.GetDouble("CourtX"),
                    CourtY = reader.GetDouble("CourtY"),
                    ShotAngle = reader.IsDBNull("ShotAngle") ? null : reader.GetDouble("ShotAngle"),
                    ShotForce = reader.IsDBNull("ShotForce") ? null : reader.GetDouble("ShotForce"),
                    KneeFlexion = reader.IsDBNull("KneeFlexion") ? null : reader.GetDouble("KneeFlexion"),
                    ElbowAngle = reader.IsDBNull("ElbowAngle") ? null : reader.GetDouble("ElbowAngle"),
                    WristSnap = reader.IsDBNull("WristSnap") ? null : reader.GetDouble("WristSnap"),
                    ReleaseHeight = reader.IsDBNull("ReleaseHeight") ? null : reader.GetDouble("ReleaseHeight"),
                    ShotResult = reader.GetBoolean("ShotResult"),
                    ShotType = reader.IsDBNull("ShotType") ? null : reader.GetString("ShotType"),
                    DefenseDistance = reader.IsDBNull("DefenseDistance") ? null : reader.GetDouble("DefenseDistance"),
                    CreatedDate = reader.GetDateTime("CreatedDate")
                });
            }

            return shots;
        }

        public List<TrainingSession> GetTrainingSessions(int playerId)
        {
            var sessions = new List<TrainingSession>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = new SqlCommand("SELECT * FROM TrainingSessions WHERE PlayerID = @PlayerID ORDER BY SessionDate DESC", connection);
            command.Parameters.AddWithValue("@PlayerID", playerId);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                sessions.Add(new TrainingSession
                {
                    SessionID = reader.GetInt32("SessionID"),
                    PlayerID = reader.GetInt32("PlayerID"),
                    SessionDate = reader.GetDateTime("SessionDate"),
                    Duration = reader.IsDBNull("Duration") ? null : reader.GetInt32("Duration"),
                    TotalShots = reader.GetInt32("TotalShots"),
                    MadeShots = reader.GetInt32("MadeShots"),
                    FatigueLevel = reader.IsDBNull("FatigueLevel") ? null : reader.GetInt32("FatigueLevel"),
                    Notes = reader.IsDBNull("Notes") ? null : reader.GetString("Notes"),
                    CreatedDate = reader.GetDateTime("CreatedDate")
                });
            }

            return sessions;
        }

        public int AddTrainingSession(TrainingSession session)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                INSERT INTO TrainingSessions (PlayerID, SessionDate, Duration, TotalShots, MadeShots, FatigueLevel, Notes)
                OUTPUT INSERTED.SessionID
                VALUES (@PlayerID, @SessionDate, @Duration, @TotalShots, @MadeShots, @FatigueLevel, @Notes)", connection);

            command.Parameters.AddWithValue("@PlayerID", session.PlayerID);
            command.Parameters.AddWithValue("@SessionDate", session.SessionDate);
            command.Parameters.AddWithValue("@Duration", session.Duration ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@TotalShots", session.TotalShots);
            command.Parameters.AddWithValue("@MadeShots", session.MadeShots);
            command.Parameters.AddWithValue("@FatigueLevel", session.FatigueLevel ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Notes", session.Notes ?? (object)DBNull.Value);

            return (int)command.ExecuteScalar();
        }

        public void AddShotRecord(ShotRecord shot)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                INSERT INTO ShotRecords (PlayerID, SessionID, ShotTime, CourtX, CourtY, ShotAngle, ShotForce, KneeFlexion, ElbowAngle, WristSnap, ReleaseHeight, ShotResult, ShotType, DefenseDistance)
                VALUES (@PlayerID, @SessionID, @ShotTime, @CourtX, @CourtY, @ShotAngle, @ShotForce, @KneeFlexion, @ElbowAngle, @WristSnap, @ReleaseHeight, @ShotResult, @ShotType, @DefenseDistance)", connection);

            command.Parameters.AddWithValue("@PlayerID", shot.PlayerID);
            command.Parameters.AddWithValue("@SessionID", shot.SessionID);
            command.Parameters.AddWithValue("@ShotTime", shot.ShotTime);
            command.Parameters.AddWithValue("@CourtX", shot.CourtX);
            command.Parameters.AddWithValue("@CourtY", shot.CourtY);
            command.Parameters.AddWithValue("@ShotAngle", shot.ShotAngle ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ShotForce", shot.ShotForce ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@KneeFlexion", shot.KneeFlexion ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ElbowAngle", shot.ElbowAngle ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@WristSnap", shot.WristSnap ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ReleaseHeight", shot.ReleaseHeight ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ShotResult", shot.ShotResult);
            command.Parameters.AddWithValue("@ShotType", shot.ShotType ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DefenseDistance", shot.DefenseDistance ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }

        public List<PostureAnalysis> GetPostureAnalysis(int playerId)
        {
            var analyses = new List<PostureAnalysis>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                SELECT pa.* FROM PostureAnalysis pa 
                INNER JOIN ShotRecords sr ON pa.ShotID = sr.ShotID 
                WHERE sr.PlayerID = @PlayerID 
                ORDER BY pa.CreatedDate", connection);
            command.Parameters.AddWithValue("@PlayerID", playerId);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                analyses.Add(new PostureAnalysis
                {
                    AnalysisID = reader.GetInt32("AnalysisID"),
                    ShotID = reader.GetInt32("ShotID"),
                    ShoulderAngle = reader.IsDBNull("ShoulderAngle") ? null : reader.GetDouble("ShoulderAngle"),
                    HipAngle = reader.IsDBNull("HipAngle") ? null : reader.GetDouble("HipAngle"),
                    FootPosition = reader.IsDBNull("FootPosition") ? null : reader.GetString("FootPosition"),
                    Balance = reader.IsDBNull("Balance") ? null : reader.GetDouble("Balance"),
                    FollowThrough = reader.IsDBNull("Follow_Through") ? null : reader.GetDouble("Follow_Through"),
                    OverallScore = reader.IsDBNull("OverallScore") ? null : reader.GetDouble("OverallScore"),
                    CreatedDate = reader.GetDateTime("CreatedDate")
                });
            }

            return analyses;
        }

        public List<HeatZone> GetHeatZones()
        {
            var zones = new List<HeatZone>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = new SqlCommand("SELECT * FROM HeatZones ORDER BY ZoneID", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                zones.Add(new HeatZone
                {
                    ZoneID = reader.GetInt32("ZoneID"),
                    ZoneName = reader.GetString("ZoneName"),
                    MinX = reader.GetDouble("MinX"),
                    MaxX = reader.GetDouble("MaxX"),
                    MinY = reader.GetDouble("MinY"),
                    MaxY = reader.GetDouble("MaxY"),
                    ZoneType = reader.IsDBNull("ZoneType") ? null : reader.GetString("ZoneType")
                });
            }

            return zones;
        }

        public List<TrainingRecommendation> GetTrainingRecommendations(int playerId)
        {
            var recommendations = new List<TrainingRecommendation>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = new SqlCommand("SELECT * FROM TrainingRecommendations WHERE PlayerID = @PlayerID AND Status = 'Active' ORDER BY Priority DESC", connection);
            command.Parameters.AddWithValue("@PlayerID", playerId);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                recommendations.Add(new TrainingRecommendation
                {
                    RecommendationID = reader.GetInt32("RecommendationID"),
                    PlayerID = reader.GetInt32("PlayerID"),
                    RecommendationType = reader.GetString("RecommendationType"),
                    Description = reader.GetString("Description"),
                    Priority = reader.GetInt32("Priority"),
                    Status = reader.GetString("Status"),
                    CreatedDate = reader.GetDateTime("CreatedDate")
                });
            }

            return recommendations;
        }

        public void AddPlayer(Player player)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                INSERT INTO Players (PlayerName, Age, Height, Weight, Position) 
                VALUES (@PlayerName, @Age, @Height, @Weight, @Position)", connection);

            command.Parameters.AddWithValue("@PlayerName", player.PlayerName);
            command.Parameters.AddWithValue("@Age", player.Age ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Height", player.Height ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Weight", player.Weight ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Position", player.Position ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }

        // ×ÜÆÀÏà¹Ø
        public void AddSummary(int playerId, DateTime date, string content)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = new SqlCommand(@"
                INSERT INTO Summaries (PlayerID, Date, Content)
                VALUES (@PlayerID, @Date, @Content)", connection);

            command.Parameters.AddWithValue("@PlayerID", playerId);
            command.Parameters.AddWithValue("@Date", date);
            command.Parameters.AddWithValue("@Content", content);

            command.ExecuteNonQuery();
        }

        public List<(DateTime Date, string Content)> GetSummaries(int playerId)
        {
            var result = new List<(DateTime, string)>();
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            var command = new SqlCommand("SELECT Date, Content FROM Summaries WHERE PlayerID = @PlayerID ORDER BY Date DESC", connection);
            command.Parameters.AddWithValue("@PlayerID", playerId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add((reader.GetDateTime(0), reader.GetString(1)));
            }
            return result;
        }

        public bool TestConnection()
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                connection.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}