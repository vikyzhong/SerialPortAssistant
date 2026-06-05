using System.Windows;
using SerialPortAssistant.Helpers;

namespace SerialPortAssistant;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ScottPlotFontConfigurator.Initialize();
        base.OnStartup(e);
    }
}
