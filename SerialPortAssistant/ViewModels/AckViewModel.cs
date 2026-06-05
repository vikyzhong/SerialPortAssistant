using System.Collections.ObjectModel;
using SerialPortAssistant.Models;

namespace SerialPortAssistant.ViewModels;

public sealed class AckViewModel : ViewModelBase
{
    private readonly Func<AppSettings> _getSettings;

    public ObservableCollection<AckEntry> Entries { get; } = new();

    public AckEntry? LatestEntry { get; private set; }

    public string LatestSummary =>
        LatestEntry == null ? "等待回传..." : LatestEntry.DisplayText;

    public RelayCommand ClearCommand { get; }

    public AckViewModel(Func<AppSettings> getSettings)
    {
        _getSettings = getSettings;
        ClearCommand = new RelayCommand(Clear);
    }

    public void HandleParsedLine(ParsedLine line)
    {
        if (line.Kind is not (LineKind.Ack or LineKind.Cmd))
            return;

        if (line.Kind == LineKind.Cmd && !LooksLikeDeviceResponse(line.Text))
            return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var payload = line.Text;
        var display = $"[{timestamp}] {payload}";

        if (LatestEntry != null)
            LatestEntry.IsLatest = false;

        var entry = new AckEntry
        {
            Timestamp = timestamp,
            Payload = payload,
            DisplayText = display,
            IsLatest = true
        };

        LatestEntry = entry;

        var max = _getSettings().MaxAckEntries;
        while (Entries.Count >= max)
            Entries.RemoveAt(0);

        Entries.Add(entry);

        OnPropertyChanged(nameof(LatestEntry));
        OnPropertyChanged(nameof(LatestSummary));
    }

    public void NotifySent(string cmdText)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var entry = new AckEntry
        {
            Timestamp = timestamp,
            Payload = cmdText,
            DisplayText = $"[{timestamp}] >> {cmdText}",
            IsLatest = true
        };

        if (LatestEntry != null)
            LatestEntry.IsLatest = false;

        LatestEntry = entry;

        var max = _getSettings().MaxAckEntries;
        while (Entries.Count >= max)
            Entries.RemoveAt(0);

        Entries.Add(entry);

        OnPropertyChanged(nameof(LatestSummary));
    }

    private static bool LooksLikeDeviceResponse(string text) =>
        text.StartsWith("ACK", StringComparison.OrdinalIgnoreCase) ||
        text.StartsWith("OK", StringComparison.OrdinalIgnoreCase) ||
        text.StartsWith("ERR", StringComparison.OrdinalIgnoreCase) ||
        text.StartsWith("NAK", StringComparison.OrdinalIgnoreCase);

    private void Clear()
    {
        Entries.Clear();
        LatestEntry = null;
        OnPropertyChanged(nameof(LatestSummary));
    }
}
