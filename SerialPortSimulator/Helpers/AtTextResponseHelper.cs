namespace SerialPortSimulator.Helpers;

public static class AtTextResponseHelper
{
    public static string BuildLine(string body)
    {
        var text = body.Trim();
        if (string.IsNullOrEmpty(text))
            return "ERR empty response";

        return text;
    }
}
