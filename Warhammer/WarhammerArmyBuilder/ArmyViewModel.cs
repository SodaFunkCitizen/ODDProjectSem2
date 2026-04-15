using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using WarhammerArmyBuilder.Services;
using WarhammerArmyBuilder.Util;
using WarhammerArmyBuilder;

namespace WarhammerArmyBuilder.ViewModels
{
    public class ArmyViewModel
    {
        public abstract class ObservableObject : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public void OnPropertyChanged([CallerMemberName] string propertyName = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                    return false;

                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly Catalogue _catalogueService = new();
        private readonly JsonArmyService _jsonService = new();
        private readonly ArmyDbService _db;

        private Army _army = new();
        private Unit _selectedUnit;

        private string _unitSearch = "";
        private string _unitRoleFilter = "All";
        private string _unitSort = "Points (desc)";

        private UnitTemplate _selectedTemplate;

        public ArmyViewModel()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArmyBuilderBasic");
            _db = new ArmyDbService(Path.Combine(appData, "armies.sqlite"));
            _db.Initialize();

            CatalogueUnits = _catalogueService.LoadEmbeddedSample();

            UnitRoles = new ObservableCollection<string>(new[] { "All", "Character", "Infantry", "Vehicle", "Unit" });
            UnitSortOptions = new ObservableCollection<string>(new[] { "Points (desc)", "Points (asc)", "Name (A-Z)", "Role (A-Z)", "Created (newest)" });

            if (!(_army.Units is ObservableCollection<Unit>))
            {
                _army.Units = new ObservableCollection<Unit>(_army.Units);
            }

            UnitsView = CollectionViewSource.GetDefaultView(Army.Units);
            UnitsView.Filter = o => o is Unit u && PassesUnitFilter(u);
            ApplySort();
            UnitsView.Refresh();
            LoadSampleArmy();
            RefreshSavedArmies();

            if (Army.Units is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += (_, __) =>
                {
                    Army.LastModifiedUtc = DateTime.UtcNow;
                    OnPropertyChanged(nameof(TotalPoints));
                    UnitsView.Refresh();
                };
            }
        }

        public Army Army
        {
            get => _army;
            private set
            {
                if (_army != value)
                {
                    // Unsubscribe previous collection change if necessary
                    if (_army?.Units is INotifyCollectionChanged oldNotify)
                        oldNotify.CollectionChanged -= ArmyUnits_CollectionChanged;

                    _army = value ?? new Army();

                    // Convert Units to ObservableCollection<Unit> if needed
                    if (!(_army.Units is ObservableCollection<Unit>))
                    {
                        _army.Units = new ObservableCollection<Unit>(_army.Units);
                    }

                    // Subscribe new army units changes so totals / view update automatically
                    if (_army.Units is INotifyCollectionChanged newNotify)
                    {
                        newNotify.CollectionChanged += ArmyUnits_CollectionChanged;
                    }

                    OnPropertyChanged(nameof(TotalPoints));
                }
            }
        }

        private void ArmyUnits_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Army.LastModifiedUtc = DateTime.UtcNow;
            OnPropertyChanged(nameof(TotalPoints));
            UnitsView.Refresh();
        }

        public int TotalPoints => Army?.Units?.Sum(u => u.Points) ?? 0;

        public Unit SelectedUnit
        {
            get => _selectedUnit;
            set => _selectedUnit = value;
        }

        public ObservableCollection<UnitTemplate> CatalogueUnits { get; private set; } = new();

        public UnitTemplate SelectedTemplate
        {
            get => _selectedTemplate;
            set => _selectedTemplate = value;
        }

        public ICollectionView UnitsView { get; private set; }

        public ObservableCollection<string> UnitRoles { get; }
        public ObservableCollection<string> UnitSortOptions { get; }

        public string UnitSearch
        {
            get => _unitSearch;
            set
            {
                if (_unitSearch != value)
                {
                    _unitSearch = value;
                    UnitsView.Refresh();
                }
            }
        }

        public string UnitRoleFilter
        {
            get => _unitRoleFilter;
            set
            {
                if (_unitRoleFilter != value)
                {
                    _unitRoleFilter = value;
                    UnitsView.Refresh();
                }
            }
        }

        public string UnitSort
        {
            get => _unitSort;
            set
            {
                if (_unitSort != value)
                {
                    _unitSort = value;
                    ApplySort();
                }
            }
        }

        public ObservableCollection<Army> SavedArmies { get; } = new();

        public void AddUnitFromTemplate(UnitTemplate template, int points, string notes)
        {
            if (template is null) throw new ArgumentNullException(nameof(template));
            Guard.NonNegative(points, nameof(points));

            var unit = new Unit
            {
                Name = template.Name,
                BattlefieldRole = template.BattlefieldRole,
                Keywords = template.Keywords,
                Points = points,
                Notes = notes ?? "",
                CreatedAtUtc = DateTime.UtcNow
            };

            Army.Units.Add(unit);
            SelectedUnit = unit;
            OnPropertyChanged(nameof(TotalPoints));
        }

        public void AddCustomUnit(string name, string role, string keywords, int points, string notes)
        {
            name = Guard.NotNullOrWhiteSpace(name, nameof(name));
            role = Guard.NotNullOrWhiteSpace(role, nameof(role));
            Guard.NonNegative(points, nameof(points));

            var unit = new Unit
            {
                Name = name,
                BattlefieldRole = role,
                Keywords = keywords ?? "",
                Points = points,
                Notes = notes ?? "",
                CreatedAtUtc = DateTime.UtcNow
            };

            Army.Units.Add(unit);
            SelectedUnit = unit;
            OnPropertyChanged(nameof(TotalPoints));
        }

