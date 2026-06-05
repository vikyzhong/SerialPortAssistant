namespace SerialPortSimulator.Helpers;

public static class AppVersion
{
    public const string Display = "V0.01";

    public static string WindowTitle(string productName) => $"{productName} {Display}";
}
