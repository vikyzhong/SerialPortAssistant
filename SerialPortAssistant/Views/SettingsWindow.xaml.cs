using System.Collections.ObjectModel;
using System.Windows;
using SerialPortAssistant.Helpers;
using SerialPortAssistant.Models;
using SerialPortAssistant.Services;

namespace SerialPortAssistant.Views;

public partial class SettingsWindow : Window
{
  private readonly SettingsService _settingsService;
  private readonly ObservableCollection<OpcodeFavoriteRow> _favoriteRows = new();

  public SettingsWindow(SettingsService settingsService)
  {
    InitializeComponent();
    _settingsService = settingsService;
    LoadFromSettings();
  }

  private void LoadFromSettings()
  {
    var s = _settingsService.Current;
    CommandOpcodeCatalog.ApplyMigration(s);

    DataPrefixBox.Text = s.Prefixes.Data;
    DataBinaryPrefixBox.Text = s.Prefixes.DataBinary;
    LogPrefixBox.Text = s.Prefixes.Log;
    CmdPrefixBox.Text = s.Prefixes.Cmd;
    CmdBinaryPrefixBox.Text = s.Prefixes.CmdBinary;
    AckBinaryPrefixBox.Text = s.Prefixes.AckBinary;

    AtLineEndingBox.ItemsSource = new[]
    {
      new AtLineEndingOption(AtTextLineEnding.CrLf, "CRLF (\\r\\n) — 标准 AT"),
      new AtLineEndingOption(AtTextLineEnding.Lf, "LF (\\n)")
    };
    AtLineEndingBox.DisplayMemberPath = nameof(AtLineEndingOption.Label);
    AtLineEndingBox.SelectedValuePath = nameof(AtLineEndingOption.Value);
    AtLineEndingBox.SelectedValue = s.AtTextLineEnding;

    Ch1NameBox.Text = s.DataChannels.Channel1;
    Ch2NameBox.Text = s.DataChannels.Channel2;
    Ch3NameBox.Text = s.DataChannels.Channel3;

    LogLevelsGrid.ItemsSource = LogLevelConfig.Normalize(s.LogLevels);
    OpcodesGrid.ItemsSource = s.CommandOpcodes.Select(o => new CommandOpcodeDefinition
    {
      Opcode = o.Opcode,
      Name = o.Name,
      TextAlias = o.TextAlias,
      DefaultPayloadHex = o.DefaultPayloadHex
    }).ToList();

    _favoriteRows.Clear();
    foreach (var op in s.CommandOpcodes.OrderBy(o => o.Opcode))
    {
      _favoriteRows.Add(new OpcodeFavoriteRow
      {
        Opcode = op.Opcode,
        Name = op.Name,
        TextAlias = op.TextAlias ?? string.Empty,
        IsFavorite = s.FavoriteOpcodes.Contains(op.Opcode)
      });
    }

    FavoritesGrid.ItemsSource = _favoriteRows;
  }

  private void Save_Click(object sender, RoutedEventArgs e)
  {
    var settings = _settingsService.Current;
    settings.Prefixes = new FieldPrefixes
    {
      Data = DataPrefixBox.Text.Trim(),
      DataBinary = DataBinaryPrefixBox.Text.Trim(),
      Log = LogPrefixBox.Text.Trim(),
      Cmd = CmdPrefixBox.Text.Trim(),
      CmdBinary = CmdBinaryPrefixBox.Text.Trim(),
      AckBinary = AckBinaryPrefixBox.Text.Trim()
    };

    if (LogLevelsGrid.ItemsSource is IEnumerable<LogLevelConfig> gridLevels)
      settings.LogLevels = gridLevels.OrderBy(l => l.LevelId).ToList();
    else
      settings.LogLevels = LogLevelConfig.CreateDefaults();

    if (OpcodesGrid.ItemsSource is IEnumerable<CommandOpcodeDefinition> opcodes)
      settings.CommandOpcodes = opcodes.OrderBy(o => o.Opcode).ToList();
    else
      settings.CommandOpcodes = CommandOpcodeDefinition.CreateDefaults();

    var favoriteSet = _favoriteRows.Where(r => r.IsFavorite).Select(r => r.Opcode).ToHashSet();
    settings.FavoriteOpcodes = settings.CommandOpcodes
      .Where(o => favoriteSet.Contains(o.Opcode))
      .Select(o => o.Opcode)
      .ToList();

    settings.DataChannels = new DataChannelNames
    {
      Channel1 = Ch1NameBox.Text.Trim(),
      Channel2 = Ch2NameBox.Text.Trim(),
      Channel3 = Ch3NameBox.Text.Trim()
    };

    if (AtLineEndingBox.SelectedValue is AtTextLineEnding ending)
      settings.AtTextLineEnding = ending;

    _settingsService.Save(settings);
    DialogResult = true;
    Close();
  }

  private void ResetLogLevels_Click(object sender, RoutedEventArgs e) =>
    LogLevelsGrid.ItemsSource = LogLevelConfig.CreateDefaults();

  private void ResetOpcodes_Click(object sender, RoutedEventArgs e) =>
    OpcodesGrid.ItemsSource = CommandOpcodeDefinition.CreateDefaults();

  private void AddOpcode_Click(object sender, RoutedEventArgs e)
  {
    if (OpcodesGrid.ItemsSource is not List<CommandOpcodeDefinition> list)
      return;

    var next = list.Count > 0 ? (byte)(list.Max(o => o.Opcode) + 1) : (byte)0x20;
    list.Add(new CommandOpcodeDefinition
    {
      Opcode = next,
      Name = "新命令",
      TextAlias = "NEW_CMD",
      DefaultPayloadHex = OpcodeFormat.Format(next)
    });
    OpcodesGrid.Items.Refresh();
  }

  private void RemoveOpcode_Click(object sender, RoutedEventArgs e)
  {
    if (OpcodesGrid.ItemsSource is not List<CommandOpcodeDefinition> list)
      return;

    if (OpcodesGrid.SelectedItem is CommandOpcodeDefinition row)
      list.Remove(row);
  }

  private sealed class OpcodeFavoriteRow
  {
    public byte Opcode { get; init; }
    public string Name { get; init; } = string.Empty;
    public string TextAlias { get; init; } = string.Empty;
    public bool IsFavorite { get; set; }
    public string OpcodeHex => OpcodeFormat.Format(Opcode);
  }

  private sealed record AtLineEndingOption(AtTextLineEnding Value, string Label);
}
