using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace ArmyBuilder
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ArmyViewModel();
        }
    }
}

public class Unit : INotifyPropertyChanged
{
    private string _name;
    private string _battlefieldRole;
    private string _keywords;
    private int _points;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(nameof(Name)); }
    }

    public string BattlefieldRole
    {
        get => _battlefieldRole;
        set { _battlefieldRole = value; OnPropertyChanged(nameof(BattlefieldRole)); }
    }

    public string Keywords
    {
        get => _keywords;
        set { _keywords = value; OnPropertyChanged(nameof(Keywords)); }
    }

    public int Points
    {
        get => _points;
        set { _points = value; OnPropertyChanged(nameof(Points)); }
    }

    public List<string> Wargear { get; set; } = new List<string>();
    public List<string> Abilities { get; set; } = new List<string>();

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

