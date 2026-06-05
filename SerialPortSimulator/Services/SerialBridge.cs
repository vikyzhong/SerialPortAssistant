using System.IO.Ports;
using System.Text;
using SerialPortSimulator.Models;

namespace SerialPortSimulator.Services;

public sealed class SerialBridge : IDisposable
{
    private SerialPort? _port;
    private readonly HostCommandParser _commandParser = new();
    private CancellationTokenSource? _readCts;
    private Task? _readTask;

    public bool IsOpen => _port?.IsOpen ?? false;

    public event EventHandler<HostCommand>? CommandReceived;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<TransferEventArgs>? BytesTransferred;

    public SerialBridge()
    {
        _commandParser.CommandReceived += (_, cmd) => CommandReceived?.Invoke(this, cmd);
    }

    public static string[] GetPortNames() => SerialPort.GetPortNames();

    public void Open(string portName, int baudRate)
    {
        Close();

        _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            ReadBufferSize = 256 * 1024,
            WriteBufferSize = 64 * 1024,
            Encoding = Encoding.UTF8
        };

        try
        {
            _port.Open();
        }
        catch (Exception ex)
        {
            _port.Dispose();
            _port = null;
            throw new InvalidOperationException($"无法打开 {portName}: {ex.Message}", ex);
        }

        _readCts = new CancellationTokenSource();
        _readTask = Task.Run(() => ReadLoopAsync(_readCts.Token));
    }

    public void WriteLine(string line)
    {
        if (_port is not { IsOpen: true })
            throw new InvalidOperationException("串口未打开");

        var text = line.EndsWith('\n') ? line : line + "\n";
        var bytes = _port.Encoding.GetBytes(text);
        _port.Write(text);
        BytesTransferred?.Invoke(this, new TransferEventArgs(TransferDirection.Tx, bytes));
    }

    public void WriteRaw(byte[] data)
    {
        if (_port is not { IsOpen: true })
            throw new InvalidOperationException("串口未打开");

        if (data.Length == 0)
            return;

        _port.Write(data, 0, data.Length);
        BytesTransferred?.Invoke(this, new TransferEventArgs(TransferDirection.Tx, data.ToArray()));
    }

    public void Close()
    {
        _readCts?.Cancel();
        try { _readTask?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }

        _readCts?.Dispose();
        _readCts = null;
        _readTask = null;

        if (_port != null)
        {
            try { if (_port.IsOpen) _port.Close(); } catch { /* ignore */ }
            _port.Dispose();
            _port = null;
        }

        _commandParser.Clear();
    }

    private async Task ReadLoopAsync(CancellationToken token)
    {
        var buf = new byte[2048];
        while (!token.IsCancellationRequested && _port is { IsOpen: true })
        {
            try
            {
                var n = await _port.BaseStream.ReadAsync(buf, token);
                if (n <= 0) continue;

                var raw = new byte[n];
                Array.Copy(buf, raw, n);
                BytesTransferred?.Invoke(this, new TransferEventArgs(TransferDirection.Rx, raw));
                _commandParser.Append(raw);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex.Message);
                break;
            }
        }
    }

    public void Dispose() => Close();
}
