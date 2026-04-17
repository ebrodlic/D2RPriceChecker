using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace D2RPriceChecker.Pipelines
{
    public class ItemDetectionPipeline
    {
        public ItemDetectionPipeline()
        {

        }

        public ItemMetadata Run(Bitmap image)
        {
            var metadata = new ItemMetadata();

            var detector = new RarityDetector();
            var rarity = detector.Detect(image);

            if (rarity == ItemRarity.Magic || rarity == ItemRarity.Rare || rarity == ItemRarity.Unique || rarity == ItemRarity.Set)
            {
                metadata.Type = ItemType.Equipment;
            }
            metadata.Rarity = rarity;

            // TODO - add more classifications

            return metadata;
            
        }  
    }

    public class RarityDetector
    {
        private readonly List<RarityColorProfile> _profiles = new()
        {
            new() { Rarity = ItemRarity.Normal, TargetColor = ColorTranslator.FromHtml("#FFFFFF") },
            new() { Rarity = ItemRarity.Superior, TargetColor = ColorTranslator.FromHtml("#FFFFFF") },
            new() { Rarity = ItemRarity.Inferior, TargetColor = ColorTranslator.FromHtml("#676767") },
            new() { Rarity = ItemRarity.EtherealOrSocketed, TargetColor = ColorTranslator.FromHtml("#676767") },
            new() { Rarity = ItemRarity.Magic, TargetColor = ColorTranslator.FromHtml("#6C6CFF") },
            new() { Rarity = ItemRarity.Rare, TargetColor = ColorTranslator.FromHtml("#FFFF62") },
            new() { Rarity = ItemRarity.Unique, TargetColor = ColorTranslator.FromHtml("#C6B275")},
            new() { Rarity = ItemRarity.Set, TargetColor = ColorTranslator.FromHtml("#00FF00")}
        };

        public ItemRarity Detect(Bitmap bitmap)
        {
            var counters = _profiles.ToDictionary(p => p.Rarity, _ => 0);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);

                    // TODO - optimize this, find first profile match and stick to that to exit early
                    // or - simply check first x pixels and find the most dominant color profile
                    foreach (var profile in _profiles)
                    {
                        if (IsClose(pixel, profile.TargetColor, profile.Tolerance))
                        {
                            counters[profile.Rarity]++;
                        }
                    }
                }
            }

            // Find dominant rarity
            var dominant = counters
                .OrderByDescending(kv => kv.Value)
                .First();

            // Optional threshold check
            int totalPixels = bitmap.Width * bitmap.Height;
            if (dominant.Value < totalPixels * 0.01) // 1% threshold
                return ItemRarity.Unknown;

            return dominant.Key;
        }

        private bool IsClose(Color a, Color b, int tolerance)
        {
            return Math.Abs(a.R - b.R) <= tolerance &&
                   Math.Abs(a.G - b.G) <= tolerance &&
                   Math.Abs(a.B - b.B) <= tolerance;
        }
    }

    public class ItemMetadata()
    {
        public ItemType Type { get; set; }
        public ItemRarity Rarity { get; set; }
    }

    public class RarityColorProfile
    {
        public ItemRarity Rarity { get; set; }
        public Color TargetColor { get; set; }
        public int Tolerance { get; set; } = 10; // tweak this
    }

    public enum ItemType
    {
        Equipment,
        Item
    }

    public enum ItemRarity
    {
        Unknown,
        Normal,              // White
        Superior,            // White
        Inferior,            // Gray
        EtherealOrSocketed,  // Gray
        Magic,               // Light Blue
        Rare,                // Light Yellow
        Unique,              // Tan
        Set                  // Green
    }
}
