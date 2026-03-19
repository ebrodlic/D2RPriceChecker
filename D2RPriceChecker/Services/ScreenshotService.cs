using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace D2RPriceChecker.Services
{
    public class ScreenshotService
    {
        // Screen capture
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        // Tooltip border color and detection parameters
        private static readonly Color TargetBorderColor = Color.FromArgb(65, 65, 64);
        private const int BorderTolerance = 8;

        private string applicationName = "D2RPriceChecker";
        private string screenshotDirName = "screenshots";
        private string tooltipDirName = "tooltips";

        private string applicationDataPath = string.Empty;
        private string screenshotsPath = string.Empty;
        private string tooltipsPath = string.Empty;

        //private bool shouldSaveScreenshot = true;
        //private bool shouldSaveTooltip = true;

        public ScreenshotService()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); 

            applicationDataPath = Path.Combine(localAppData, applicationName);
            screenshotsPath = Path.Combine(applicationDataPath, screenshotDirName);
            tooltipsPath = Path.Combine(applicationDataPath, tooltipDirName);

            if (!Directory.Exists(applicationDataPath))         
                Directory.CreateDirectory(applicationDataPath);           

            if (!Directory.Exists(screenshotsPath))        
                Directory.CreateDirectory(screenshotsPath);     

            if (!Directory.Exists(tooltipsPath))      
                Directory.CreateDirectory(tooltipsPath); 
        }

        public Bitmap? CaptureItemTooltip()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            string filename = $"{timestamp}.png";

            var screenshot = CapturePrimaryScreen();

            //if (shouldSaveScreenshot)
            //    screenshot.Save(Path.Combine(screenshotsPath, filename), ImageFormat.Png);

            var tooltip = DetectTooltip(screenshot);

            //if(tooltip != null && shouldSaveTooltip)
            //    tooltip.Save(Path.Combine(tooltipsPath, filename), ImageFormat.Png);

            return tooltip;
        }

        

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

        public BitmapImage LoadScreenshotImage(string path)
        {
            var img = new BitmapImage();        
            img.BeginInit();
            img.UriSource = new Uri(path);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();

            return img;
        }



       



        public Bitmap BitmapFromBitmapImage(BitmapImage bitmapImage)
        {
            using (MemoryStream ms = new MemoryStream())
            {
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
        }

        public BitmapImage BitmapImageFromBitmap(Bitmap bitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        public Bitmap DebugBorderMask(Bitmap img)
        {
            var mask = BuildBorderMask(img, TargetBorderColor, BorderTolerance);
            return MaskToBitmap(mask);
        }

        public Bitmap DebugComponents(Bitmap img)
        {
            var width = img.Width;
            var height = img.Height;

            var mask = BuildBorderMask(img, TargetBorderColor, BorderTolerance);
            var labels = new int[height, width];
            var components = LabelConnectedComponents(mask, labels);

            var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            if (components.Count == 0)
                return result;

            // Assign a distinct color to each component
            var rng = new Random(42);
            var colors = new Dictionary<int, Color>();
            foreach (var comp in components)
            {
                colors[comp.Id] = Color.FromArgb(rng.Next(80, 256), rng.Next(80, 256), rng.Next(80, 256));
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var id = labels[y, x];
                    result.SetPixel(x, y, id != 0 ? colors[id] : Color.Black);
                }
            }

            return result;
        }

        public Bitmap DebugRectangularBorder(Bitmap img)
        {
            var width = img.Width;
            var height = img.Height;
            var totalArea = width * height;

            var mask = BuildBorderMask(img, TargetBorderColor, BorderTolerance);
            var labels = new int[height, width];
            var components = LabelConnectedComponents(mask, labels);

            var result = (Bitmap)img.Clone();

            if (components.Count == 0)
                return result;

            components.Sort((a, b) => b.Area.CompareTo(a.Area));

            Component chosen = components[0];
            foreach (var comp in components)
            {
                if (comp.Area / (double)totalArea <= 0.7)
                {
                    chosen = comp;
                    break;
                }
            }

            var borderRect = FindRectangularBorder(mask, labels, chosen.Id,
                chosen.MinX, chosen.MinY, chosen.MaxX + 1, chosen.MaxY + 1);

            if (!borderRect.HasValue)
                return result;

            // Draw the detected border rectangle in red
            var red = Color.Red;
            var box = borderRect.Value;
            for (var x = box.X1; x < box.X2; x++)
            {
                result.SetPixel(x, Math.Max(box.Y1, 0), red);
                result.SetPixel(x, Math.Min(box.Y2 - 1, height - 1), red);
            }
            for (var y = box.Y1; y < box.Y2; y++)
            {
                result.SetPixel(Math.Max(box.X1, 0), y, red);
                result.SetPixel(Math.Min(box.X2 - 1, width - 1), y, red);
            }

            return result;
        }

        private static Bitmap MaskToBitmap(bool[,] mask)
        {
            var height = mask.GetLength(0);
            var width = mask.GetLength(1);
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    bmp.SetPixel(x, y, mask[y, x] ? Color.White : Color.Black);
                }
            }

            return bmp;
        }

        public Bitmap? DetectTooltip(Bitmap img)
        {
            var width = img.Width;
            var height = img.Height;
            var totalArea = width * height;

            var mask = BuildBorderMask(img, TargetBorderColor, BorderTolerance);

            var labels = new int[height, width];
            var components = LabelConnectedComponents(mask, labels);

            if (components.Count == 0)
            {
                return null;
            }

            components.Sort((a, b) => b.Area.CompareTo(a.Area));

            Component chosen = components[0];
            foreach (var comp in components)
            {
                var ratio = comp.Area / (double)totalArea;
                if (ratio <= 0.7)
                {
                    chosen = comp;
                    break;
                }
            }

            // Start with bounding box of the gray component
            var searchX1 = chosen.MinX;
            var searchY1 = chosen.MinY;
            var searchX2 = chosen.MaxX + 1;
            var searchY2 = chosen.MaxY + 1;

            // Find the actual rectangular border lines (ignoring outlier noise)
            var borderRect = FindRectangularBorder(mask, labels, chosen.Id, searchX1, searchY1, searchX2, searchY2);
            if (!borderRect.HasValue)
            {
                return null;
            }

            var x1 = borderRect.Value.X1;
            var y1 = borderRect.Value.Y1;
            var x2 = borderRect.Value.X2;
            var y2 = borderRect.Value.Y2;

            var rectArea = (x2 - x1) * (y2 - y1);
            var rectRatio = rectArea / (double)totalArea;

            if (rectRatio > 0.7)
            {
                return null;
            }

            // Trim a fixed amount from all edges to remove the border (typically 2-3 pixels thick)
            const int borderWidth = 2;
            x1 += borderWidth;
            y1 += borderWidth;
            x2 -= borderWidth;
            y2 -= borderWidth;

            if (x2 <= x1 || y2 <= y1)
            {
                return null;
            }

            // Find horizontal separator and crop to only keep content above it
            var separator = FindHorizontalSeparator(mask, x1, y1, x2, y2);
            if (separator.HasValue)
            {
                y2 = separator.Value;
            }

            if (x2 <= x1 || y2 <= y1)
            {
                return null;
            }

            var cropRect = Rectangle.FromLTRB(x1, y1, x2, y2);

            return img.Clone(cropRect, img.PixelFormat);
        }


        private static bool[,] BuildBorderMask(Bitmap img, Color target, int tol)
        {
            var mask = new bool[img.Height, img.Width];

            for (var y = 0; y < img.Height; y++)
            {
                for (var x = 0; x < img.Width; x++)
                {
                    var c = img.GetPixel(x, y);
                    var dr = Math.Abs(c.R - target.R);
                    var dg = Math.Abs(c.G - target.G);
                    var db = Math.Abs(c.B - target.B);
                    mask[y, x] = dr <= tol && dg <= tol && db <= tol;
                }
            }

            return mask;
        }

        private static InnerBox? FindRectangularBorder(bool[,] mask, int[,] labels, int targetId, int searchX1, int searchY1, int searchX2, int searchY2)
        {
            var height = mask.GetLength(0);
            var width = mask.GetLength(1);

            // Find the top border: first row with a long horizontal run of target pixels
            int? topBorder = null;
            for (var y = searchY1; y < searchY2; y++)
            {
                var runLength = 0;
                for (var x = searchX1; x < searchX2; x++)
                {
                    if (labels[y, x] == targetId)
                    {
                        runLength++;
                    }
                }

                // If this row has substantial coverage, it's likely the top border
                if (runLength >= (searchX2 - searchX1) * 0.5)
                {
                    topBorder = y;
                    break;
                }
            }

            if (!topBorder.HasValue)
            {
                return null;
            }

            // Find the bottom border: last row with a long horizontal run
            int? bottomBorder = null;
            for (var y = searchY2 - 1; y >= searchY1; y--)
            {
                var runLength = 0;
                for (var x = searchX1; x < searchX2; x++)
                {
                    if (labels[y, x] == targetId)
                    {
                        runLength++;
                    }
                }

                if (runLength >= (searchX2 - searchX1) * 0.5)
                {
                    bottomBorder = y + 1;
                    break;
                }
            }

            if (!bottomBorder.HasValue || bottomBorder <= topBorder)
            {
                return null;
            }

            // Find the left border: first column with a long vertical run
            int? leftBorder = null;
            for (var x = searchX1; x < searchX2; x++)
            {
                var runLength = 0;
                for (var y = topBorder.Value; y < bottomBorder.Value; y++)
                {
                    if (labels[y, x] == targetId)
                    {
                        runLength++;
                    }
                }

                if (runLength >= (bottomBorder.Value - topBorder.Value) * 0.5)
                {
                    leftBorder = x;
                    break;
                }
            }

            if (!leftBorder.HasValue)
            {
                return null;
            }

            // Find the right border: last column with a long vertical run
            int? rightBorder = null;
            for (var x = searchX2 - 1; x >= searchX1; x--)
            {
                var runLength = 0;
                for (var y = topBorder.Value; y < bottomBorder.Value; y++)
                {
                    if (labels[y, x] == targetId)
                    {
                        runLength++;
                    }
                }

                if (runLength >= (bottomBorder.Value - topBorder.Value) * 0.5)
                {
                    rightBorder = x + 1;
                    break;
                }
            }

            if (!rightBorder.HasValue || rightBorder <= leftBorder)
            {
                return null;
            }

            return new InnerBox(leftBorder.Value, topBorder.Value, rightBorder.Value, bottomBorder.Value);
        }

        private static int? FindHorizontalSeparator(bool[,] mask, int x1, int y1, int x2, int y2)
        {
            var width = x2 - x1;
            var minMargin = 10; // Don't detect separators too close to edges

            // Scan from top to bottom looking for a horizontal line of gray pixels
            for (var y = y1 + minMargin; y < y2 - minMargin; y++)
            {
                var grayCount = 0;
                for (var x = x1; x < x2; x++)
                {
                    if (mask[y, x])
                    {
                        grayCount++;
                    }
                }

                // If this row has substantial gray coverage (60%+), it's likely a separator
                var coverage = grayCount / (double)width;
                if (coverage >= 0.6)
                {
                    // Found the separator, return the y coordinate
                    return y;
                }
            }

            return null;
        }

        private static List<Component> LabelConnectedComponents(bool[,] mask, int[,] labels)
        {
            var height = mask.GetLength(0);
            var width = mask.GetLength(1);

            var components = new List<Component>();
            var id = 0;
            var queue = new Queue<Point>();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (!mask[y, x] || labels[y, x] != 0)
                    {
                        continue;
                    }

                    id++;
                    var comp = new Component
                    {
                        Id = id,
                        Area = 0,
                        MinX = x,
                        MinY = y,
                        MaxX = x,
                        MaxY = y
                    };

                    labels[y, x] = id;
                    queue.Enqueue(new Point(x, y));

                    while (queue.Count > 0)
                    {
                        var p = queue.Dequeue();
                        comp.Area++;

                        if (p.X < comp.MinX) comp.MinX = p.X;
                        if (p.Y < comp.MinY) comp.MinY = p.Y;
                        if (p.X > comp.MaxX) comp.MaxX = p.X;
                        if (p.Y > comp.MaxY) comp.MaxY = p.Y;

                        Visit(mask, labels, id, p.X - 1, p.Y, queue);
                        Visit(mask, labels, id, p.X + 1, p.Y, queue);
                        Visit(mask, labels, id, p.X, p.Y - 1, queue);
                        Visit(mask, labels, id, p.X, p.Y + 1, queue);
                    }

                    components.Add(comp);
                }
            }

            return components;
        }

        private static void Visit(bool[,] mask, int[,] labels, int id, int x, int y, Queue<Point> queue)
        {
            var height = mask.GetLength(0);
            var width = mask.GetLength(1);

            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return;
            }

            if (!mask[y, x] || labels[y, x] != 0)
            {
                return;
            }

            labels[y, x] = id;
            queue.Enqueue(new Point(x, y));
        }


        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
   

        private sealed class Component
        {
            public int Id { get; init; }
            public int Area { get; set; }
            public int MinX { get; set; }
            public int MinY { get; set; }
            public int MaxX { get; set; }
            public int MaxY { get; set; }
        }

        private readonly record struct InnerBox(int X1, int Y1, int X2, int Y2);
    }
}
