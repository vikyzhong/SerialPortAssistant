using System.Windows;
using SerialPortAssistant.Helpers;

namespace SerialPortAssistant.Views;

public partial class SaveCommandDialog : Window
{
  public sealed class SaveCommandResult
  {
    public required byte Opcode { get; init; }
    public required string Name { get; init; }
    public required string TextAlias { get; init; }
  }

  public SaveCommandResult? Result { get; private set; }

  public SaveCommandDialog(byte opcode, string defaultName, string defaultAlias, string textBody, string payloadHex)
  {
    InitializeComponent();
    OpcodeBox.Text = OpcodeFormat.Format(opcode);
    NameBox.Text = defaultName;
    AliasBox.Text = defaultAlias;
    _ = (textBody, payloadHex);
  }

  private void Ok_Click(object sender, RoutedEventArgs e)
  {
    if (!OpcodeFormat.TryParse(OpcodeBox.Text.Trim(), out var opcode))
    {
      MessageBox.Show("Opcode 须为 0x00–0xFF 的十六进制。", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
      return;
    }

    var name = NameBox.Text.Trim();
    var alias = AliasBox.Text.Trim();
    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(alias))
    {
      MessageBox.Show("名称与文本别名不能为空。", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
      return;
    }

    Result = new SaveCommandResult
    {
      Opcode = opcode,
      Name = name,
      TextAlias = alias
    };
    DialogResult = true;
    Close();
  }
}