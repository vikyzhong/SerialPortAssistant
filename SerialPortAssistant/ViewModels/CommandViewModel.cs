using System.Collections.ObjectModel;

using System.Windows;

using SerialPortAssistant.Helpers;

using SerialPortAssistant.Models;

using SerialPortAssistant.Services;



namespace SerialPortAssistant.ViewModels;



public sealed class CommandViewModel : ViewModelBase

{

  private readonly SettingsService _settingsService;

  private readonly Action<CommandSendBundle> _sendAction;



  private CommandOpcodeDefinition? _selectedOpcode;

  private CommandDraft? _draft;

  private CommandWireFormat _wireFormat = CommandWireFormat.Text;



  private string _textBody = string.Empty;

  private string _payloadHex = string.Empty;

  private string _cmdPrefix = "AT+";

  private string _cmdBinaryPrefix = "CMD";

  private string _binaryFrameSizeLabel = string.Empty;

  private string _sendContentHex = string.Empty;

  private string _statusText = string.Empty;

  private bool _suppressEditableSync;

  private bool _suppressSendContentSync;

  private bool _useRawSendContent;

  private bool _suppressHistorySelection;

  private SentCommandHistoryItem? _selectedHistoryItem;



  public ObservableCollection<CommandOpcodeDefinition> Opcodes { get; } = new();

  public ObservableCollection<CommandOpcodeDefinition> FavoriteOpcodes { get; } = new();

  public ObservableCollection<SentCommandHistoryItem> SendHistory { get; } = new();



  public SentCommandHistoryItem? SelectedHistoryItem

  {

    get => _selectedHistoryItem;

    set

    {

      if (!SetProperty(ref _selectedHistoryItem, value))

        return;

      if (value == null || _suppressHistorySelection)

        return;

      ApplyHistoryItem(value);

    }

  }



  public CommandOpcodeDefinition? SelectedOpcode

  {

    get => _selectedOpcode;

    set

    {

      if (!SetProperty(ref _selectedOpcode, value))

        return;



      if (value != null)

        LoadFromOpcode(value);

    }

  }



  public string CmdPrefix

  {

    get => _cmdPrefix;

    private set => SetProperty(ref _cmdPrefix, value);

  }



  public string CmdBinaryPrefix

  {

    get => _cmdBinaryPrefix;

    private set => SetProperty(ref _cmdBinaryPrefix, value);

  }



  public string TextBody

  {

    get => _textBody;

    set

    {

      if (!SetProperty(ref _textBody, value) || _suppressEditableSync)

        return;



      _useRawSendContent = false;

      RecomposeDraft();

    }

  }



  public string PayloadHex

  {

    get => _payloadHex;

    set

    {

      var incoming = HexPayloadFormat.FormatInputLive(value);

      if (!SetProperty(ref _payloadHex, incoming) || _suppressEditableSync)

        return;



      _useRawSendContent = false;

      // 半字节输入中（如先敲 "1" 再敲 "2"）不触发 Compose 回写，避免 "1" 被当成 0x01。
      if (!HexPayloadFormat.HasIncompleteTrailingNibble(incoming))
        RecomposeDraft();

    }

  }



  public string BinaryFrameSizeLabel

  {

    get => _binaryFrameSizeLabel;

    private set => SetProperty(ref _binaryFrameSizeLabel, value);

  }



  public CommandWireFormat WireFormat

  {

    get => _wireFormat;

    set

    {

      if (!SetProperty(ref _wireFormat, value))

        return;



      _useRawSendContent = false;

      OnPropertyChanged(nameof(IsTextWire));

      OnPropertyChanged(nameof(IsBinaryWire));

      UpdateSendContentFromWireFormat();

      PersistWireFormat();

    }

  }



  public bool IsTextWire

  {

    get => WireFormat == CommandWireFormat.Text;

    set { if (value) WireFormat = CommandWireFormat.Text; }

  }



  public bool IsBinaryWire

  {

    get => WireFormat == CommandWireFormat.Binary;

    set { if (value) WireFormat = CommandWireFormat.Binary; }

  }



  public string SendContentHex

  {

    get => _sendContentHex;

    set

    {

      if (!SetProperty(ref _sendContentHex, value))

        return;

      if (_suppressSendContentSync)

        return;

      EnterRawSendMode();

    }

  }



  public string StatusText

