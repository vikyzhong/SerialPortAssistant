using System.Windows;

namespace SerialPortAssistant.Views;

public partial class RenameDialog : Window
{
    public string ResultText => InputBox.Text;

    public RenameDialog(string title, string prompt, string initialValue)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        InputBox.Text = initialValue;
        InputBox.SelectAll();
        InputBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
