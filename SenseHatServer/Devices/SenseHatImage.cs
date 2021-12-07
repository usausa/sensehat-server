namespace SenseHatServer.Devices;

using System.Buffers;
using System.Runtime.CompilerServices;

public sealed class SenseHatImage : IDisposable
{
    public byte Width { get; }

    public byte Height { get; }

    private readonly int bufferSize;

    private readonly byte[] buffer;

    public SenseHatImage(byte width, byte height)
    {
        Width = width;
        Height = height;
        bufferSize = width * height * 2;
        buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        buffer.AsSpan(0, bufferSize).Fill(0);
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixel(byte x, byte y, SenseHatColor color)
    {
        var offset = ((y * Width) + x) * 2;
        buffer[offset] = color.Byte0;
        buffer[offset + 1] = color.Byte1;
    }

    public void Clear()
    {
        buffer.AsSpan(0, bufferSize).Fill(0);
    }

    public async ValueTask ShowAsync(Stream stream, CancellationToken cancellation = default)
    {
        stream.Seek(0, SeekOrigin.Begin);
        await stream.WriteAsync(buffer.AsMemory(0, bufferSize), cancellation).ConfigureAwait(false);
        await stream.FlushAsync(cancellation).ConfigureAwait(false);
    }

    public async ValueTask SaveAsync(Stream stream, CancellationToken cancellation = default)
    {
        await stream.WriteAsync(buffer.AsMemory(0, bufferSize), cancellation).ConfigureAwait(false);
        await stream.FlushAsync(cancellation).ConfigureAwait(false);
    }

    public static async ValueTask<SenseHatImage> LoadAsync(Stream stream, byte width, byte height, CancellationToken cancellation = default)
    {
        var image = new SenseHatImage(width, height);
        var read = await stream.ReadAsync(image.buffer.AsMemory(0, image.bufferSize), cancellation).ConfigureAwait(false);
        if (read != image.bufferSize)
        {
            throw new IOException("Hat image load failed.");
        }

        return image;
    }
}