  {

    get => _statusText;

    private set => SetProperty(ref _statusText, value);

  }



  public RelayCommand SendCommand { get; }

  public RelayCommand SaveNewCommandCommand { get; }



  public CommandViewModel(SettingsService settingsService, Action<CommandSendBundle> sendAction, Func<bool> canSend)

  {

    _settingsService = settingsService;

    _sendAction = sendAction;



    SendCommand = new RelayCommand(Send, canSend);

    SaveNewCommandCommand = new RelayCommand(SaveNewCommand);



    ReloadFromSettings();

  }



  public void ReloadFromSettings()

  {

    var settings = _settingsService.Current;

    CommandOpcodeCatalog.ApplyMigration(settings);



    CmdPrefix = string.IsNullOrEmpty(settings.Prefixes.Cmd) ? "AT+" : settings.Prefixes.Cmd;

    CmdBinaryPrefix = string.IsNullOrEmpty(settings.Prefixes.CmdBinary) ? "CMD" : settings.Prefixes.CmdBinary;



    WireFormat = settings.LastCommandWireFormat == CommandWireFormat.Auto

      ? CommandWireFormat.Text

      : settings.LastCommandWireFormat;



    Opcodes.Clear();

    foreach (var op in settings.CommandOpcodes.OrderBy(o => o.Opcode))

      Opcodes.Add(CloneOpcode(op));



    RefreshFavorites();

    RefreshSendHistory();



    if (SelectedOpcode != null)

    {

      var match = Opcodes.FirstOrDefault(o => o.Opcode == SelectedOpcode.Opcode);

      SelectedOpcode = match ?? (Opcodes.Count > 0 ? Opcodes[0] : null);

    }

    else if (Opcodes.Count > 0)

    {

      SelectedOpcode = Opcodes[0];

    }

    else

    {

      _draft = null;

      TextBody = string.Empty;

      PayloadHex = string.Empty;

      BinaryFrameSizeLabel = string.Empty;

      SendContentHex = string.Empty;

      StatusText = string.Empty;

    }

  }



  public void ApplyOpcode(byte opcode)

  {

    var match = Opcodes.FirstOrDefault(o => o.Opcode == opcode);

    if (match != null)

      SelectedOpcode = match;

  }



  private void RefreshFavorites()

  {

    FavoriteOpcodes.Clear();

    foreach (var op in CommandOpcodeCatalog.ResolveFavorites(_settingsService.Current))

    {

      var clone = Opcodes.FirstOrDefault(o => o.Opcode == op.Opcode) ?? CloneOpcode(op);

      FavoriteOpcodes.Add(clone);

    }

  }



  private void RefreshSendHistory(SentCommandHistoryItem? selectItem = null)

  {

    var previous = _selectedHistoryItem;

    _suppressHistorySelection = true;

    SendHistory.Clear();

    foreach (var item in _settingsService.Current.SendHistory)

      SendHistory.Add(item);

    SentCommandHistoryItem? next = null;

    if (selectItem != null)

      next = SendHistory.FirstOrDefault(h => h.Matches(selectItem));

    else if (previous != null)

      next = SendHistory.FirstOrDefault(h => h.Matches(previous));

    if (next == null && SendHistory.Count > 0)

      next = SendHistory[0];

    SelectedHistoryItem = next;

    _suppressHistorySelection = false;

  }



  private void ApplyHistoryItem(SentCommandHistoryItem item)

  {

    var prefixes = _settingsService.Current.Prefixes;

    var kind = SentCommandHistoryRestorer.TryRestore(

      item, prefixes, out var textBody, out var payloadHex, out var rawSendHex);

    _suppressEditableSync = true;

    if (kind == SentCommandRestoreKind.Cmds)

    {

      _useRawSendContent = false;

      WireFormat = CommandWireFormat.Text;

      TextBody = textBody;

      _suppressEditableSync = false;

      RecomposeDraft();

    }

    else if (kind == SentCommandRestoreKind.Cmdb)

    {

      _useRawSendContent = false;

      WireFormat = CommandWireFormat.Binary;

      PayloadHex = payloadHex;

      _suppressEditableSync = false;

      RecomposeDraft();

    }

    else

    {

      _useRawSendContent = true;

      _draft = null;

      TextBody = string.Empty;

      PayloadHex = string.Empty;

      BinaryFrameSizeLabel = string.Empty;

      SendContentHex = rawSendHex;

      StatusText = "发送格式: 原样 · 已填入发送内容";

      _suppressEditableSync = false;

    }

  }



