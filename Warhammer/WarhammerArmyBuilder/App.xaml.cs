using SQLitePCL;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace WarhammerArmyBuilder
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Batteries_V2.Init();
            base.OnStartup(e);
        }
    }
}
