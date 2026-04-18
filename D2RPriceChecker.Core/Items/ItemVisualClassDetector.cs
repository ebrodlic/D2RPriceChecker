using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace D2RPriceChecker.Core.Items
{
    public class ItemVisualClassDetector
    {
        private readonly List<ItemVisualColorProfile> _profiles = new()
    {
        new() { Class = ItemVisualClass.White,  TargetColor = ColorTranslator.FromHtml("#FFFFFF") },
        new() { Class = ItemVisualClass.Gray,   TargetColor = ColorTranslator.FromHtml("#676767") },
        new() { Class = ItemVisualClass.Blue,   TargetColor = ColorTranslator.FromHtml("#6C6CFF") },
        new() { Class = ItemVisualClass.Yellow, TargetColor = ColorTranslator.FromHtml("#FFFF62") },
        new() { Class = ItemVisualClass.Tan,    TargetColor = ColorTranslator.FromHtml("#C6B275") },
        new() { Class = ItemVisualClass.Green,  TargetColor = ColorTranslator.FromHtml("#00FF00") },
        new() { Class = ItemVisualClass.Gold,   TargetColor = ColorTranslator.FromHtml("#FFA700") }
    };

        public ItemVisualClass Detect(Bitmap bitmap)
        {
            var scores = _profiles.ToDictionary(p => p.Class, _ => 0);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);

                    foreach (var profile in _profiles)
                    {
                        if (IsClose(pixel, profile.TargetColor, profile.Tolerance))
                        {
                            scores[profile.Class]++;
                        }
                    }
                }
            }

            var best = scores.OrderByDescending(x => x.Value).First();

            int totalPixels = bitmap.Width * bitmap.Height;

            if (best.Value < totalPixels * 0.01)
                return ItemVisualClass.Unknown;

            return best.Key;
        }

        private bool IsClose(Color a, Color b, int tolerance)
        {
            return Math.Abs(a.R - b.R) <= tolerance &&
                   Math.Abs(a.G - b.G) <= tolerance &&
                   Math.Abs(a.B - b.B) <= tolerance;
        }
    }
}