  private void RecordSendHistory(CommandSendBundle bundle)

  {

    var entry = SentCommandHistoryItem.FromBundle(bundle);

    var settings = _settingsService.Current;

    settings.SendHistory.RemoveAll(h => h.Matches(entry));

    settings.SendHistory.Insert(0, entry);

    while (settings.SendHistory.Count > settings.MaxCommandHistory)

      settings.SendHistory.RemoveAt(settings.SendHistory.Count - 1);

    _settingsService.Save(settings);

    RefreshSendHistory(entry);

  }



  private void LoadFromOpcode(CommandOpcodeDefinition definition)

  {

    _suppressEditableSync = true;

    _useRawSendContent = false;

    TextBody = CommandOpcodeResolver.DisplayCommandText(definition);

    PayloadHex = CommandOpcodeCatalog.DefaultPayloadHex(definition);

    _suppressEditableSync = false;

    RecomposeDraft();

  }



  private void RecomposeDraft()

  {

    try

    {

      _draft = CommandComposer.Compose(TextBody, PayloadHex, _settingsService.Current);

      // 弱联动：Compose 会按码表补全另一侧（AT+↔CMD），需写回编辑框；历史命令只存单侧时靠此还原。
      _suppressEditableSync = true;
      TextBody = _draft.TextBody;
      PayloadHex = _draft.PayloadHex;
      _suppressEditableSync = false;

      BinaryFrameSizeLabel = _draft.BinaryByteCount > 0

        ? $"{_draft.BinaryByteCount} 字节（含帧头与 LF）"

        : string.Empty;

      UpdateSendContentFromWireFormat();

    }

    catch (Exception ex)

    {

      _draft = null;

      BinaryFrameSizeLabel = string.Empty;

      _suppressSendContentSync = true;

      SendContentHex = string.Empty;

      _suppressSendContentSync = false;

      StatusText = ex.Message;

    }

  }



  private void EnterRawSendMode()

  {

    _useRawSendContent = true;

    _draft = null;

    StatusText = "发送格式: 原样 · 发送内容框（可编辑）";

  }



  private void UpdateSendContentFromWireFormat()

  {

    if (_draft == null)

      return;



    _suppressSendContentSync = true;

    if (WireFormat == CommandWireFormat.Binary)

    {

      SendContentHex = _draft.BinaryFrameHex;

      StatusText = $"发送格式: 二进制 · {_draft.ResolvedHint}";

    }

    else

    {

      SendContentHex = TrafficFormatter.FormatHex(_draft.TextFrameBytes);

      StatusText = $"发送格式: 文本 · {_draft.ResolvedHint}";

    }

    _suppressSendContentSync = false;

  }



  private void Send()
  {
    if (_useRawSendContent)
    {
      if (TrySendRawContent(showErrors: true, out var rawBundle) && rawBundle != null)
        RecordSendHistory(rawBundle);
      return;
    }

    if (_draft == null)
    {
      MessageBox.Show("请先选择命令或填写有效内容。", "发送", MessageBoxButton.OK, MessageBoxImage.Information);
      return;
    }

    if (TrySendCore(showErrors: true, out var bundle) && bundle != null)
      RecordSendHistory(bundle);
  }

  /// <summary>发送当前编辑帧；失败时不弹窗（供定时发送调用）。</summary>
  public bool TrySend()
  {
    if (_useRawSendContent)
      return TrySendRawContent(showErrors: false, out _);
    return _draft != null && TrySendCore(showErrors: false, out _);
  }

  private bool TrySendRawContent(bool showErrors, out CommandSendBundle? bundle)
  {
    bundle = null;
    try
    {
      if (string.IsNullOrWhiteSpace(SendContentHex))
        throw new InvalidOperationException("发送内容不能为空。");

      var bytes = SerialMonitorHelper.ParseHexPayload(SendContentHex);
      if (bytes.Length == 0)
        throw new InvalidOperationException("发送内容不能为空。");

      bundle = new CommandSendBundle
      {
        RawFrame = bytes,
        DisplayText = TrafficFormatter.FormatHex(bytes),
        PreviewHex = TrafficFormatter.FormatHex(bytes),
        UsedBinaryWire = false
      };

      _sendAction(bundle);
      return true;
    }
    catch (Exception ex)
    {
      if (showErrors)
        MessageBox.Show(ex.Message, "发送失败", MessageBoxButton.OK, MessageBoxImage.Warning);
      return false;
    }
  }

