namespace SerialPortSimulator.Services;

public sealed class TestDataGenerator
{
    private double _phase;

    public (ushort V1, ushort V2, ushort V3) NextSampleValues(bool threeChannels)
    {
        _phase += 0.15;
        var baseVal = 16000 + 800 * Math.Sin(_phase);
        var v1 = (ushort)Math.Clamp(baseVal + 200 * Math.Sin(_phase * 1.1), 0, 65535);
        var v2 = (ushort)Math.Clamp(baseVal + 200 * Math.Sin(_phase * 1.3 + 1), 0, 65535);
        var v3 = (ushort)Math.Clamp(baseVal + 200 * Math.Sin(_phase * 0.9 + 2), 0, 65535);
        return (v1, v2, v3);
    }

    public string NextDataLine(bool threeChannels)
    {
        var (v1, v2, v3) = NextSampleValues(threeChannels);
        return threeChannels
            ? $"DATA:{v1},{v2},{v3}"
            : $"DATA:{v1}";
    }

    public byte[] NextDatbFrame(bool threeChannels)
    {
        var (v1, v2, v3) = NextSampleValues(threeChannels);
        return threeChannels
            ? Helpers.DatbFrameHelper.Build([v1, v2, v3])
            : Helpers.DatbFrameHelper.Build([v1]);
    }

    private readonly Random _random = new();
    private int _logCounter;

    private static readonly string[] LogMessages =
    [
        "IMU 采样正常",
        "温度 42.5℃",
        "Flash 写入完成",
        "进入低功耗模式",
        "校准数据已加载",
        "DMA 缓冲区警告",
        "看门狗已喂狗"
    ];

    public string NextLogLine(bool useLevelByte)
    {
        var level = _random.Next(0, 5);
        var msg = LogMessages[_logCounter++ % LogMessages.Length];

        if (useLevelByte)
            return "LOG:" + (char)level + msg;

        return $"LOG:{level},{msg}";
    }
}
