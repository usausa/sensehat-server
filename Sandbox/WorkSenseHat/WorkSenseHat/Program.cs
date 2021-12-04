namespace WorkSenseHat;

using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

public static class Program
{
    public static void Main(string[] args)
    {
        using var stream = File.OpenWrite(args[0]);

        Scroll(stream, new HatColor(0xF, 0x0, 0x0));
        Scroll(stream, new HatColor(0xF, 0xC, 0x00));
        Scroll(stream, new HatColor(0x0, 0xF, 0x0));
        Scroll(stream, new HatColor(0xF, 0x0, 0x0));
        Scroll(stream, new HatColor(0xF, 0xC, 0x00));
        Scroll(stream, new HatColor(0x0, 0xF, 0x0));
        Scroll(stream, new HatColor(0xF, 0x0, 0x0));
        Scroll(stream, new HatColor(0xF, 0xC, 0x00));
        Scroll(stream, new HatColor(0x0, 0xF, 0x0));

        //Loop(stream);
    }

    private static void Scroll(Stream stream, HatColor color)
    {
        using var movie = new SensorHatMovie(8, 8, 16);
        movie.GetFrame(0).Fill(7, 0, 1, 8, color);
        movie.GetFrame(1).Fill(6, 0, 2, 8, color);
        movie.GetFrame(2).Fill(5, 0, 3, 8, color);
        movie.GetFrame(3).Fill(4, 0, 4, 8, color);
        movie.GetFrame(4).Fill(3, 0, 5, 8, color);
        movie.GetFrame(5).Fill(2, 0, 6, 8, color);
        movie.GetFrame(6).Fill(1, 0, 7, 8, color);
        movie.GetFrame(7).Fill(0, 0, 8, 8, color);
        movie.GetFrame(8).Fill(0, 0, 7, 8, color);
        movie.GetFrame(9).Fill(0, 0, 6, 8, color);
        movie.GetFrame(10).Fill(0, 0, 5, 8, color);
        movie.GetFrame(11).Fill(0, 0, 4, 8, color);
        movie.GetFrame(12).Fill(0, 0, 3, 8, color);
        movie.GetFrame(13).Fill(0, 0, 2, 8, color);
        movie.GetFrame(14).Fill(0, 0, 1, 8, color);
        movie.GetFrame(15).Fill(0, 0, 0, 8, color);

        for (var i = 0; i < movie.FrameCount; i++)
        {
            movie.SetWait(i, 67);
        }

        movie.Play(stream);
    }

    private static void Loop(FileStream stream)
    {
        using var image = new SenseHatImage(8, 8);

        while (true)
        {
            for (var loop = 0; loop < 3; loop++)
            {
                for (byte y = 0; y < image.Height; y++)
                {
                    for (byte x = 0; x < image.Width; x++)
                    {
                        //image.Clear();
                        switch (loop)
                        {
                            case 0:
                                image.SetPixel(x, y, new(255, 0, 0));
                                break;
                            case 1:
                                image.SetPixel(x, y, new(0, 255, 0));
                                break;
                            case 2:
                                image.SetPixel(x, y, new(0, 0, 255));
                                break;
                        }

                        stream.Seek(0, SeekOrigin.Begin);
                        image.Write(stream);
                        Thread.Sleep(15);
                    }
                }
            }
        }
    }
}

public sealed class SensorHatMovie : IDisposable
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

    public SensorHatMovie(byte width, byte height, int frame)
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

    public void Play(Stream stream)
    {
        var size = Width * Height * 2;
        for (var i = 0; i < FrameCount; i++)
        {
            var offset = HeaderSize + ((size + WaitSize) * i);

            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(buffer, offset, size);
            stream.Flush();

            Thread.Sleep(BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset + size)));
        }
    }
}

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
    public void SetPixel(byte x, byte y, HatColor color)
    {
        var offset = (((y * Width) + x) * 2) + bufferOffset;
        buffer[offset] = color.Byte1;
        buffer[offset + 1] = color.Byte2;
    }
}

public readonly struct HatColor
{
    public readonly byte Byte1;

    public readonly byte Byte2;

    public HatColor(byte r, byte g, byte b)
    {
        Byte1 = (byte)((b & 0x0F) << 4);
        Byte2 = (byte)(((r & 0x0F) << 4) + (g & 0x0F));
    }
}

public sealed class SenseHatImage : IDisposable
{
    public byte Width { get; }

    public byte Height { get; }

    private readonly int bufferSize;

    private readonly bool usePool;

    private readonly byte[] buffer;

    private readonly int bufferOffset;

    public SenseHatImage(byte width, byte height, byte[]? buffer = null, int offset = 0)
    {
        Width = width;
        Height = height;
        bufferSize = width * height * 2;
        if (buffer is null)
        {
            usePool = true;
            this.buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            bufferOffset = 0;
            buffer.AsSpan(0, bufferSize).Fill(0);
        }
        else
        {
            usePool = false;
            this.buffer = buffer;
            bufferOffset = offset;
        }
    }

    public void Dispose()
    {
        if (usePool)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixel(byte x, byte y, HatColor color)
    {
        var offset = (((y * Width) + x) * 2) + bufferOffset;
        buffer[offset] = color.Byte1;
        buffer[offset + 1] = color.Byte2;
    }

    public void Clear()
    {
        buffer.AsSpan(0, bufferSize).Fill(0);
    }

    public void Write(Stream stream)
    {
        stream.Write(buffer, 0, bufferSize);
        stream.Flush();
    }
}

public static class SenseHatFrameExtensions
{
    public static void Fill(this SenseHatFrame frame, byte left, byte top, byte width, byte height, HatColor color)
    {
        for (var y = top; y < top + height; y++)
        {
            for (var x = left; x < left + width; x++)
            {
                frame.SetPixel(x, y, color);
            }
        }
    }
}
