namespace SenseHatServer.Devices;

#pragma warning disable CA1051
#pragma warning disable CA1815
public readonly struct SenseHatColor
{
    public readonly byte Byte1;

    public readonly byte Byte2;

    public SenseHatColor(byte r, byte g, byte b)
    {
        Byte1 = (byte)((b & 0x0F) << 4);
        Byte2 = (byte)(((r & 0x0F) << 4) + (g & 0x0F));
    }
}
#pragma warning restore CA1815
#pragma warning restore CA1051
