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
        if (frame < 0)
        {
            throw new ArgumentException("Invalid frame size.", nameof(frame));
        }

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

    public async ValueTask SaveAsync(Stream stream, CancellationToken cancellation = default)
    {
        var size = HeaderSize + (((Width * Height * 2) + WaitSize) * FrameCount);
        await stream.WriteAsync(buffer, 0, size, cancellation).ConfigureAwait(false);
        await stream.FlushAsync(cancellation).ConfigureAwait(false);
    }

    public static async ValueTask<SenseHatMovie> LoadAsync(Stream stream, CancellationToken cancellation = default)
    {
        var header = ArrayPool<byte>.Shared.Rent(HeaderSize);
        try
        {
            var read = await stream.ReadAsync(header, 0, HeaderSize, cancellation).ConfigureAwait(false);
            if (read != HeaderSize)
            {
                throw new IOException("Hat movie load failed.");
            }

            var width = header[WidthOffset];
            var height = header[HeightOffset];
            var frame = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(FrameOffset));
            if (frame < 0)
            {
                throw new IOException("Hat movie load failed.");
            }

            var movie = new SenseHatMovie(width, height, frame);

            var size = ((width * height * 2) + WaitSize) * frame;
            read = await stream.ReadAsync(movie.buffer, HeaderSize, size, cancellation).ConfigureAwait(false);
            if (read != size)
            {
                throw new IOException("Hat movie load failed.");
            }

            return movie;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(header);
        }
    }
}
