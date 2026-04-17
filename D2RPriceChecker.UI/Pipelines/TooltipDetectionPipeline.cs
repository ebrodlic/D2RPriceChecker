using System.Drawing;
using System.Drawing.Imaging;

namespace D2RPriceChecker.Pipelines;

public class TooltipDetectionPipeline
{
    private static readonly Color TargetBorderColor = Color.FromArgb(65, 65, 64);
    private const int BorderTolerance = 8;

    /// <summary>
    /// Processes a screenshot and returns all pipeline results as pre-rendered bitmaps.
    /// </summary>
    public TooltipDetectionPipelineResult Run(Bitmap screenshot)
    {
        var totalArea = screenshot.Width * screenshot.Height;
        var labels = new int[screenshot.Height, screenshot.Width];

        // Use optimized helper for pixel operations
        var mask = BitmapUtils.CreateBorderMask(screenshot, TargetBorderColor, BorderTolerance);
        var components = LabelConnectedComponents(mask, labels);
        var borderRect = FindBestBorder(components, mask, labels, totalArea);

        return new TooltipDetectionPipelineResult(screenshot)
        {
            BorderMask = BitmapUtils.RenderBorderMask(mask),
            Components = BitmapUtils.RenderComponents(labels, components),
            BorderOverlay = RenderBorderOverlay(screenshot, borderRect),
            Tooltip = CropTooltip(screenshot, mask, borderRect, totalArea)
        };
    }

    #region Pipeline steps

    private static InnerBox? FindBestBorder(List<Component> components, bool[,] mask, int[,] labels, int totalArea)
    {
        if (components.Count == 0) return null;

        InnerBox? best = null;
        int bestArea = 0;

        foreach (var comp in components)
        {
            if (comp.Area / (double)totalArea > 0.7) continue;

            var rect = FindRectangularBorder(mask, labels, comp.Id,
                comp.MinX, comp.MinY, comp.MaxX + 1, comp.MaxY + 1);

            if (!rect.HasValue) continue;

            var area = (rect.Value.X2 - rect.Value.X1) * (rect.Value.Y2 - rect.Value.Y1);
            if (area > bestArea)
            {
                best = rect;
                bestArea = area;
            }
        }

        return best;
    }

    private static Bitmap? CropTooltip(Bitmap screenshot, bool[,] mask,
        InnerBox? borderRect, int totalArea)
    {
        if (!borderRect.HasValue) return null;

        int x1 = borderRect.Value.X1;
        int y1 = borderRect.Value.Y1;
        int x2 = borderRect.Value.X2;
        int y2 = borderRect.Value.Y2;

        if ((x2 - x1) * (y2 - y1) / (double)totalArea > 0.7) return null;

        const int trimWidth = 2;
        x1 += trimWidth;
        y1 += trimWidth;
        x2 -= trimWidth;
        y2 -= trimWidth;

        if (x2 <= x1 || y2 <= y1) return null;

        var separator = FindHorizontalSeparator(mask, x1, y1, x2, y2);
        if (separator.HasValue)
            y2 = separator.Value;

        if (x2 <= x1 || y2 <= y1) return null;

        return screenshot.Clone(Rectangle.FromLTRB(x1, y1, x2, y2), screenshot.PixelFormat);
    }

    #endregion

    #region Rendering (still uses SetPixel only for overlay)

    private static Bitmap RenderBorderOverlay(Bitmap original, InnerBox? borderRect)
    {
        var result = (Bitmap)original.Clone();

        if (!borderRect.HasValue)
            return result;

        var red = Color.Red;
        var box = borderRect.Value;
        int height = original.Height;
        int width = original.Width;

        for (int x = box.X1; x < box.X2; x++)
        {
            result.SetPixel(x, Math.Max(box.Y1, 0), red);
            result.SetPixel(x, Math.Min(box.Y2 - 1, height - 1), red);
        }

        for (int y = box.Y1; y < box.Y2; y++)
        {
            result.SetPixel(Math.Max(box.X1, 0), y, red);
            result.SetPixel(Math.Min(box.X2 - 1, width - 1), y, red);
        }

        return result;
    }

