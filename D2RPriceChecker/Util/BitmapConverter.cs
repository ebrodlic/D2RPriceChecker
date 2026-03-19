using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace D2RPriceChecker.Util;

public static class BitmapConverter
{
    public static Bitmap BitmapFromBitmapImage(BitmapImage bitmapImage)
    {
        using var ms = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
        encoder.Save(ms);

        // Create a stream-independent copy so the MemoryStream can be safely disposed.
        // GDI+ requires the source stream to remain open for the Bitmap's lifetime,
        // so we draw onto a new Bitmap to decouple it from the stream.
        using var temp = new Bitmap(ms);
        var result = new Bitmap(temp.Width, temp.Height, temp.PixelFormat);
        using (var g = Graphics.FromImage(result))
        {
            g.DrawImage(temp, 0, 0, temp.Width, temp.Height);
        }
        return result;
    }

    public static BitmapImage BitmapImageFromBitmap(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        ms.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
