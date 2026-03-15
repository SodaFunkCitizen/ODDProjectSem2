using System.Text.Json;
using System.IO;
using Microsoft.Data.Sqlite;
using WarhammerArmyMaker.Models;

namespace WarhammerArmyMaker.Services;

public sealed class ArmyDatabaseService
{
    private readonly string _dbPath;

    public ArmyDatabaseService(string dbPath)
    {
        _dbPath = dbPath;
    }

    private string ConnectionString => new SqliteConnectionStringBuilder { DataSource = _dbPath }.ToString();

    public void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS Armies (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Faction TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS ArmyUnits (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ArmyId INTEGER NOT NULL,
    UnitName TEXT NOT NULL,
    Quantity INTEGER NOT NULL,
    Notes TEXT NOT NULL,
    StatsJson TEXT NOT NULL,
    CategoriesJson TEXT NOT NULL,
    FOREIGN KEY (ArmyId) REFERENCES Armies(Id)
);";
        command.ExecuteNonQuery();
    }

    public int SaveArmy(Army army)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        int armyId;
        if (army.Id <= 0)
        {
            var insertArmy = connection.CreateCommand();
            insertArmy.Transaction = transaction;
            insertArmy.CommandText = @"INSERT INTO Armies (Name, Faction, CreatedUtc) VALUES ($name, $faction, $createdUtc); SELECT last_insert_rowid();";
            insertArmy.Parameters.AddWithValue("$name", army.Name);
            insertArmy.Parameters.AddWithValue("$faction", army.Faction);
            insertArmy.Parameters.AddWithValue("$createdUtc", army.CreatedUtc.ToString("O"));
            armyId = Convert.ToInt32((long)insertArmy.ExecuteScalar()!);
        }
        else
        {
            armyId = army.Id;
            var updateArmy = connection.CreateCommand();
            updateArmy.Transaction = transaction;
            updateArmy.CommandText = "UPDATE Armies SET Name = $name, Faction = $faction WHERE Id = $id";
            updateArmy.Parameters.AddWithValue("$id", armyId);
            updateArmy.Parameters.AddWithValue("$name", army.Name);
            updateArmy.Parameters.AddWithValue("$faction", army.Faction);
            updateArmy.ExecuteNonQuery();

            var deleteUnits = connection.CreateCommand();
            deleteUnits.Transaction = transaction;
            deleteUnits.CommandText = "DELETE FROM ArmyUnits WHERE ArmyId = $armyId";
            deleteUnits.Parameters.AddWithValue("$armyId", armyId);
            deleteUnits.ExecuteNonQuery();
        }

        foreach (var unit in army.Units)
        {
            var insertUnit = connection.CreateCommand();
            insertUnit.Transaction = transaction;
            insertUnit.CommandText = @"INSERT INTO ArmyUnits (ArmyId, UnitName, Quantity, Notes, StatsJson, CategoriesJson)
                                       VALUES ($armyId, $unitName, $quantity, $notes, $statsJson, $categoriesJson)";
            insertUnit.Parameters.AddWithValue("$armyId", armyId);
            insertUnit.Parameters.AddWithValue("$unitName", unit.UnitName);
            insertUnit.Parameters.AddWithValue("$quantity", unit.Quantity);
            insertUnit.Parameters.AddWithValue("$notes", unit.Notes);
            insertUnit.Parameters.AddWithValue("$statsJson", unit.StatsJson);
            insertUnit.Parameters.AddWithValue("$categoriesJson", unit.CategoriesJson);
            insertUnit.ExecuteNonQuery();
        }

        transaction.Commit();
        return armyId;
    }

    public List<Army> GetArmies()
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var armies = new List<Army>();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Faction, CreatedUtc FROM Armies ORDER BY Id DESC";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            armies.Add(new Army
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Faction = reader.GetString(2),
                CreatedUtc = DateTime.Parse(reader.GetString(3)),
            });
        }

        foreach (var army in armies)
        {
            army.Units = GetUnits(army.Id, connection);
        }

        return armies;
    }

    public Army? GetArmy(int armyId)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Faction, CreatedUtc FROM Armies WHERE Id = $id";
        command.Parameters.AddWithValue("$id", armyId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new Army
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Faction = reader.GetString(2),
            CreatedUtc = DateTime.Parse(reader.GetString(3)),
            Units = GetUnits(armyId, connection)
        };
    }

    public void ExportArmyToDatabase(Army army, string exportPath)
    {
        if (File.Exists(exportPath))
        {
            File.Delete(exportPath);
        }

        var exportService = new ArmyDatabaseService(exportPath);
        exportService.Initialize();
        exportService.SaveArmy(new Army
        {
            Name = army.Name,
            Faction = army.Faction,
            CreatedUtc = army.CreatedUtc,
            Units = army.Units.Select(x => new ArmyUnit
            {
                UnitName = x.UnitName,
                Quantity = x.Quantity,
                Notes = x.Notes,
                StatsJson = x.StatsJson,
                CategoriesJson = x.CategoriesJson,
            }).ToList()
        });
    }

    public Army ImportArmyFromDatabase(string importPath)
    {
        var importService = new ArmyDatabaseService(importPath);
        importService.Initialize();
        return importService.GetArmies().FirstOrDefault() ?? new Army();
    }

    private static List<ArmyUnit> GetUnits(int armyId, SqliteConnection connection)
    {
        var units = new List<ArmyUnit>();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, UnitName, Quantity, Notes, StatsJson, CategoriesJson FROM ArmyUnits WHERE ArmyId = $armyId ORDER BY Id";
        command.Parameters.AddWithValue("$armyId", armyId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            units.Add(new ArmyUnit
            {
                Id = reader.GetInt32(0),
                UnitName = reader.GetString(1),
                Quantity = reader.GetInt32(2),
                Notes = reader.GetString(3),
                StatsJson = reader.GetString(4),
                CategoriesJson = reader.GetString(5)
            });
        }

        return units;
    }
}
