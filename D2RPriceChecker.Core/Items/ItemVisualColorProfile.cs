using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace D2RPriceChecker.Core.Items
{
    public class ItemVisualColorProfile
    {
        public ItemVisualClass Class { get; set; }
        public Color TargetColor { get; set; }
        public int Tolerance { get; set; } = 10; // tweak this
    }
}
