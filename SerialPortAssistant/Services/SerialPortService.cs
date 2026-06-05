using System.IO.Ports;
using System.Text;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.Services;

public sealed class SerialPortService : IDisposable
{
    private SerialPort? _port;
    private readonly ProtocolFrameParser _frameParser;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;

    public bool IsOpen => _port?.IsOpen ?? false;
    public bool DtrEnable => _port?.DtrEnable ?? false;
    public bool RtsEnable => _port?.RtsEnable ?? false;
    public Handshake Handshake => _port?.Handshake ?? Handshake.None;

    public event EventHandler<ParsedLine>? LineReceived;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<BytesTransferredEventArgs>? BytesTransferred;

    public SerialPortService(Func<AppSettings> getSettings)
    {
        _frameParser = new ProtocolFrameParser(getSettings);
    }

    public void ConfigureParser(LineParser parser) => _frameParser.Configure(parser);

    public static string[] GetPortNames() => SerialPort.GetPortNames();

    public void Open(SerialSettings settings)
    {
        if (IsOpen)
            Close();

        _port = new SerialPort
        {
            PortName = settings.PortName,
            BaudRate = settings.BaudRate,
            Parity = settings.Parity,
            DataBits = settings.DataBits,
            StopBits = settings.StopBits,
            Handshake = settings.Handshake,
            DtrEnable = settings.DtrEnable,
            RtsEnable = settings.RtsEnable,
            ReadBufferSize = 1024 * 1024,
            WriteBufferSize = 64 * 1024,
            Encoding = Encoding.UTF8,
            NewLine = "\n"
        };

        try
        {
            _port.Open();
        }
        catch (Exception ex)
        {
            _port.Dispose();
            _port = null;
            throw TranslateException(ex, settings.BaudRate);
        }

        _readCts = new CancellationTokenSource();
        _readTask = Task.Run(() => ReadLoopAsync(_readCts.Token));
    }

    public void Close()
    {
        _readCts?.Cancel();

        var port = _port;
        _port = null;
        if (port != null)
        {
            try
            {
                if (port.IsOpen)
                    port.Close();
            }
            catch
            {
                // ignore shutdown races
            }

            try
            {
                port.Dispose();
            }
            catch
            {
                // ignore
            }
        }

        var readTask = _readTask;
        _readTask = null;
        _readCts?.Dispose();
        _readCts = null;

        if (readTask != null)
        {
            _ = readTask.ContinueWith(
                _ => { },
                TaskScheduler.Default);
        }

        _frameParser.Clear();
    }

    public void ApplyLineSignals(bool dtrEnable, bool rtsEnable, Handshake handshake)
    {
        if (_port is not { IsOpen: true })
            throw new InvalidOperationException("串口未打开");

        _port.Handshake = handshake;
        _port.DtrEnable = dtrEnable;
        _port.RtsEnable = rtsEnable;
    }

    public void WriteAtText(string line, AtTextLineEnding lineEnding)
    {
        if (_port is not { IsOpen: true })
            throw new InvalidOperationException("串口未打开");

        var bytes = AtTextLineEndingHelper.ToWireBytes(line, lineEnding);
        _port.Write(bytes, 0, bytes.Length);
        BytesTransferred?.Invoke(this, new BytesTransferredEventArgs(TrafficDirection.Tx, bytes));
    }

    public void WriteLine(string text)
    {
        if (_port is not { IsOpen: true })
            throw new InvalidOperationException("串口未打开");

        var payload = text.EndsWith('\n') ? text : text + "\n";
        var bytes = _port.Encoding.GetBytes(payload);
        _port.Write(payload);
        BytesTransferred?.Invoke(this, new BytesTransferredEventArgs(TrafficDirection.Tx, bytes));
    }

    public void WriteRaw(byte[] data)
    {
        if (_port is not { IsOpen: true })
            throw new InvalidOperationException("串口未打开");

        if (data.Length == 0)
            return;

        _port.Write(data, 0, data.Length);
        BytesTransferred?.Invoke(this, new BytesTransferredEventArgs(TrafficDirection.Tx, data.ToArray()));
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        while (!cancellationToken.IsCancellationRequested && _port is { IsOpen: true })
        {
            try
            {
                var stream = _port.BaseStream;
                var count = await stream.ReadAsync(buffer, cancellationToken);
                if (count <= 0)
                    continue;

                var raw = new byte[count];
                Array.Copy(buffer, raw, count);
                BytesTransferred?.Invoke(this, new BytesTransferredEventArgs(TrafficDirection.Rx, raw));

                foreach (var parsed in _frameParser.Append(raw))
                    LineReceived?.Invoke(this, parsed);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
                break;
            }
        }
    }

    private static Exception TranslateException(Exception ex, int baudRate)
    {
        return ex switch
        {
            ArgumentOutOfRangeException =>
                new InvalidOperationException($"当前驱动/硬件可能不支持波特率 {baudRate}。", ex),
            UnauthorizedAccessException =>
                new InvalidOperationException("无法打开串口，端口可能被其他程序占用。", ex),
            _ => new InvalidOperationException($"打开串口失败: {ex.Message}", ex)
        };
    }

    public void Dispose() => Close();
}
