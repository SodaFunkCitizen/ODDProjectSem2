using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Data.Sqlite;
using WarhammerArmyBuilder;

namespace WarhammerArmyBuilder.Services
{
    public class ArmyDbService
    {
        private readonly string _dbPath;

        public ArmyDbService(string dbPath)
        {
            _dbPath = dbPath;
            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        private string ConnectionString => $"Data Source={_dbPath}";

        public void Initialize()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                using (var pragma = connection.CreateCommand())
                {
                    pragma.CommandText = "PRAGMA foreign_keys = ON;";
                    pragma.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Armies(
                            Id TEXT PRIMARY KEY,
                            Name TEXT NOT NULL,
                            Faction TEXT NOT NULL,
                            CreatedAtUtc TEXT NOT NULL,
                            LastModifiedUtc TEXT NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS ArmyUnits(
                            Id TEXT PRIMARY KEY,
                            ArmyId TEXT NOT NULL,
                            Name TEXT NOT NULL,
                            BattlefieldRole TEXT NOT NULL,
                            Keywords TEXT NOT NULL,
                            Points INTEGER NOT NULL,
                            CreatedAtUtc TEXT NOT NULL,
                            Notes TEXT NOT NULL,
                            FOREIGN KEY(ArmyId) REFERENCES Armies(Id) ON DELETE CASCADE
                        );
                    ";
                    command.ExecuteNonQuery();
                }
            }
        }

        public void AddArmy(string name, string faction, Army army)
        {
            if (army == null) throw new ArgumentNullException(nameof(army));

            army.Name = name ?? "";
            army.Faction = faction ?? "";
            army.LastModifiedUtc = DateTime.UtcNow;

            SaveArmy(army);
        }

        public void SaveArmy(Army army)
        {
            if (army == null) throw new ArgumentNullException(nameof(army));

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                using (var pragma = connection.CreateCommand())
                {
                    pragma.CommandText = "PRAGMA foreign_keys = ON;";
                    pragma.ExecuteNonQuery();
                }

                using (var transaction = connection.BeginTransaction())
                {
                    var armyId = army.Id.ToString();

                    using (var upsertArmy = connection.CreateCommand())
                    {
                        upsertArmy.Transaction = transaction;
                        upsertArmy.CommandText = @"
                            INSERT INTO Armies (Id, Name, Faction, CreatedAtUtc, LastModifiedUtc)
                            VALUES (@Id, @Name, @Faction, @CreatedAtUtc, @LastModifiedUtc)
                            ON CONFLICT(Id) DO UPDATE SET
                                Name = excluded.Name,
                                Faction = excluded.Faction,
                                LastModifiedUtc = excluded.LastModifiedUtc;
                        ";
                        upsertArmy.Parameters.AddWithValue("@Id", armyId);
                        upsertArmy.Parameters.AddWithValue("@Name", army.Name ?? "");
                        upsertArmy.Parameters.AddWithValue("@Faction", army.Faction ?? "");
                        upsertArmy.Parameters.AddWithValue("@CreatedAtUtc", army.CreatedAtUtc.ToString("o"));
                        upsertArmy.Parameters.AddWithValue("@LastModifiedUtc", army.LastModifiedUtc.ToString("o"));
                        upsertArmy.ExecuteNonQuery();
                    }

                    using (var deleteUnits = connection.CreateCommand())
                    {
                        deleteUnits.Transaction = transaction;
                        deleteUnits.CommandText = "DELETE FROM ArmyUnits WHERE ArmyId = @ArmyId;";
                        deleteUnits.Parameters.AddWithValue("@ArmyId", armyId);
                        deleteUnits.ExecuteNonQuery();
                    }

                    foreach (var unit in army.Units)
                    {
                        if (string.IsNullOrWhiteSpace(unit.Id))
                            unit.Id = Guid.NewGuid().ToString();

                        using (var insertUnit = connection.CreateCommand())
                        {
                            insertUnit.Transaction = transaction;
                            insertUnit.CommandText = @"
                                INSERT INTO ArmyUnits
                                (Id, ArmyId, Name, BattlefieldRole, Keywords, Points, CreatedAtUtc, Notes)
                                VALUES
                                (@Id, @ArmyId, @Name, @BattlefieldRole, @Keywords, @Points, @CreatedAtUtc, @Notes);
                            ";
                            insertUnit.Parameters.AddWithValue("@Id", unit.Id);
                            insertUnit.Parameters.AddWithValue("@ArmyId", armyId);
                            insertUnit.Parameters.AddWithValue("@Name", unit.Name ?? "");
                            insertUnit.Parameters.AddWithValue("@BattlefieldRole", unit.BattlefieldRole ?? "");
                            insertUnit.Parameters.AddWithValue("@Keywords", unit.Keywords ?? "");
                            insertUnit.Parameters.AddWithValue("@Points", unit.Points);
                            insertUnit.Parameters.AddWithValue("@CreatedAtUtc", unit.CreatedAtUtc.ToString("o"));
                            insertUnit.Parameters.AddWithValue("@Notes", unit.Notes ?? "");
                            insertUnit.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }
        }

        public List<Army> GetArmies()
        {
            var armies = new List<Army>();

            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, Name, Faction, CreatedAtUtc, LastModifiedUtc
                        FROM Armies
                        ORDER BY LastModifiedUtc DESC;
                    ";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            armies.Add(new Army
                            {
                                Id = Guid.Parse(reader.GetString(0)),
                                Name = reader.GetString(1),
                                Faction = reader.GetString(2),
                                CreatedAtUtc = DateTime.Parse(reader.GetString(3)),
                                LastModifiedUtc = DateTime.Parse(reader.GetString(4)),
                                Units = new ObservableCollection<Unit>()
                            });
                        }
                    }
                }
            }

            return armies;
        }

        public Army LoadArmy(string id)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                Army army = null;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, Name, Faction, CreatedAtUtc, LastModifiedUtc
                        FROM Armies
                        WHERE Id = @Id;
                    ";
                    command.Parameters.AddWithValue("@Id", id);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            army = new Army
                            {
                                Id = Guid.Parse(reader.GetString(0)),
                                Name = reader.GetString(1),
                                Faction = reader.GetString(2),
                                CreatedAtUtc = DateTime.Parse(reader.GetString(3)),
                                LastModifiedUtc = DateTime.Parse(reader.GetString(4)),
                                Units = new ObservableCollection<Unit>()
                            };
                        }
                    }
                }

                if (army == null)
                    return null;

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT Id, ArmyId, Name, BattlefieldRole, Keywords, Points, CreatedAtUtc, Notes
                        FROM ArmyUnits
                        WHERE ArmyId = @ArmyId
                        ORDER BY CreatedAtUtc ASC;
                    ";
                    command.Parameters.AddWithValue("@ArmyId", id);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            army.Units.Add(new Unit
                            {
                                Id = reader.GetString(0),
                                Name = reader.GetString(2),
                                BattlefieldRole = reader.GetString(3),
                                Keywords = reader.GetString(4),
                                Points = reader.GetInt32(5),
                                CreatedAtUtc = DateTime.Parse(reader.GetString(6)),
                                Notes = reader.GetString(7)
                            });
                        }
                    }
                }

                return army;
            }
        }

        public void DeleteArmy(string id)
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                using (var pragma = connection.CreateCommand())
                {
                    pragma.CommandText = "PRAGMA foreign_keys = ON;";
                    pragma.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Armies WHERE Id = @Id;";
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}