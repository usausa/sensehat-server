namespace SenseHatServer.Devices;

public static class SenseHatFrameExtensions
{
    public static void Fill(this SenseHatFrame frame, byte left, byte top, byte width, byte height, SenseHatColor color)
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
