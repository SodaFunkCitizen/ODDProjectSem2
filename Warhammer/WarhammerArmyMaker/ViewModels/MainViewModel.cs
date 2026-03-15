using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using WarhammerArmyMaker.Models;
using WarhammerArmyMaker.Services;

namespace WarhammerArmyMaker.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly BattleScribeImportService _importService;
    private readonly ArmyDatabaseService _databaseService;
    private ImportedUnit? _selectedImportedUnit;
    private ArmyUnit? _selectedArmyUnit;
    private string _catalogueUrl = "https://raw.githubusercontent.com/BSData/wh40k-10e/main/Imperium%20-%20Black%20Templars.cat";
    private string _armyName = "My Black Templars Army";
    private string _statusMessage = "Ready.";

    public MainViewModel()
    {
        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WarhammerArmyMaker");
        var dbPath = Path.Combine(appDataFolder, "WarhammerArmyMaker.db");

        _importService = new BattleScribeImportService();
        _databaseService = new ArmyDatabaseService(dbPath);
        _databaseService.Initialize();

        ImportedUnits = new ObservableCollection<ImportedUnit>();
        CurrentArmyUnits = new ObservableCollection<ArmyUnit>();
        SavedArmies = new ObservableCollection<Army>();

        ImportCatalogueCommand = new AsyncRelayCommand(ImportCatalogueAsync);
        AddSelectedUnitCommand = new RelayCommand(AddSelectedUnit, () => SelectedImportedUnit is not null);
        RemoveSelectedArmyUnitCommand = new RelayCommand(RemoveSelectedArmyUnit, () => SelectedArmyUnit is not null);
        SaveArmyCommand = new RelayCommand(SaveArmy);
        ExportArmyDatabaseCommand = new RelayCommand(ExportArmyDatabase);
        ImportArmyDatabaseCommand = new RelayCommand(ImportArmyDatabase);
        LoadSavedArmyCommand = new RelayCommand<Army>(LoadSavedArmy);

        LoadSavedArmies();
    }

    public ObservableCollection<ImportedUnit> ImportedUnits { get; }
    public ObservableCollection<ArmyUnit> CurrentArmyUnits { get; }
    public ObservableCollection<Army> SavedArmies { get; }

    public string CatalogueUrl
    {
        get => _catalogueUrl;
        set => SetProperty(ref _catalogueUrl, value);
    }

    public string ArmyName
    {
        get => _armyName;
        set => SetProperty(ref _armyName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public ImportedUnit? SelectedImportedUnit
    {
        get => _selectedImportedUnit;
        set
        {
            if (SetProperty(ref _selectedImportedUnit, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public ArmyUnit? SelectedArmyUnit
    {
        get => _selectedArmyUnit;
        set
        {
            if (SetProperty(ref _selectedArmyUnit, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand ImportCatalogueCommand { get; }
    public ICommand AddSelectedUnitCommand { get; }
    public ICommand RemoveSelectedArmyUnitCommand { get; }
    public ICommand SaveArmyCommand { get; }
    public ICommand ExportArmyDatabaseCommand { get; }
    public ICommand ImportArmyDatabaseCommand { get; }
    public ICommand LoadSavedArmyCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task ImportCatalogueAsync()
    {
        try
        {
            StatusMessage = "Importing catalogue...";
            var units = await _importService.ImportFromUrlAsync(CatalogueUrl);

            ImportedUnits.Clear();
            foreach (var unit in units)
            {
                ImportedUnits.Add(unit);
            }

            StatusMessage = $"Imported {ImportedUnits.Count} units from catalogue.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
    }

    private void AddSelectedUnit()
    {
        if (SelectedImportedUnit is null)
        {
            return;
        }

        var existing = CurrentArmyUnits.FirstOrDefault(x => x.UnitName == SelectedImportedUnit.Name);
        if (existing is not null)
        {
            existing.Quantity++;
            RefreshArmyUnits();
            StatusMessage = $"Added another {SelectedImportedUnit.Name}.";
            return;
        }

        CurrentArmyUnits.Add(new ArmyUnit
        {
            UnitName = SelectedImportedUnit.Name,
            Quantity = 1,
            Notes = SelectedImportedUnit.PrimaryCategory,
            StatsJson = JsonSerializer.Serialize(SelectedImportedUnit.Stats),
            CategoriesJson = JsonSerializer.Serialize(SelectedImportedUnit.Categories)
        });

        StatusMessage = $"Added {SelectedImportedUnit.Name} to the army.";
    }

    private void RemoveSelectedArmyUnit()
    {
        if (SelectedArmyUnit is null)
        {
            return;
        }

        CurrentArmyUnits.Remove(SelectedArmyUnit);
        StatusMessage = "Removed unit from the army.";
    }

    private void SaveArmy()
    {
        var army = new Army
        {
            Name = ArmyName,
            Faction = "Black Templars",
            CreatedUtc = DateTime.UtcNow,
            Units = CurrentArmyUnits.Select(x => new ArmyUnit
            {
                UnitName = x.UnitName,
                Quantity = x.Quantity,
                Notes = x.Notes,
                StatsJson = x.StatsJson,
                CategoriesJson = x.CategoriesJson
            }).ToList()
        };

        var id = _databaseService.SaveArmy(army);
        StatusMessage = $"Army saved to local database with id {id}.";
        LoadSavedArmies();
    }

    private void ExportArmyDatabase()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Army Database (*.armydb)|*.armydb|SQLite Database (*.db)|*.db",
            FileName = ArmyName.Replace(' ', '_') + ".armydb"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var army = new Army
        {
            Name = ArmyName,
            Faction = "Black Templars",
            CreatedUtc = DateTime.UtcNow,
            Units = CurrentArmyUnits.Select(x => new ArmyUnit
            {
                UnitName = x.UnitName,
                Quantity = x.Quantity,
                Notes = x.Notes,
                StatsJson = x.StatsJson,
                CategoriesJson = x.CategoriesJson
            }).ToList()
        };

        _databaseService.ExportArmyToDatabase(army, dialog.FileName);
        StatusMessage = $"Army exported to {dialog.FileName}.";
    }

    private void ImportArmyDatabase()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Army Database (*.armydb;*.db)|*.armydb;*.db|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var army = _databaseService.ImportArmyFromDatabase(dialog.FileName);
        LoadArmy(army);
        StatusMessage = $"Imported army database from {dialog.FileName}.";
    }

    private void LoadSavedArmy(Army? army)
    {
        if (army is null)
        {
            return;
        }

        var loaded = _databaseService.GetArmy(army.Id);
        if (loaded is not null)
        {
            LoadArmy(loaded);
            StatusMessage = $"Loaded saved army '{loaded.Name}'.";
        }
    }

    private void LoadArmy(Army army)
    {
        ArmyName = army.Name;
        CurrentArmyUnits.Clear();
        foreach (var unit in army.Units)
        {
            CurrentArmyUnits.Add(unit);
        }
    }

    private void LoadSavedArmies()
    {
        SavedArmies.Clear();
        foreach (var army in _databaseService.GetArmies())
        {
            SavedArmies.Add(army);
        }
    }

    private void RefreshArmyUnits()
    {
        var snapshot = CurrentArmyUnits.ToList();
        CurrentArmyUnits.Clear();
        foreach (var item in snapshot)
        {
            CurrentArmyUnits.Add(item);
        }
    }

    private void RaiseCanExecuteChanged()
    {
        if (AddSelectedUnitCommand is RelayCommand add)
        {
            add.RaiseCanExecuteChanged();
        }

        if (RemoveSelectedArmyUnitCommand is RelayCommand remove)
        {
            remove.RaiseCanExecuteChanged();
        }
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class RelayCommand<T> : ICommand
{
    private readonly Action<T?> _execute;

    public RelayCommand(Action<T?> execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute((T?)parameter);
}

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private bool _isRunning;

    public AsyncRelayCommand(Func<Task> execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isRunning;

    public async void Execute(object? parameter)
    {
        if (_isRunning)
        {
            return;
        }

        try
        {
            _isRunning = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            await _execute();
        }
        finally
        {
            _isRunning = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
