using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace D2RPriceChecker.Services
{
    public class DatasetManager
    {
        private readonly string _basePath;

        private readonly string _screenshotsDirName = "Screenshots";
        private readonly string _tooltipsDirName = "Tooltips";
        private readonly string _masksDirName = "Masks";

        public DatasetManager()
        {
            var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _basePath = Path.Combine(root, "D2RPriceChecker", "Cache");

            Directory.CreateDirectory(Path.Combine(_basePath, _screenshotsDirName));
            Directory.CreateDirectory(Path.Combine(_basePath, _tooltipsDirName));
            Directory.CreateDirectory(Path.Combine(_basePath, _masksDirName));
        }

        public void Save(string id, TooltipPipelineResult result)
        {
            var screenshotPath = Path.Combine(_basePath, _screenshotsDirName, $"{id}.png");
            var tooltipPath = Path.Combine(_basePath, _tooltipsDirName, $"{id}.png");
            var maskPath = Path.Combine(_basePath, _masksDirName, $"{id}.png");

            result.Screenshot.Save(screenshotPath, ImageFormat.Png);
            result.Tooltip?.Save(tooltipPath, ImageFormat.Png);
            result.BorderMask?.Save(maskPath, ImageFormat.Png);
        }
    }
}
