using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using WarhammerArmyBuilder;

namespace WarhammerArmyBuilder.Services
{
    public class ArmyDbService
    {
        private readonly string _dbPath;

        List<Army> _armies = new List<Army>();

        public ArmyDbService(string dbPath)
        {
            _dbPath = dbPath;
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));
        }

        private string ConnectionString => $"Data Source={_dbPath};Version=3;";

        public void Initialize()
        {
            var connection = new SqliteConnection(ConnectionString);
            {
                connection.Open();
                var command = connection.CreateCommand();
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

        public void AddArmy(string name, string faction, Army army)
        {
            var connection = new SqliteConnection(ConnectionString);
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Armies (Id, Name, Faction, CreatedAtUtc, LastModifiedUtc)
                    VALUES (@Id, @Name, @Faction, @CreatedAtUtc, @LastModifiedUtc);
                ";
                command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@Faction", faction);
                command.Parameters.AddWithValue("@CreatedAtUtc", DateTime.UtcNow.ToString("o"));
                command.Parameters.AddWithValue("@LastModifiedUtc", DateTime.UtcNow.ToString("o"));
                command.ExecuteNonQuery();
            }
            var del = connection.CreateCommand();
            {
                del.CommandText = @"
                    DELETE FROM Armies WHERE Id = @Id;
                ";
                del.Parameters.AddWithValue("@Id", "some-id");
                del.ExecuteNonQuery();

            }

            foreach (var armies in GetArmies())
            {
                var ins = connection.CreateCommand();
                {
                    ins.CommandText = @"
                        INSERT INTO Armies (Id, Name, Faction, CreatedAtUtc, LastModifiedUtc)
                        VALUES (@Id, @Name, @Faction, @CreatedAtUtc, @LastModifiedUtc);
                    ";
                    ins.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                    ins.Parameters.AddWithValue("@Name", army.Name);
                    ins.Parameters.AddWithValue("@Faction", army.Faction);
                    ins.Parameters.AddWithValue("@CreatedAtUtc", DateTime.UtcNow.ToString("o"));
                    ins.Parameters.AddWithValue("@LastModifiedUtc", DateTime.UtcNow.ToString("o"));
                    ins.ExecuteNonQuery();
                }
            }
        }

        public List<Army> GetArmies()
        {
            var armies = new List<Army>();
            var connection = new SqliteConnection(ConnectionString);
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT Id, Name, Faction, CreatedAtUtc, LastModifiedUtc
                    FROM Armies;
                ";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        armies.Add(new Army
                        { 
                            Name = reader.GetString(1),
                            Faction = reader.GetString(2),
                            CreatedAtUtc = DateTime.Parse(reader.GetString(3)),
                            LastModifiedUtc = DateTime.Parse(reader.GetString(4))
                        });
                    }
                }
            }
            return armies;
        }
        public Army LoadArmy(string id)
        {
            Army army = null;
            var connection = new SqliteConnection(ConnectionString);
            {

                connection.Open();
                var command1 = connection.CreateCommand();
                command1.CommandText = @"
                    SELECT Id, Name, Faction, CreatedAtUtc, LastModifiedUtc
                    FROM Armies
                    WHERE Id = @Id;
                ";
                command1.Parameters.AddWithValue("@Id", id);
                using (var reader = command1.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Army
                        {
                            Name = reader.GetString(1),
                            Faction = reader.GetString(2),
                            CreatedAtUtc = DateTime.Parse(reader.GetString(3)),
                            LastModifiedUtc = DateTime.Parse(reader.GetString(4))
                        };
                    }
                }
            }
            if (army is null) return null;

            var command = connection.CreateCommand();
            {
                command.CommandText = @"
                    SELECT Id, ArmyId, Name, BattlefieldRole, Keywords, Points, CreatedAtUtc, Notes
                    FROM ArmyUnits
                    WHERE ArmyId = @ArmyId;
                ";
                command.Parameters.AddWithValue("@ArmyId", army.Id);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        army.Units.Add(new Unit
                        {
                            Id = reader.GetString(0),
                            Name = reader.GetString(2),
                            BattlefieldRole = reader.GetString(3),
                            Keywords = reader.GetString(3),
                            Points = reader.GetInt32(5),
                            CreatedAtUtc = DateTime.Parse(reader.GetString(6)),
                            Notes = reader.GetString(7)
                        });
                    }

                    foreach (var unit in army.Units)
                    {
                        var del = connection.CreateCommand();
                        {
                            del.CommandText = @"
                                DELETE FROM ArmyUnits WHERE Id = @Id;
                            ";
                            del.Parameters.AddWithValue("@Id", unit.Id);
                            del.ExecuteNonQuery();
                        }
                    }

                    foreach (var unit in army.Units)
                    {
                        var ins = connection.CreateCommand();
                        {
                            ins.CommandText = @"
                                INSERT INTO ArmyUnits (Id, ArmyId, Name, BattlefieldRole, Keywords, Points, CreatedAtUtc, Notes)
                                VALUES (@Id, @ArmyId, @Name, @BattlefieldRole, @Keywords, @Points, @CreatedAtUtc, @Notes);
                            ";
                            ins.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                            ins.Parameters.AddWithValue("@ArmyId", army.Id);
                            ins.Parameters.AddWithValue("@Name", unit.Name);
                            ins.Parameters.AddWithValue("@BattlefieldRole", unit.BattlefieldRole);
                            ins.Parameters.AddWithValue("@Keywords", string.Join(",", unit.Keywords));
                            ins.Parameters.AddWithValue("@Points", unit.Points);
                            ins.Parameters.AddWithValue("@CreatedAtUtc", DateTime.UtcNow.ToString("o"));
                            ins.Parameters.AddWithValue("@Notes", unit.Notes);
                            ins.ExecuteNonQuery();
                        }
                    }

                    return army;
                }
            }
        }

        public void DeleteArmy(string id)
        {
            var connection = new SqliteConnection(ConnectionString);
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                    DELETE FROM Armies WHERE Id = @Id;
                ";
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }
    }
}