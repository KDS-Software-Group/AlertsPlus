using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static Noticore.ViewScheduler;

namespace Noticore
{
    public class SettingsRepository
    {
        private string _dbPath = "";

        // database code
        public SettingsRepository()
        {
            _dbPath = GetDatabasePath();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                // Existing Settings Table
                command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Settings (Key TEXT PRIMARY KEY, Value TEXT);
            CREATE TABLE IF NOT EXISTS Notifications (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT,
                Message TEXT,
                TargetTime TEXT,
                IsEnabled INTEGER
            );";
                command.ExecuteNonQuery();
            }
        }


        public void SaveSetting(string key, string value)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "INSERT OR REPLACE INTO Settings (Key, Value) VALUES ($key, $value)";
                command.Parameters.AddWithValue("$key", key);
                command.Parameters.AddWithValue("$value", value);
                command.ExecuteNonQuery();
            }
        }

        public string GetSetting(string key, string defaultValue)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Value FROM Settings WHERE Key = $key";
                command.Parameters.AddWithValue("$key", key);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return reader.GetString(0);
                    }
                }
            }
            return defaultValue;
        }
        
        public T Get<T>(string key, T defaultValue)
        {
            string value = GetSetting(key, defaultValue?.ToString() ?? "");
            return (T)Convert.ChangeType(value, typeof(T));
        }

        // saving classes

        public void UpdateNotification(ScheduledNotification note)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Notifications SET IsEnabled = $enabled WHERE Id = $id";
                command.Parameters.AddWithValue("$enabled", note.IsEnabled ? 1 : 0);
                command.Parameters.AddWithValue("$id", note.Id);
                command.ExecuteNonQuery();
            }
        }

        public void AddScheduledNotification(ScheduledNotification notification)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText =
                @"
            INSERT INTO Notifications (Title, Message, TargetTime, IsEnabled) 
            VALUES ($title, $message, $time, $enabled)
        ";

                command.Parameters.AddWithValue("$title", notification.Title);
                command.Parameters.AddWithValue("$message", notification.Message);
                // We store the time as a string so SQLite can read it easily later
                command.Parameters.AddWithValue("$time", notification.TargetTime.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$enabled", notification.IsEnabled ? 1 : 0);

                command.ExecuteNonQuery();
            }
        }

        public List<ScheduledNotification> GetAllNotifications()
        {
            var list = new List<ScheduledNotification>();
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Notifications";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ScheduledNotification
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            Message = reader.GetString(2),
                            TargetTime = DateTime.Parse(reader.GetString(3)),
                            IsEnabled = reader.GetInt32(4) == 1
                        });
                    }
                }
            }
            return list;
        }

        private string GetDatabasePath()
        {
            // points to C:\Users\whatevername\AppData\Local\Noticore
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Noticore");

            // create the noticore folder if it doesn't exist
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            return Path.Combine(folder, "noticore_settings.db");
        }

        public void DeleteNotification(int id)
        {
            using (var connection = new SqliteConnection($"Data Source={_dbPath}"))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Notifications WHERE Id = $id";
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }
    }
}