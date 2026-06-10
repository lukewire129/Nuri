using DeltaUI.WPF;
using DeltaUISample.Components;
using System.Windows;

namespace DeltaUISample
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            Title = "MVU Application";
            Width = 400;
            Height = 300;

            ApplicationRoot.Initialize (new CounterComponent (), this);
        }
    }
}