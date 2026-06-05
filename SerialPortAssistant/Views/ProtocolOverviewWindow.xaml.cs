using System.Windows;

namespace SerialPortAssistant.Views;

public partial class ProtocolOverviewWindow : Window
{
    public ProtocolOverviewWindow()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
