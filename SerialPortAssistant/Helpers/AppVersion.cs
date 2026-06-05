using System.Reflection;

namespace SerialPortAssistant.Helpers;

public static class AppVersion
{
    public const string Display = "V0.02";

    public static string WindowTitle(string productName) => $"{productName} {Display}";

    public static string AboutLine() => $"版本 {ReadDisplay()}";

    public static string ReadDisplay()
    {
        var info = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        return string.IsNullOrWhiteSpace(info) ? Display : info.Split('+')[0].Trim();
    }
}
