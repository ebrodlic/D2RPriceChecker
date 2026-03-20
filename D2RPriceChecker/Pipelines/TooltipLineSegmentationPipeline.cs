using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace D2RPriceChecker.Pipelines
{
    internal class TooltipLineSegmentationPipeline
    {
        public int _brightnessThreshold { get; set; } = 70;
        public int _maxChannelThreshold { get; set; } = 140;
        public double _densityThresholdRatio { get; set; } = 0.005;
        public int _padding { get; set; } = 5;


        // Row Density Calculation/Brightness thresholding
        // - Iterate through each row of the tooltip image and calculate the average brightness.    

        // go through each row of the tooltip image and calculate the average brightness
        // if the average brightness is above a certain threshold, consider it as a line break
        // store the positive lines in a list, until we hit empty line
        public TooltipLineSegmetnationPipelineResult Run(Bitmap tooltip)
        {
            var result = new TooltipLineSegmetnationPipelineResult(tooltip);

            var minDensity = tooltip.Width * _densityThresholdRatio;

            var cutoffStartIndex = -1;
            var cutoffEndIndex = -1;

            for (int y = 0; y < tooltip.Height; y++)
            {
                var count = 0;

                for (int x = 0; x < tooltip.Width; x++)
                {
                    Color pixel = tooltip.GetPixel(x, y);
                    int r = pixel.R;
                    int g = pixel.G;
                    int b = pixel.B;

                    float brightness = (0.299f * pixel.R) + (0.587f * pixel.G) + (0.114f * pixel.B);
                    int maxChannel = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));

                    bool isTextPixel = brightness > _brightnessThreshold || maxChannel > _maxChannelThreshold;

                    if (isTextPixel)
                        count++;
                }

                // if the count of text pixels in the row is above the minimum density threshold, we consider it a text row
                if (count > minDensity)
                {
                    // content detected, note start of line if not already noted, and continue
                    if (cutoffStartIndex == -1)
                        cutoffStartIndex = y;

                    // this is a line break, we can add the line to the result
                    //var line = tooltip.Clone(new Rectangle(0, y, tooltip.Width, 1), tooltip.PixelFormat);
                    //result.TooltipLines.Add(line);
                }
                else
                {
                    if(cutoffStartIndex != -1)
                    {
                        cutoffEndIndex = y;
 
                        var lineY = Math.Max(cutoffStartIndex - _padding, 0);
                        var lineHeight = Math.Min(cutoffEndIndex - cutoffStartIndex + (2 * _padding), tooltip.Height - lineY);
                        // alternate:  Math.Min(cutoffEndIndex - cutoffStartIndex + (2 * padding), tooltip.Height - cutoffStartIndex + padding))


                        var line = tooltip.Clone(new Rectangle(0, lineY, tooltip.Width, lineHeight), tooltip.PixelFormat);
                        result.TooltipLines.Add(line);

                        //reset indexes
                        cutoffStartIndex = -1;
                        cutoffEndIndex = -1;

                        // increment y by padding to skip over the padding area we just added to the line, as we don't want to detect it as a new line

                        y += _padding - 1;
                    }
                }
            }

            return result;
        }
    }
}
