using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WarhammerArmyBuilder.ViewModels;
/*
So basically this is the rough draft of the back end of the website.
So there is the rough things i need this to do which is to add units from template and 
I'm writing these notes just to show that this is my though process and not AI slop
I am sorry about that fuck up before. 
Now i'm gonna start on the unit SQL back end 

I won't deny alot of the next few builds are not really gonna work 
*/
namespace WarhammerArmyBuilder
{

    public partial class AddUnitWindow : Window
    {
        private ArmyViewModel VM { get; }

        public AddUnitWindow(ArmyViewModel vm)
        {
            InitializeComponent();
            VM = vm;
            DataContext = vm;
            CustomRoleCombo.SelectedIndex = 1; 
        }

        private void AddFromTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (VM.SelectedTemplate is null)
                {
                    MessageBox.Show("Select a template first.");
                    return;
                }

                if (!int.TryParse(TemplatePointsText.Text, out var points) || points < 0)
                {
                    MessageBox.Show("Enter a valid non-negative points value.");
                    return;
                }

                VM.AddUnitFromTemplate(VM.SelectedTemplate, points, TemplateNotesText.Text ?? "");
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Add Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddCustom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(CustomPointsText.Text, out var points) || points < 0)
                {
                    MessageBox.Show("Enter a valid non-negative points value.");
                    return;
                }

                var role = (CustomRoleCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Unit";
                VM.AddCustomUnit(CustomNameText.Text ?? "", role, CustomKeywordsText.Text ?? "", points, CustomNotesText.Text ?? "");
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Add Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

}
