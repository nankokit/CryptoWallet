using System.Data.SQLite;
using System.IO;

namespace CryptoWallet
{
    public static class DatabaseService
    {
        private static readonly string DbPath = Path.Combine(Directory.GetCurrentDirectory(), "wallet.db");

        public static void InitializeDatabase()
        {
            if (!File.Exists(DbPath))
            {
                SQLiteConnection.CreateFile(DbPath);
            }

            using var connection = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            connection.Open();

            string sql = @"CREATE TABLE IF NOT EXISTS Users (
                            UserId TEXT PRIMARY KEY,
                            SeedHash TEXT NOT NULL
                          )";
            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        public static void SaveUser(string userId, string seedPhrase)
        {
            string seedHash = BCrypt.Net.BCrypt.HashPassword(seedPhrase);
            using var connection = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            connection.Open();
            string sql = "INSERT OR REPLACE INTO Users (UserId, SeedHash) VALUES (@userId, @seedHash)";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@seedHash", seedHash);
            command.ExecuteNonQuery();
        }

        public static bool VerifyUser(string userId, string seedPhrase)
        {
            using var connection = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            connection.Open();
            string sql = "SELECT SeedHash FROM Users WHERE UserId = @userId";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@userId", userId);
            string storedHash = (string)command.ExecuteScalar();

            return storedHash != null && BCrypt.Net.BCrypt.Verify(seedPhrase, storedHash);
        }
    }
}