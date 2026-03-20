using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace D2RPriceChecker.Services;

public class ScreenshotService
{
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    public Bitmap CapturePrimaryScreen()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(0, 0, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }

    public Bitmap LoadBitmapSafe(string path)
    {
        using var temp = new Bitmap(path);
        var bitmap = new Bitmap(temp.Width, temp.Height, PixelFormat.Format32bppArgb);

        using (var g = Graphics.FromImage(bitmap))
        {
            g.DrawImage(temp, 0, 0, temp.Width, temp.Height);
        }

        return bitmap;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
}
