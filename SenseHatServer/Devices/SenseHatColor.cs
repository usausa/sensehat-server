namespace SenseHatServer.Devices;

#pragma warning disable CA1051
#pragma warning disable CA1815
public readonly struct SenseHatColor
{
    public readonly byte Byte0;

    public readonly byte Byte1;

    public SenseHatColor(byte r, byte g, byte b)
    {
        var rgb = ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3);
        Byte0 = (byte)(rgb & 0xFF);
        Byte1 = (byte)((rgb >> 8) & 0xFF);
    }
}
#pragma warning restore CA1815
#pragma warning restore CA1051
