namespace SerialPortSimulator.Models;

public enum TransferDirection
{
    Tx,
    Rx
}

public sealed class TransferEventArgs : EventArgs
{
    public TransferDirection Direction { get; }
    public byte[] Data { get; }

    public TransferEventArgs(TransferDirection direction, byte[] data)
    {
        Direction = direction;
        Data = data;
    }
}
