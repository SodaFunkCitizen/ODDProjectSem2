using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Warhammer;

public class ArmyViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Unit> Units { get; set; }

    private Unit _selectedUnit;
    public Unit SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            _selectedUnit = value;
            OnPropertyChanged(nameof(SelectedUnit));
        }
    }

    public int TotalPoints => Units.Sum(u => u.Points);

    // Commands
    public RelayCommand AddUnitCommand { get; }
    public RelayCommand RemoveUnitCommand { get; }
    public RelayCommand ValidateCommand { get; }
    public RelayCommand ExportCommand { get; }

    public ArmyViewModel()
    {
        Units = new ObservableCollection<Unit>();
        Units.CollectionChanged += (s, e) => OnPropertyChanged(nameof(TotalPoints));

        AddUnitCommand = new RelayCommand(_ => AddUnit());
        RemoveUnitCommand = new RelayCommand(_ => RemoveUnit(), _ => SelectedUnit != null);
        ValidateCommand = new RelayCommand(_ => ValidateArmy());
        ExportCommand = new RelayCommand(_ => ExportArmy());

        LoadSampleData();
    }

    private void AddUnit()
    {
        var newUnit = new Unit
        {
            Name = "Intercessor Squad",
            BattlefieldRole = "Battleline",
            Keywords = "Infantry, Tacticus, Core",
            Points = 95,
            Wargear = new List<string>() { "Bolt Rifles", "Frag Grenades", "Krak Grenades" },
            Abilities = new List<string>() { "Oath of Moment", "Objective Secured" }
        };

        Units.Add(newUnit);
        SelectedUnit = newUnit;
        OnPropertyChanged(nameof(TotalPoints));
    }

    private void RemoveUnit()
    {
        if (SelectedUnit != null)
        {
            Units.Remove(SelectedUnit);
            SelectedUnit = null;
            OnPropertyChanged(nameof(TotalPoints));
        }
    }

    private void ValidateArmy()
    {
        if (TotalPoints > 2000)
        {
            MessageBox.Show("Army exceeds 2000 points!", "Validation");
        }
        else
        {
            MessageBox.Show("Army is valid!", "Validation");
        }
    }

    private void ExportArmy()
    {
        string export = "Army List:\n\n";

        foreach (var unit in Units)
        {
            export += $"{unit.Name} - {unit.Points} pts\n";
        }

        export += $"\nTotal: {TotalPoints} pts";

        MessageBox.Show(export, "Export Preview");
    }

    private void LoadSampleData()
    {
        Units.Add(new Unit
        {
            Name = "Captain in Gravis Armour",
            BattlefieldRole = "Character",
            Keywords = "Character, Gravis, Leader",
            Points = 105,
            Wargear = new List<string>() { "Master-crafted Bolt Rifle", "Power Sword" },
            Abilities = new List<string>() { "Rites of Battle", "Iron Resolve" }
        });

        Units.Add(new Unit
        {
            Name = "Redemptor Dreadnought",
            BattlefieldRole = "Vehicle",
            Keywords = "Vehicle, Dreadnought",
            Points = 210,
            Wargear = new List<string>() { "Macro Plasma Incinerator", "Onslaught Gatling Cannon" },
            Abilities = new List<string>() { "Duty Eternal" }
        });

        OnPropertyChanged(nameof(TotalPoints));
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}