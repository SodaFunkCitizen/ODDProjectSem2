using System.Windows;
using WarhammerArmyMaker.ViewModels;

namespace WarhammerArmyMaker;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}