    #endregion

    #region Border mask and connected components

    private static List<Component> LabelConnectedComponents(bool[,] mask, int[,] labels)
    {
        int height = mask.GetLength(0);
        int width = mask.GetLength(1);

        var components = new List<Component>();
        int id = 0;
        var queue = new Queue<Point>();

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (!mask[y, x] || labels[y, x] != 0) continue;

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

        return components;
    }

    private static void Visit(bool[,] mask, int[,] labels, int id, int x, int y, Queue<Point> queue)
    {
        int height = mask.GetLength(0);
        int width = mask.GetLength(1);

        if (x < 0 || y < 0 || x >= width || y >= height) return;
        if (!mask[y, x] || labels[y, x] != 0) return;

        labels[y, x] = id;
        queue.Enqueue(new Point(x, y));
    }

    #endregion

    #region Rectangular border detection

    private static InnerBox? FindRectangularBorder(bool[,] mask, int[,] labels, int targetId,
        int searchX1, int searchY1, int searchX2, int searchY2)
    {
        // Find the top border: first row with a long horizontal run of target pixels
        int? topBorder = null;
        for (var y = searchY1; y < searchY2; y++)
        {
            var runLength = 0;
            for (var x = searchX1; x < searchX2; x++)
            {
                if (labels[y, x] == targetId)
                    runLength++;
            }

            if (runLength >= (searchX2 - searchX1) * 0.5)
            {
                topBorder = y;
                break;
            }
        }

        if (!topBorder.HasValue)
            return null;

        // Find the bottom border: last row with a long horizontal run
        int? bottomBorder = null;
        for (var y = searchY2 - 1; y >= searchY1; y--)
        {
            var runLength = 0;
            for (var x = searchX1; x < searchX2; x++)
            {
                if (labels[y, x] == targetId)
                    runLength++;
            }

            if (runLength >= (searchX2 - searchX1) * 0.5)
            {
                bottomBorder = y + 1;
                break;
            }
        }

        if (!bottomBorder.HasValue || bottomBorder <= topBorder)
            return null;

        // Find the left border: first column with a long vertical run
        int? leftBorder = null;
        for (var x = searchX1; x < searchX2; x++)
        {
            var runLength = 0;
            for (var y = topBorder.Value; y < bottomBorder.Value; y++)
            {
                if (labels[y, x] == targetId)
                    runLength++;
            }

            if (runLength >= (bottomBorder.Value - topBorder.Value) * 0.5)
            {
                leftBorder = x;
                break;
            }
        }

        if (!leftBorder.HasValue)
            return null;

        // Find the right border: last column with a long vertical run
        int? rightBorder = null;
        for (var x = searchX2 - 1; x >= searchX1; x--)
        {
            var runLength = 0;
            for (var y = topBorder.Value; y < bottomBorder.Value; y++)
            {
                if (labels[y, x] == targetId)
                    runLength++;
            }

            if (runLength >= (bottomBorder.Value - topBorder.Value) * 0.5)
            {
                rightBorder = x + 1;
                break;
            }
        }

        if (!rightBorder.HasValue || rightBorder <= leftBorder)
            return null;

        return new InnerBox(leftBorder.Value, topBorder.Value, rightBorder.Value, bottomBorder.Value);
    }

    #endregion

    #region Separator detection

    private static int? FindHorizontalSeparator(bool[,] mask, int x1, int y1, int x2, int y2)
    {
        int width = x2 - x1;
        int minMargin = 10;

        for (int y = y1 + minMargin; y < y2 - minMargin; y++)
        {
            int grayCount = 0;
            for (int x = x1; x < x2; x++)
                if (mask[y, x])
                    grayCount++;

            if (grayCount / (double)width >= 0.6)
                return y;
        }

        return null;
    }

    #endregion

    #region Helper types 

    public readonly record struct InnerBox(int X1, int Y1, int X2, int Y2);

    #endregion
}
