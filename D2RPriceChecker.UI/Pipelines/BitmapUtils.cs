using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace D2RPriceChecker.Pipelines
{
    public static class BitmapUtils
    {
        // -----------------------------
        // 1. Optimized border mask
        // -----------------------------
        public static bool[,] CreateBorderMask(Bitmap img, Color target, int tol)
        {
            int width = img.Width;
            int height = img.Height;
            var mask = new bool[height, width];

            var rect = new Rectangle(0, 0, width, height);
            var bmpData = img.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        byte b = row[x * 4 + 0];
                        byte g = row[x * 4 + 1];
                        byte r = row[x * 4 + 2];

                        mask[y, x] = Math.Abs(r - target.R) <= tol &&
                                     Math.Abs(g - target.G) <= tol &&
                                     Math.Abs(b - target.B) <= tol;
                    }
                }
            }

            img.UnlockBits(bmpData);
            return mask;
        }

        // -----------------------------
        // 2. Optimized rendering mask
        // -----------------------------
        public static Bitmap RenderBorderMask(bool[,] mask)
        {
            int height = mask.GetLength(0);
            int width = mask.GetLength(1);
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        bool isMask = mask[y, x];
                        row[x * 4 + 0] = (byte)(isMask ? 255 : 0); // B
                        row[x * 4 + 1] = (byte)(isMask ? 255 : 0); // G
                        row[x * 4 + 2] = (byte)(isMask ? 255 : 0); // R
                        row[x * 4 + 3] = 255;                      // A
                    }
                }
            }

            bmp.UnlockBits(bmpData);
            return bmp;
        }

        // -----------------------------
        // 3. Optimized render components
        // -----------------------------
        public static Bitmap RenderComponents(int[,] labels, List<Component> components)
        {
            int height = labels.GetLength(0);
            int width = labels.GetLength(1);
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            if (components.Count == 0)
                return bmp;

            // Generate deterministic random colors
            var rng = new Random(42);
            var colors = new Dictionary<int, Color>();
            foreach (var comp in components)
                colors[comp.Id] = Color.FromArgb(rng.Next(80, 256), rng.Next(80, 256), rng.Next(80, 256));

            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int stride = bmpData.Stride;

                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        int id = labels[y, x];
                        Color c = id != 0 ? colors[id] : Color.Black;
                        row[x * 4 + 0] = c.B;
                        row[x * 4 + 1] = c.G;
                        row[x * 4 + 2] = c.R;
                        row[x * 4 + 3] = 255;
                    }
                }
            }

            bmp.UnlockBits(bmpData);
            return bmp;
        }

        public static Bitmap LetterboxResize(Bitmap src, int targetWidth, int targetHeight, out float scale, out int xOffset, out int yOffset)
        {
            int srcWidth = src.Width;
            int srcHeight = src.Height;

            scale = Math.Min((float)targetWidth / srcWidth, (float)targetHeight / srcHeight);
            int newWidth = (int)(srcWidth * scale);
            int newHeight = (int)(srcHeight * scale);

            xOffset = (targetWidth - newWidth) / 2;
            yOffset = (targetHeight - newHeight) / 2;

            Bitmap output = new Bitmap(targetWidth, targetHeight);
            using (Graphics g = Graphics.FromImage(output))
            {
                g.Clear(Color.Black); // padding
                g.DrawImage(src, xOffset, yOffset, newWidth, newHeight);
            }

            return output;
        }
    }
}
