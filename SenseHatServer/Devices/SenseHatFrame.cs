namespace SenseHatServer.Devices;

using System.Runtime.CompilerServices;

public sealed class SenseHatFrame
{
    public byte Width { get; }

    public byte Height { get; }

    private readonly byte[] buffer;

    private readonly int bufferOffset;

    public SenseHatFrame(byte width, byte height, byte[] buffer, int offset)
    {
        Width = width;
        Height = height;
        this.buffer = buffer;
        bufferOffset = offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixel(byte x, byte y, SenseHatColor color)
    {
        var offset = (((y * Width) + x) * 2) + bufferOffset;
        buffer[offset] = color.Byte0;
        buffer[offset + 1] = color.Byte1;
    }
}
