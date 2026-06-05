using System.Text;
using System.Windows;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Services;

namespace SerialPortAssistant.ViewModels;

public sealed class RawSendViewModel : ViewModelBase
{
  private readonly SerialPortService _serialService;
  private readonly Func<bool> _isConnected;

  private string _rawSend = string.Empty;
  private bool _rawSendAsHex = true;
  private bool _rawSendAppendNewline;

  public string RawSend
  {
    get => _rawSend;
    set => SetProperty(ref _rawSend, value);
  }

  public bool RawSendAsHex
  {
    get => _rawSendAsHex;
    set => SetProperty(ref _rawSendAsHex, value);
  }

  public bool RawSendAppendNewline
  {
    get => _rawSendAppendNewline;
    set => SetProperty(ref _rawSendAppendNewline, value);
  }

  public RelayCommand SendCommand { get; }

  public RawSendViewModel(SerialPortService serialService, Func<bool> isConnected)
  {
    _serialService = serialService;
    _isConnected = isConnected;
    SendCommand = new RelayCommand(SendRaw, () => _isConnected());
  }

  public void NotifyConnectionChanged() => SendCommand.RaiseCanExecuteChanged();

  private void SendRaw()
  {
    try
    {
      byte[] payload = RawSendAsHex
        ? SerialMonitorHelper.ParseHexPayload(RawSend)
        : Encoding.UTF8.GetBytes(RawSend);

      if (RawSendAppendNewline && (payload.Length == 0 || payload[^1] != (byte)'\n'))
      {
        var withNl = new byte[payload.Length + 1];
        Array.Copy(payload, withNl, payload.Length);
        withNl[^1] = (byte)'\n';
        payload = withNl;
      }

      if (payload.Length == 0)
      {
        MessageBox.Show("发送内容为空。", "任意发送", MessageBoxButton.OK, MessageBoxImage.Information);
        return;
      }

      _serialService.WriteRaw(payload);
    }
    catch (Exception ex)
    {
      MessageBox.Show(ex.Message, "发送失败", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
  }
}
