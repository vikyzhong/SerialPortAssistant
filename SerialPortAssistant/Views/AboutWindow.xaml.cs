using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using SerialPortAssistant.Helpers;

namespace SerialPortAssistant.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = AppVersion.AboutLine();
    }

    private void EmailLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
