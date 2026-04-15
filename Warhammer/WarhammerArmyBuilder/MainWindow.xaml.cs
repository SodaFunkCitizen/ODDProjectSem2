using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using WarhammerArmyBuilder;
using WarhammerArmyBuilder.ViewModels;

namespace WarhammerArmyBuilder
{
    public partial class MainWindow : Window
    {
        private ArmyViewModel VM => (ArmyViewModel)DataContext;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "XAML load failed", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            try
            {
                DataContext = new ArmyViewModel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ViewModel initialization failed", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void AddUnit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new AddUnitWindow(VM) { Owner = this };
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Add Unit Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveUnit_Click(object sender, RoutedEventArgs e)
        {
            VM.RemoveSelectedUnit();
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            var result = VM.ValidateArmy();
            MessageBox.Show(result, "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "JSON (*.json)|*.json",
                    FileName = $"{VM.Army.Name}.json"
                };

                if (sfd.ShowDialog(this) != true) return;
                VM.ExportArmyToJson(sfd.FileName);
                MessageBox.Show("Exported.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new OpenFileDialog { Filter = "JSON (*.json)|*.json" };
                if (ofd.ShowDialog(this) != true) return;

                VM.ImportArmyFromJson(ofd.FileName);
                MessageBox.Show("Imported.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                VM.SaveArmyToDatabase();
                MessageBox.Show("Saved to database.", "Database", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshSaved_Click(object sender, RoutedEventArgs e)
        {
            VM.RefreshSavedArmies();
        }

        private void LoadDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SavedArmiesList.SelectedItem is not Army selected)
                {
                    MessageBox.Show("Select an army first.", "Database", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                VM.LoadArmyFromDatabase(selected.Id);
                MessageBox.Show("Loaded.", "Database", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteDb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SavedArmiesList.SelectedItem is not Army selected)
                {
                    MessageBox.Show("Select an army first.", "Database", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show($"Delete '{selected.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                    return;

                VM.DeleteArmyFromDatabase(selected.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadCatalogueUrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = CatalogueSourceText.Text?.Trim();
                if (string.IsNullOrWhiteSpace(url))
                {
                    MessageBox.Show("Paste a raw .cat URL first, or use 'Load from File'.", "Catalogue", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await VM.LoadCatalogueFromUrlAsync(url);
                MessageBox.Show("Catalogue loaded.", "Catalogue", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Catalogue Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCatalogueFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new OpenFileDialog { Filter = "BattleScribe Catalogue (*.cat)|*.cat|XML (*.xml)|*.xml|All files (*.*)|*.*" };
                if (ofd.ShowDialog(this) != true) return;

                VM.LoadCatalogueFromFile(ofd.FileName);
                MessageBox.Show("Catalogue loaded.", "Catalogue", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Catalogue Load Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

}
