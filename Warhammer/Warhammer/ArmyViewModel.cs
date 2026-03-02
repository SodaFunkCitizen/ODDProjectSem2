using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

public class ArmyViewModel : INotifyPropertyChanged
{
    public ObservableCollection<Unit> Units { get; set; }
    
    private Unit _selectedUnit;
    public Unit SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (_selectedUnit != value)
            {
                _selectedUnit = value;
                OnPropertyChanged(nameof(SelectedUnit));
            }
        }
    }
    public int TotalPoints => Units.Sum(u => u.Points);

    public ArmyViewModel()
    {
        Units = new ObservableCollection<Unit>();
        Units.CollectionChanged += (s, e) =>
            OnPropertyChanged(nameof(TotalPoints));

        LoadSampleData();
    }




    public void AddUnit()
    {
        var unit = new Unit
        {
            Name = "Intercessor Squad",
            BattlefieldRole = "Battleline",
            Keywords = "Infantry, Tacticus, Core",
            Points = 95
        };

        Units.Add(unit);
        SelectedUnit = unit;

        OnPropertyChanged(nameof(SelectedUnit));
        OnPropertyChanged(nameof(TotalPoints));
    }

    public void ValidateArmy()
    {
        if (TotalPoints > 2000)
            MessageBox.Show("Army exceeds 2000 points!");
        else
            MessageBox.Show("Army is valid!");
    }

    public void ExportArmy()
    {
        string text = "Army List\n\n";

        foreach (var unit in Units)
            text += $"{unit.Name} - {unit.Points} pts\n";

        text += $"\nTotal: {TotalPoints} pts";

        MessageBox.Show(text);
    }

    private void LoadSampleData()
    {
        Units.Add(new Unit
        {
            Name = "Captain in Gravis Armour",
            BattlefieldRole = "Character",
            Keywords = "Character, Gravis, Leader",
            Points = 105
        });

        Units.Add(new Unit
        {
            Name = "Redemptor Dreadnought",
            BattlefieldRole = "Vehicle",
            Keywords = "Vehicle, Dreadnought",
            Points = 210
        });
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this,
            new PropertyChangedEventArgs(name));
}