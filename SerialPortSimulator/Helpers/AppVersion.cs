namespace SerialPortSimulator.Helpers;

public static class AppVersion
{
    public const string Display = "V0.02";

    public static string WindowTitle(string productName) => $"{productName} {Display}";
}
