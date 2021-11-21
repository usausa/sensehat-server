namespace WorkSenseHat;

using System.Buffers;
using System.Runtime.CompilerServices;

public static class Program
{
    public static void Main(string[] args)
    {
        using var image = new SenseHatImage(8, 8);

        using var stream = File.OpenWrite(args[0]);
        while (true)
        {
            for (var loop = 0; loop < 3; loop++)
            {
                for (var y = 0; y < image.Height; y++)
                {
                    for (var x = 0; x < image.Width; x++)
                    {
                        //image.Clear();
                        switch (loop)
                        {
                            case 0:
                                image.SetPixel(x, y, 255, 0, 0);
                                break;
                            case 1:
                                image.SetPixel(x, y, 0, 255, 0);
                                break;
                            case 2:
                                image.SetPixel(x, y, 0, 0, 255);
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

public sealed class SenseHatImage : IDisposable
{
    public int Width { get; }

    public int Height { get; }

    private readonly int bufferSize;

    private readonly byte[] buffer;

    public SenseHatImage(int width, int height)
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
    public void SetPixel(int x, int y, byte r, byte g, byte b)
    {
        var offset = ((y * Width) + x) * 2;
        buffer[offset] = (byte)((b & 0x0F) << 4);
        buffer[offset + 1] = (byte)(((r & 0x0F) << 4) + (g & 0x0F));
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