  private bool TrySendCore(bool showErrors, out CommandSendBundle? bundle)
  {
    bundle = null;
    try
    {
      if (WireFormat == CommandWireFormat.Binary)
      {
        if (_draft!.BinaryFrame.Length == 0)
          throw new InvalidOperationException("二进制 payload 无效。");

        bundle = new CommandSendBundle
        {
          RawFrame = _draft.BinaryFrame,
          DisplayText = CommandFrameEncoder.DescribeCmdbPayload(
            SerialMonitorHelper.ParseHexPayload(_draft.PayloadHex)),
          PreviewHex = _draft.BinaryFrameHex,
          UsedBinaryWire = true
        };
      }
      else
      {
        bundle = new CommandSendBundle
        {
          TextLine = _draft!.TextFrameDisplay,
          DisplayText = _draft.TextFrameDisplay,
          PreviewHex = TrafficFormatter.FormatHex(_draft.TextFrameBytes),
          UsedBinaryWire = false
        };
      }

      _sendAction(bundle);
      return true;
    }
    catch (Exception ex)
    {
      if (showErrors)
        MessageBox.Show(ex.Message, "发送失败", MessageBoxButton.OK, MessageBoxImage.Warning);
      return false;
    }
  }

  private void SaveNewCommand()

  {

    if (!CommandOpcodeCatalog.TryParsePayloadHex(PayloadHex, out var opcodeFromPayload, out var parseError))

    {

      MessageBox.Show(parseError, "保存为新命令", MessageBoxButton.OK, MessageBoxImage.Warning);

      return;

    }



    var textBody = TextBody.Trim();
    var defaultName = textBody;
    if (string.IsNullOrEmpty(defaultName) ||
        Opcodes.Any(o => string.Equals(o.Name, defaultName, StringComparison.OrdinalIgnoreCase)))
      defaultName = $"新命令 {OpcodeFormat.Format(opcodeFromPayload)}";

    var dialog = new Views.SaveCommandDialog(
      opcodeFromPayload,
      defaultName,
      textBody,
      textBody,
      PayloadHex.Trim());



    if (dialog.ShowDialog() != true || dialog.Result == null)

      return;



    var result = CommandOpcodeCatalog.ValidateNewCommand(
      dialog.Result.Opcode,
      dialog.Result.Name,
      dialog.Result.TextAlias,
      PayloadHex.Trim(),
      Opcodes.ToList());



    if (!result.Success)

    {

      MessageBox.Show(result.ErrorMessage, "保存为新命令", MessageBoxButton.OK, MessageBoxImage.Warning);

      return;

    }



    var definition = result.Definition!;

    Opcodes.Add(CloneOpcode(definition));

    ReorderOpcodes();



    var settings = _settingsService.Current;

    settings.CommandOpcodes = Opcodes.Select(CloneOpcode).OrderBy(o => o.Opcode).ToList();

    if (!settings.FavoriteOpcodes.Contains(definition.Opcode))

      settings.FavoriteOpcodes.Add(definition.Opcode);



    _settingsService.Save(settings);

    RefreshFavorites();

    SelectedOpcode = Opcodes.First(o => o.Opcode == definition.Opcode);



    MessageBox.Show($"已保存命令 {OpcodeFormat.Format(definition.Opcode)} {definition.Name}。", "保存为新命令",

      MessageBoxButton.OK, MessageBoxImage.Information);

  }



  private void PersistWireFormat()

  {

    var settings = _settingsService.Current;

    settings.LastCommandWireFormat = WireFormat;

    _settingsService.Save(settings);

  }



  private void ReorderOpcodes()
  {
    var sorted = Opcodes.OrderBy(o => o.Opcode).ToList();
    Opcodes.Clear();
    foreach (var op in sorted)
      Opcodes.Add(op);
  }

  private static CommandOpcodeDefinition CloneOpcode(CommandOpcodeDefinition source) =>

    new()

    {

      Opcode = source.Opcode,

      Name = source.Name,

      TextAlias = source.TextAlias,

      DefaultPayloadHex = source.DefaultPayloadHex

    };

}