        public void RemoveSelectedUnit()
        {
            if (SelectedUnit is null) return;
            Army.Units.Remove(SelectedUnit);
            SelectedUnit = null;
            OnPropertyChanged(nameof(TotalPoints));
        }

        public string ValidateArmy(int maxPoints = 2000)
        {
            if (TotalPoints > maxPoints)
                return $"Army exceeds {maxPoints} points (currently {TotalPoints}).";

            if (Army.Units.Count == 0)
                return "Army has no units.";

            return "Army looks valid (basic validation).";
        }

        public void ExportArmyToJson(string path)
        {
            Army.LastModifiedUtc = DateTime.UtcNow;
            _jsonService.SaveToFile(Army, path);
        }

        public void ImportArmyFromJson(string path)
        {
            var loaded = _jsonService.LoadFromFile(path);

            Army = loaded ?? new Army();
            if (!(Army.Units is ObservableCollection<Unit>))
            {
                Army.Units = new ObservableCollection<Unit>(Army.Units);
            }
            UnitsView = CollectionViewSource.GetDefaultView(Army.Units);
            UnitsView.Filter = o => o is Unit u && PassesUnitFilter(u);
            ApplySort();
            UnitsView.Refresh();
            SelectedUnit = Army.Units.FirstOrDefault();
            OnPropertyChanged(nameof(TotalPoints));

            Army.Units.CollectionChanged += (_, __) =>
            {
                Army.LastModifiedUtc = DateTime.UtcNow;
                OnPropertyChanged(nameof(TotalPoints));
                UnitsView.Refresh();
            };
        }

        public void SaveArmyToDatabase()
        {
            Army.LastModifiedUtc = DateTime.UtcNow;
            _db.AddArmy(Army.Name, Army.Faction, Army);
            RefreshSavedArmies();
        }

        public void LoadArmyFromDatabase(Guid id)
        {
            var loaded = _db.LoadArmy(id.ToString());
            if (loaded is null) throw new InvalidOperationException("Army not found.");

            Army = loaded;
            if (!(Army.Units is ObservableCollection<Unit>))
            {
                Army.Units = new ObservableCollection<Unit>(Army.Units);
            }
            UnitsView = CollectionViewSource.GetDefaultView(Army.Units);
            UnitsView.Filter = o => o is Unit u && PassesUnitFilter(u);
            ApplySort();
            UnitsView.Refresh();
            SelectedUnit = Army.Units.FirstOrDefault();
            OnPropertyChanged(nameof(TotalPoints));

            if (Army.Units is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged += (_, __) =>
                {
                    Army.LastModifiedUtc = DateTime.UtcNow;
                    OnPropertyChanged(nameof(TotalPoints));
                    UnitsView.Refresh();
                };
            }
        }

        public void DeleteArmyFromDatabase(Guid id)
        {
            _db.DeleteArmy(id.ToString());
            RefreshSavedArmies();
        }

        public void RefreshSavedArmies()
        {
            SavedArmies.Clear();
            foreach (var a in _db.GetArmies())
                SavedArmies.Add(a);
        }

        public async Task LoadCatalogueFromUrlAsync(string url)
        {
            var loaded = await _catalogueService.LoadFromUrlAsync(url);
            CatalogueUnits = loaded;
            OnPropertyChanged(nameof(CatalogueUnits));
            SelectedTemplate = CatalogueUnits.FirstOrDefault();
        }

        public void LoadCatalogueFromFile(string path)
        {
            var loaded = _catalogueService.LoadFromFile(path);
            CatalogueUnits = loaded;
            OnPropertyChanged(nameof(CatalogueUnits));
            SelectedTemplate = CatalogueUnits.FirstOrDefault();
        }

        private bool PassesUnitFilter(Unit u)
        {
            if (!u.Matches(UnitSearch))
                return false;

            if (!string.Equals(UnitRoleFilter, "All", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(u.BattlefieldRole, UnitRoleFilter, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private void ApplySort()
        {
            UnitsView.SortDescriptions.Clear();

            switch (UnitSort)
            {
                case "Points (asc)":
                    UnitsView.SortDescriptions.Add(new SortDescription(nameof(Unit.Points), ListSortDirection.Ascending));
                    break;
                case "Name (A-Z)":
                    UnitsView.SortDescriptions.Add(new SortDescription(nameof(Unit.Name), ListSortDirection.Ascending));
                    break;
                case "Role (A-Z)":
                    UnitsView.SortDescriptions.Add(new SortDescription(nameof(Unit.BattlefieldRole), ListSortDirection.Ascending));
                    break;
                case "Created (newest)":
                    UnitsView.SortDescriptions.Add(new SortDescription(nameof(Unit.CreatedAtUtc), ListSortDirection.Descending));
                    break;
                default:
                    UnitsView.SortDescriptions.Add(new SortDescription(nameof(Unit.Points), ListSortDirection.Descending));
                    break;
            }

            UnitsView.Refresh();
        }

        private void LoadSampleArmy()
        {
            Army.Name = "Black Templars - Test Crusade";
            Army.Faction = "Black Templars";
            Army.Units.Add(new Unit
            {
                Name = "Captain in Gravis Armour",
                BattlefieldRole = "Character",
                Keywords = "Character, Gravis, Leader",
                Points = 105,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
                Notes = "Sample"
            });
            Army.Units.Add(new Unit
            {
                Name = "Redemptor Dreadnought",
                BattlefieldRole = "Vehicle",
                Keywords = "Vehicle, Dreadnought",
                Points = 210,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                Notes = "Sample"
            });
            SelectedUnit = Army.Units.FirstOrDefault();
            OnPropertyChanged(nameof(TotalPoints));
        }
    }
}
