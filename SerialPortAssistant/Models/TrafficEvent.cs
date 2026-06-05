namespace SerialPortAssistant.Models;

public enum TrafficDirection
{
    Tx,
    Rx
}

public sealed class BytesTransferredEventArgs : EventArgs
{
    public TrafficDirection Direction { get; }
    public byte[] Data { get; }

    public BytesTransferredEventArgs(TrafficDirection direction, byte[] data)
    {
        Direction = direction;
        Data = data;
    }
}
