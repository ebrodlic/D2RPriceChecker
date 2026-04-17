using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace D2RPriceChecker.Pipelines
{
    public class TooltipDetectionPipelineYolo : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly float _confidenceThreshold;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="modelPath">Path to ONNX model</param>
        /// <param name="confidenceThreshold">Minimum confidence for detections</param>
        public TooltipDetectionPipelineYolo(string modelPath, float confidenceThreshold = 0.25f)
        {
            _session = new InferenceSession(modelPath);
            _confidenceThreshold = confidenceThreshold;
        }

        static Bitmap Crop(Bitmap src, float[] box)
        {
            int x1 = (int)Math.Max(0, box[0]);
            int y1 = (int)Math.Max(0, box[1]);
            int x2 = (int)Math.Min(src.Width, box[2]);
            int y2 = (int)Math.Min(src.Height, box[3]);

            int w = x2 - x1;
            int h = y2 - y1;

            Rectangle rect = new Rectangle(x1, y1, w, h);
            return src.Clone(rect, src.PixelFormat);
        }

        /// <summary>
        /// Runs YOLO tooltip detection on the input bitmap
        /// Returns the cropped bitmap of the first detected tooltip, or null if none found
        /// </summary>
        public Bitmap Run(Bitmap screenshot)
        {
            float scale;
            int xOffset, yOffset;

            Bitmap resized = BitmapUtils.LetterboxResize(screenshot, 640, 640, out scale, out xOffset, out yOffset);
            float[]? boxOriginal = RunInference(_session, resized, scale, xOffset, yOffset);


            if (boxOriginal != null)
            {
                Bitmap crop = Crop(screenshot, boxOriginal);
                return crop;         
            }
            else
            {
                return null;
            }
        }

        // 🔹 Returns (x1,y1,x2,y2) in ORIGINAL IMAGE COORDS
        static float[]? RunInference(InferenceSession session, Bitmap input640, float scale, int xOffset, int yOffset)
        {
            var tensor = ImageToTensor(input640);
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("images", tensor) };

            using var results = session.Run(inputs);
            var output = results.First().AsTensor<float>(); // [1,300,6]

            float bestConf = 0;
            float[]? bestBox = null;

            for (int i = 0; i < 300; i++)
            {
                float x1 = output[0, i, 0];
                float y1 = output[0, i, 1];
                float x2 = output[0, i, 2];
                float y2 = output[0, i, 3];
                float conf = output[0, i, 4];

                if (conf > 0.25f && conf > bestConf)
                {
                    bestConf = conf;

                    // 🔥 map BACK to original coords
                    float origX1 = (x1 - xOffset) / scale;
                    float origY1 = (y1 - yOffset) / scale;
                    float origX2 = (x2 - xOffset) / scale;
                    float origY2 = (y2 - yOffset) / scale;

                    bestBox = new float[] { origX1, origY1, origX2, origY2 };
                }
            }

            return bestBox;
        }

        /// <summary>
        /// Dispose ONNX session
        /// </summary>
        public void Dispose()
        {
            _session.Dispose();
        }

        static DenseTensor<float> ImageToTensor(Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;
            float[] data = new float[3 * width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color px = bmp.GetPixel(x, y);
                    int idx = y * width + x;
                    data[idx] = px.R / 255f;
                    data[width * height + idx] = px.G / 255f;
                    data[2 * width * height + idx] = px.B / 255f;
                }
            }

            return new DenseTensor<float>(data, new int[] { 1, 3, height, width });
        }
    }
}
