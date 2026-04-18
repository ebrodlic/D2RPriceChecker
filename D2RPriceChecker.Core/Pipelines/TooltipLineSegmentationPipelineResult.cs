using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace D2RPriceChecker.Core.Pipelines
{
    public class TooltipLineSegmentationPipelineResult
    {
        public Bitmap Tooltip { get; set; }
        public List<Bitmap> TooltipLines { get; set; }

        public TooltipLineSegmentationPipelineResult(Bitmap tooltip)
        {
            Tooltip = tooltip;
            TooltipLines = new List<Bitmap>();
        }
    }
}
