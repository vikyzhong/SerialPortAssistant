using System.Text;

namespace SerialPortAssistant.Helpers;

public static class AtTextResponseHelper
{
    public static string BuildLine(string body)
    {
        var text = body.Trim();
        if (string.IsNullOrEmpty(text))
            throw new InvalidOperationException("文本回传正文不能为空。");

        return text.EndsWith('\n') ? text : text + "\n";
    }

    public static byte[] BuildBytes(string body) => Encoding.UTF8.GetBytes(BuildLine(body));
}
