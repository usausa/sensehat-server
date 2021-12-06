namespace SenseHatServer.Devices;

using System.Buffers;
using System.Buffers.Binary;

public sealed class SenseHatMovie : IDisposable
{
    private const int FrameSize = 4;
    private const int WidthSize = 1;
    private const int HeightSize = 1;
    private const int ReservedSize = 2;

    private const int FrameOffset = 0;
    private const int WidthOffset = FrameSize + FrameOffset;
    private const int HeightOffset = WidthOffset + WidthSize;
    private const int ReservedOffset = HeightOffset + HeightSize;

    private const int HeaderSize = ReservedOffset + ReservedSize;

    private const int WaitSize = 4;

    private readonly byte[] buffer;

    public int FrameCount => BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(FrameOffset));

    public byte Width => buffer[WidthOffset];

    public byte Height => buffer[HeightOffset];

    public SenseHatMovie(byte width, byte height, int frame)
    {
        buffer = ArrayPool<byte>.Shared.Rent(HeaderSize + (((width * height * 2) + WaitSize) * frame));
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(FrameOffset), frame);
        buffer[WidthOffset] = width;
        buffer[HeightOffset] = height;
        buffer.AsSpan(HeaderSize).Fill(0);
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    public SenseHatFrame GetFrame(int index)
    {
        var offset = HeaderSize + (((Width * Height * 2) + WaitSize) * index);
        return new SenseHatFrame(Width, Height, buffer, offset);
    }

    public void SetWait(int index, int wait)
    {
        var size = Width * Height * 2;
        var offset = HeaderSize + ((size + WaitSize) * index) + size;
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), wait);
    }

    public async ValueTask PlayAsync(Stream stream, CancellationToken cancellation = default)
    {
        var size = Width * Height * 2;
        for (var i = 0; i < FrameCount; i++)
        {
            var offset = HeaderSize + ((size + WaitSize) * i);

            stream.Seek(0, SeekOrigin.Begin);
            await stream.WriteAsync(buffer.AsMemory(offset, size), cancellation).ConfigureAwait(false);
            await stream.FlushAsync(cancellation).ConfigureAwait(false);

            await Task.Delay(BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset + size)), cancellation).ConfigureAwait(false);
        }
    }
}
