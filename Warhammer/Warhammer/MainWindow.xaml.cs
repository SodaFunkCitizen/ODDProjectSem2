using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace ArmyBuilder
{
    public partial class MainWindow : Window
    {
        private ArmyViewModel ViewModel;

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new ArmyViewModel();
            DataContext = ViewModel;
        }

        private void AddUnit_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddUnit();
        }

        private void Validate_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ValidateArmy();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ExportArmy();
        }
    }
}
