namespace SerialPortAssistant.ViewModels;

public sealed class LogLevelFilterItem : ViewModelBase
{
    private bool _isVisible;

    public int LevelId { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#424242";

    public string DisplayLabel => $"{LevelId}:{Name}";

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (SetProperty(ref _isVisible, value))
                VisibilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? VisibilityChanged;
}
