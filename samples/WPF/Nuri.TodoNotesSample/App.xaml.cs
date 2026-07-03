using System.Windows;
using Nuri.TodoNotesSample.Components;
using Nuri.WPF;

namespace Nuri.TodoNotesSample;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        NuriApplication.Run<TodoNotesComponent>("Nuri Todo Notes", width: 1040, height: 760);
    }
}
