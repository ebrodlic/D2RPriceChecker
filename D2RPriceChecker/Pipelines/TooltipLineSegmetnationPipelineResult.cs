using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace D2RPriceChecker.Pipelines
{
    public class TooltipLineSegmetnationPipelineResult
    {
        public Bitmap Tooltip { get; set; }
        public List<Bitmap> TooltipLines { get; set; }

        public TooltipLineSegmetnationPipelineResult(Bitmap tooltip)
        {
            Tooltip = tooltip;
            TooltipLines = new List<Bitmap>();
        }
    }
}
