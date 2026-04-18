using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Core.Pipelines
{
    public class TooltipLineSegmentationPipelineSettings
    {
        public int ContentCutoffValue { get; set; } = 5; // Minimum number of content pixels in a row to be considered as content
        public int DistanceThreshold { get; set; } = 40; // max allowed distance(pixels) without matches in the row before breaking the scan, distance between 2 characters
        public int MaxRowsBlobSequenceLength { get; set; } = 11;
        public int PlaceholderProp { get; set; } = 10;
        public int BrightnessThreshold { get; set; } = 80;
        public int MaxChannelThreshold { get; set; } = 140;
        public int CapitalizationOffset { get; set; } = 6; // How many pixels from the top of the line to check for capitalization and glow effects (to determine if it's a header or not)
        public int FloorOffset { get; set; } = 3; // How many pixels from the bottom of the content line to add to cover potential glow effects
        public int PaddingTop { get; set; } = 5; // How many pixels to add to top as padding to include some anti aliasing and dark area
        public int PaddingBottom { get; set; } = 3; // How many pixels to add to bottom as padding to include some anti aliasing and dark area
        public int PaddingLeft { get; set; } = 24;
        public int PaddingRight { get; set; } = 24;
    }
}
