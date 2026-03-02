using System.ComponentModel;
public class Unit : INotifyPropertyChanged
{
    public string Name { get; set; }
    public string BattlefieldRole { get; set; }
    public string Keywords { get; set; }
    public int Points { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
}