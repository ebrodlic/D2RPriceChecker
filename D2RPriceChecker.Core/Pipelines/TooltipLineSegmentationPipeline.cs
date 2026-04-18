using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.Pipelines;
using System.Text;

namespace D2RPriceChecker.Core.Pipelines
{
    public class TooltipLineSegmentationPipeline
    {
        private TooltipLineSegmentationPipelineSettings Settings { get; set; }


        public TooltipLineSegmentationPipelineResult Run(Bitmap tooltip, TooltipLineSegmentationPipelineSettings settings)
        {
            Settings = settings;

            var result = new TooltipLineSegmentationPipelineResult(tooltip);

            //var rowsMask = GetRowsMask(tooltip);

            var blobs = FindTextBlobs(tooltip);

            var lines = GetLinesFromBlobDetections(tooltip, blobs);

            result.TooltipLines = lines;

            return result;
        }

        // Scan tooltip rows to detect areas of text body (10-11 min px height mid section)
        private List<ContentBlobDetection> FindTextBlobs(Bitmap tooltip)
        {
            var result = new List<ContentBlobDetection>();

            //Waiting on 10 vertical matches
            var sequenceCounter = 0;
            var sequenceStartY = -1;
            var sequenceEndY = -1;

            var sequenceMinX = tooltip.Width;
            var sequenceMaxX = 0;

            int mid = tooltip.Width / 2;

            // Scan the rows from top to bottom
            for (int y = 0; y < tooltip.Height; y++)
            {
                var rowScanResult = ScanRowLeft(tooltip, y);

                //TODO - may need to fix x min max detection due to inflation value
                if (!rowScanResult.HasContent)
                    rowScanResult = ScanRowRight(tooltip, y, rowScanResult.ContentPixelCount);

                if (rowScanResult.HasContent)
                {
                    if (sequenceStartY == -1)
                        sequenceStartY = y; // mark the start of a sequence

                    sequenceCounter++;

                    sequenceMinX = Math.Min(sequenceMinX, rowScanResult.MinX);
                    sequenceMaxX = Math.Max(sequenceMaxX, rowScanResult.MaxX);
                }
                else // if empty row
                {
                    if (sequenceCounter > 0)
                    {
                        if (sequenceCounter >= Settings.MaxRowsBlobSequenceLength) // valid sequence
                        {
                            if (sequenceCounter > Settings.MaxRowsBlobSequenceLength) //if sequence larger than 11, trim top part (likely topside of capitalized letters)
                                sequenceStartY += sequenceCounter - Settings.MaxRowsBlobSequenceLength;

                            sequenceEndY = y - 1; // end of the sequence is the last true index

                            result.Add(new ContentBlobDetection
                            {
                                StartY = sequenceStartY,
                                EndY = sequenceEndY,
                                StartX = sequenceMinX,
                                EndX = sequenceMaxX
                            });
                        }

                        sequenceCounter = 0;
                        sequenceStartY = -1;
                        sequenceEndY = -1;

                        sequenceMinX = tooltip.Width;
                        sequenceMaxX = 0;
                    }
                }
            }

            return result;
        }

        private RowScanResult ScanRowLeft(Bitmap tooltip, int y)
        {
            var result = new RowScanResult();

            int mid = tooltip.Width / 2;
            int distanceCounter = 0;

            for (int x = mid; x > 0; x--)
            {
                if (distanceCounter >= Settings.DistanceThreshold)
                {
                    break; // stop scanning this row if we have reached the distance threshold without matches
                }

                Color pixel = tooltip.GetPixel(x, y);

                if (IsPixelContent(pixel))
                {
                    result.ContentPixelCount++;
                    result.MinX = x;

                    distanceCounter = 0; // reset distance counter on match
                }
                else
                {
                    distanceCounter++;
                }
            }

            if (result.ContentPixelCount > Settings.ContentCutoffValue)
            {
                result.HasContent = true;
                result.MaxX = mid + (mid - result.MinX);
            }

            return result;
        }

        private RowScanResult ScanRowRight(Bitmap tooltip, int y, int inflateCount = 0)
        {
            var result = new RowScanResult();

            // For letters like spirit, with very low density, we want to include findings from the left side
            if (inflateCount > 0)
                result.ContentPixelCount = inflateCount;

            int mid = tooltip.Width / 2;
            int distanceCounter = 0;

            for (int x = mid; x < tooltip.Width; x++)
            {
                if (distanceCounter >= Settings.DistanceThreshold)
                {
                    break; // stop scanning this row if we have reached the distance threshold without matches
                }

                Color pixel = tooltip.GetPixel(x, y);

                if (IsPixelContent(pixel))
                {
                    result.ContentPixelCount++;
                    result.MaxX = x;

                    distanceCounter = 0; // reset distance counter on match
                }
                else
                {
                    distanceCounter++;
                }
            }

            if (result.ContentPixelCount > Settings.ContentCutoffValue)
            {
                result.HasContent = true;
                result.MinX = mid - (result.MaxX - mid);
            }

            return result;
        }

        private List<Bitmap> GetLinesFromBlobDetections(Bitmap tooltip, List<ContentBlobDetection> blobs)
        {
            var lines = new List<Bitmap>();

            foreach (var blob in blobs)
            {
                // Add capitalization effect to line start
                int yCapitalizationStart = blob.StartY - Settings.CapitalizationOffset;
                int yfloorOffset = blob.EndY + Settings.FloorOffset;

                // Add padding to the line boundaries
                int paddedStartY = Math.Max(0, yCapitalizationStart - Settings.PaddingTop);
                int paddedEndY = Math.Min(tooltip.Height - 1, yfloorOffset + Settings.PaddingBottom);
                int paddedLineHeight = paddedEndY - paddedStartY + 1;

                int paddedStartX = Math.Max(0, blob.StartX - Settings.PaddingLeft); // TODO - add padding left/right settings if needed DOCUMENT: padding left needs to be higher for runeword and capitalization width
                int paddedEndX = Math.Min(tooltip.Width - 1, blob.EndX + Settings.PaddingRight);
                int paddedLineWidth = paddedEndX - paddedStartX + 1;

                // Extract the line bitmap with padding
                Bitmap lineBitmap = tooltip.Clone(new Rectangle(paddedStartX, paddedStartY, Math.Max(paddedLineWidth, 1), Math.Max(paddedLineHeight, 1)), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                lines.Add(lineBitmap);
            }
            return lines;
        }

        private bool IsPixelContent(Color pixel)
        {
            int r = pixel.R;
            int g = pixel.G;
            int b = pixel.B;

            float brightness = (0.299f * pixel.R) + (0.587f * pixel.G) + (0.114f * pixel.B);
            int maxChannel = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));

            var isContent = brightness > Settings.BrightnessThreshold || maxChannel > Settings.MaxChannelThreshold;

            return isContent;
        }

        //private bool IsPixelTextColor(Color pixel)
        //{
        //    bool isYellow = hue >= 40 && hue <= 65 && saturation > 0.4 && value > 0.5;
        //    bool isBlue = hue >= 180 && hue <= 250 && saturation > 0.4;
        //    bool isGreen = hue >= 80 && hue <= 160 && saturation > 0.4;
        //}
    }

    public class RowScanResult
    {
        public bool HasContent { get; set; }
        public int ContentPixelCount { get; set; }
        public int MinX { get; set; }
        public int MaxX { get; set; }
    }

    public class ContentBlobDetection
    {
        public int StartY { get; set; }
        public int EndY { get; set; }
        public int StartX { get; set; }
        public int EndX { get; set; }
    }
}

