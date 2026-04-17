using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace D2RPriceChecker.Services
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Microsoft.ML.OnnxRuntime;
    using Microsoft.ML.OnnxRuntime.Tensors;

    public class OcrService
    {
        private readonly InferenceSession _session;
        private readonly Dictionary<int, char> _idxToChar;
        private readonly int _targetHeight = 28;
        private readonly int _channels = 3;

        public OcrService(string onnxPath)
        {
            _session = new InferenceSession(onnxPath);

            // Sorted char list like Python
            var charList = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.,:'-+/()% ".ToCharArray();
            Array.Sort(charList);
            _idxToChar = new Dictionary<int, char>();
            for (int i = 0; i < charList.Length; i++)
                _idxToChar[i + 1] = charList[i]; // +1 because 0 is CTC blank
        }

        /// <summary>
        /// Predicts the OCR text from a single bitmap image.
        /// </summary>
        public string PredictText(Bitmap bmp)
        {
            float[] inputTensorData = Preprocess(bmp, out int width);

            var inputTensor = new DenseTensor<float>(inputTensorData, new int[] { 1, _channels, _targetHeight, width });
            var inputs = new[] { NamedOnnxValue.CreateFromTensor("input", inputTensor) };
            using var results = _session.Run(inputs);
            var onnxOutput = results.First().AsTensor<float>().ToArray();

            int C = _idxToChar.Count + 1;
            int T = onnxOutput.Length / (1 * C);
            float[,,] logits = new float[T, 1, C];
            for (int t = 0; t < T; t++)
                for (int c = 0; c < C; c++)
                    logits[t, 0, c] = onnxOutput[t * C + c];

            var decodedIndices = CtcBeamSearchDecode(logits);

            StringBuilder decodedText = new StringBuilder();
            int prevIdx = 0;
            foreach (var idx in decodedIndices)
            {
                if (idx != 0 && idx != prevIdx)
                    decodedText.Append(_idxToChar[idx]);
                prevIdx = idx;
            }

            return decodedText.ToString();
        }

        /// <summary>
        /// Predicts OCR text for a batch of bitmaps.
        /// </summary>
        public List<string> PredictTextBatch(List<Bitmap> bitmaps)
        {
            var results = new List<string>();
            foreach (var bmp in bitmaps)
            {
                results.Add(PredictText(bmp));
            }
            return results;
        }

        /// <summary>
        /// Preprocess bitmap to CHW tensor without padding (dynamic width).
        /// </summary>
        private float[] Preprocess(Bitmap bmp, out int width)
        {
            int H = _targetHeight;
            int C = _channels;
            width = bmp.Width;

            float[,,] raw = new float[H, width, C];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < width; x++)
                {
                    Color px = bmp.GetPixel(x, y);
                    raw[y, x, 0] = (px.R / 255f - 0.5f) / 0.5f;
                    raw[y, x, 1] = (px.G / 255f - 0.5f) / 0.5f;
                    raw[y, x, 2] = (px.B / 255f - 0.5f) / 0.5f;
                }

            float[] tensor = new float[C * H * width];
            for (int c = 0; c < C; c++)
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < width; x++)
                        tensor[c * H * width + y * width + x] = raw[y, x, c];

            return tensor;
        }

        private List<int> CtcGreedyDecode(float[,,] logits, int blank = 0, float minConfidence = 0.5f)
        {
            int T = logits.GetLength(0);
            int C = logits.GetLength(2);
            List<int> output = new List<int>();
            int? prev = null;

            for (int t = 0; t < T; t++)
            {
                // argmax over classes
                int maxIdx = 0;
                float maxVal = logits[t, 0, 0];
                for (int c = 1; c < C; c++)
                {
                    if (logits[t, 0, c] > maxVal)
                    {
                        maxVal = logits[t, 0, c];
                        maxIdx = c;
                    }
                }

                float confidence = Softmax(logits, t, 0, maxIdx);

                if (maxIdx != blank)
                {
                    if (prev == null || prev == blank || prev != maxIdx || confidence > minConfidence)
                        output.Add(maxIdx);
                }

                prev = maxIdx;
            }

            return output;
        }

        private float Softmax(float[,,] logits, int t, int batch, int idx)
        {
            int C = logits.GetLength(2);
            float sumExp = 0f;
            for (int c = 0; c < C; c++)
                sumExp += MathF.Exp(logits[t, batch, c]);
            return MathF.Exp(logits[t, batch, idx]) / sumExp;
        }
        private List<int> CtcBeamSearchDecode(float[,,] logits, int beamWidth = 5, int blank = 0)
        {
            int T = logits.GetLength(0); // sequence length
            int C = logits.GetLength(2); // num_classes
            var Beam = new List<(List<int> seq, float score)> { (new List<int>(), 0f) };

            for (int t = 0; t < T; t++)
            {
                var nextBeam = new List<(List<int> seq, float score)>();

                foreach (var (seq, score) in Beam)
                {
                    for (int c = 0; c < C; c++)
                    {
                        float logProb = logits[t, 0, c]; // raw logits, or you can take softmax and log
                        float newScore = score + logProb;

                        var newSeq = new List<int>(seq);

                        if (c != blank || (seq.Count > 0 && seq.Last() != c))
                            newSeq.Add(c);

                        nextBeam.Add((newSeq, newScore));
                    }
                }

                // Keep top beamWidth sequences
                Beam = nextBeam
                    .OrderByDescending(b => b.score)
                    .Take(beamWidth)
                    .ToList();
            }

            // Return the best sequence
            return Beam[0].seq;
        }
    }
}

