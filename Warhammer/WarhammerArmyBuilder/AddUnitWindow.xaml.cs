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

        public AddUnitWindow()
        {
           InitializeComponent();
          
        }

        private void AddFromTemplate_Click()
        {
           
        }

        private void AddCustom_Click()
        {

        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
