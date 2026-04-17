using D2RPriceChecker.Pipelines;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace D2RPriceChecker.Services
{
    public class CacheService
    {
        public string RootDir { get;  }
        public string CacheDir { get; }

        private readonly string _screenshotsDir;
        private readonly string _masksDir;
        private readonly string _tooltipDir;
        private readonly string _linesDir;

        private readonly bool _saveScreenshots = true;
        private readonly bool _saveMasks = false;
        private readonly bool _saveTooltips = true;
        private readonly bool _saveLines = true;


        public CacheService()
        {
            RootDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D2RPriceChecker");
            CacheDir = Path.Combine(RootDir, "Cache");

            _screenshotsDir = Path.Combine(CacheDir, "Screenshots");
            _masksDir = Path.Combine(CacheDir, "Masks");
            _tooltipDir = Path.Combine(CacheDir, "Tooltips");
            _linesDir = Path.Combine(CacheDir, "Lines");

            CreateDirectories();
        }

        private void CreateDirectories()
        {
            Directory.CreateDirectory(RootDir);
            Directory.CreateDirectory(CacheDir);

            Directory.CreateDirectory(_screenshotsDir);
            Directory.CreateDirectory(_masksDir);
            Directory.CreateDirectory(_tooltipDir);
            Directory.CreateDirectory(_linesDir);
        }

        public void Save(string id, TooltipDetectionPipelineResult result)
        {
            var screenshotPath = Path.Combine(_screenshotsDir, $"{id}.png");
            var tooltipPath = Path.Combine(_tooltipDir, $"{id}.png");
            var maskPath = Path.Combine(_masksDir, $"{id}.png");

            if (_saveScreenshots)
                result.Screenshot.Save(screenshotPath, ImageFormat.Png);

            if(_saveMasks)
                result.BorderMask?.Save(maskPath, ImageFormat.Png);

            if (_saveTooltips)
                result.Tooltip?.Save(tooltipPath, ImageFormat.Png);
        }

        public void Save(string id, TooltipLineSegmetnationPipelineResult result)
        {
            if (_saveLines)
            {
                for (int i = 0; i < result.TooltipLines.Count; i++)
                {
                    var line = result.TooltipLines[i];
                    var linePath = Path.Combine(_linesDir, $"{id}_{i:D2}.png");

                    line.Save(linePath, ImageFormat.Png);
                }
            }
        }
    }
}
