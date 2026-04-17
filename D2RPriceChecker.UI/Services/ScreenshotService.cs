using D2RPriceChecker.Util;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace D2RPriceChecker.Services;

public class ScreenshotService
{
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    public Bitmap CapturePrimaryScreen()
    {
        int width = Win32.GetSystemMetrics(SM_CXSCREEN);
        int height = Win32.GetSystemMetrics(SM_CYSCREEN);

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(0, 0, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        }

        return bitmap;
    }
    public Bitmap CaptureGameWindow(string processName)
    {
        var hwnd = FindWindowHandle(processName);

        if (hwnd == IntPtr.Zero)
            throw new Exception("Window not found");

        Win32.GetWindowRect(hwnd, out RECT rect);

        var bitmap = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);

        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bitmap.Size);
        }

        return bitmap;
    }


    private nint FindWindowHandle(string processName)
    {
        var processes = Process.GetProcessesByName(processName);

        foreach (var proc in processes)
        {
            var hwnd = proc.MainWindowHandle;

            if (hwnd == IntPtr.Zero)
            {
                hwnd = FindWindowByEnum(proc.Id);
            }

            if (IsValidWindow(hwnd))
                return hwnd;
        }

        return IntPtr.Zero;
    }

    private nint FindWindowByEnum(int processId)
    {
        IntPtr bestHwnd = IntPtr.Zero;
        int bestArea = 0;

        Win32.EnumWindows((hwnd, lParam) =>
        {
            // Get process ID for this window
            Win32.GetWindowThreadProcessId(hwnd, out uint windowPid);

            if (windowPid != processId)
                return true; // continue

            if (!IsValidWindow(hwnd))
                return true; // continue

            // Get size
            Win32.GetWindowRect(hwnd, out RECT rect);

            int area = rect.Width * rect.Height;

            // Pick the largest window (usually the main game window)
            if (area > bestArea)
            {
                bestArea = area;
                bestHwnd = hwnd;
            }

            return true; // continue enumeration
        }, IntPtr.Zero);

        return bestHwnd;
    }

    private bool IsValidWindow(nint hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        if (!Win32.IsWindow(hwnd))
            return false;

        if (!Win32.IsWindowVisible(hwnd))
            return false;

        if (Win32.IsIconic(hwnd))
            return false;

        Win32.GetWindowRect(hwnd, out RECT rect);

        if (rect.Width < 800 || rect.Height < 600)
            return false;

        return true;
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
}

