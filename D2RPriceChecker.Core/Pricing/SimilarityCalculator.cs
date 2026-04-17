using D2RPriceChecker.Core.Traderie.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace D2RPriceChecker.Core.Pricing
{
    public class SimilarityCalculator
    {
        public double Compute(Trade trade, List<string> itemText)
        {
            var tradeFeatures = ExtractFeatures(trade.Properties);
            var knownFeatures = tradeFeatures.Keys;
    
            var ocrFeatures = ExtractFeatures(itemText, knownFeatures);

            return Compare(tradeFeatures, ocrFeatures);
        }

        public Dictionary<string, double> ExtractFeatures(IEnumerable<ListingProperty> props)
        {
            var features = new Dictionary<string, double>();

            foreach (var prop in props)
            {
                if(!IsRelevant(prop)) 
                    continue;

                var key = prop.Property.Trim();
                var value = prop.Number!.Value;

                features[key] = value;
            }

            return features;
        }

        public Dictionary<string, double> ExtractFeatures(
            IEnumerable<string> ocrLines,
            IEnumerable<string> knownFeatures)
        {
            var result = new Dictionary<string, double>();

            foreach (var line in ocrLines)
            {
                var value = ParseNumeric(line);
                if (value == null)
                    continue;

                var match = MatchFeature(line, knownFeatures);
                if (match == null)
                    continue;         

                result[match] = value.Value;
            }

            return result;
        }

        private double Compare(
            Dictionary<string, double> a,
            Dictionary<string, double> b)
        {
            var keys = a.Keys.Union(b.Keys);

            double sum = 0;
            int count = 0;

            foreach (var key in keys)
            {
                var va = a.TryGetValue(key, out var av) ? av : 0;
                var vb = b.TryGetValue(key, out var bv) ? bv : 0;

                // normalized difference
                var diff = Math.Abs(va - vb);
                var norm = Math.Max(Math.Abs(va), Math.Abs(vb));

                if (norm > 0)
                    diff /= norm;

                sum += diff;
                count++;
            }

            return count == 0 ? 0 : 1.0 - (sum / count);
        }

        private (string feature, double value)? ParseOcr(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var match = Regex.Match(line, @"([+-]?\d+(\.\d+)?)");

            if (!match.Success)
                return null;

            var value = double.Parse(match.Groups[1].Value);

            // remove number and cleanup
            var feature = Regex.Replace(line, @"([+-]?\d+(\.\d+)?)", "")
                                .Replace("+", "")
                                .Trim()
                                .ToLowerInvariant();

            return (feature, value);
        }

        private double? ParseNumeric(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // Find first number in the string (handles +28, 28%, etc.)
            var match = Regex.Match(line, @"([+-]?\d+(\.\d+)?)");

            if (!match.Success)
                return null;

            if (!double.TryParse(match.Groups[1].Value, out var value))
                return null;

            return value;
        }

        private string? MatchFeature(string line, IEnumerable<string> knownFeatures)
        {
            var normalizedLine = Normalize(line);

            string? best = null;
            double bestScore = 0;

            foreach (var feature in knownFeatures)
            {
                var normalizedFeature = Normalize(feature);

                double score = FuzzyScore(normalizedLine, normalizedFeature);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = feature;
                }
            }

            return bestScore > 0.5 ? best : null;
        }

        private double FuzzyScore(string a, string b)
        {
            // directional containment matters more than set overlap
            if (a.Contains(b) || b.Contains(a))
                return 0.9;

            var aTokens = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var bTokens = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var setA = aTokens.ToHashSet();
            var setB = bTokens.ToHashSet();

            var intersection = setA.Intersect(setB).Count();
            var union = setA.Union(setB).Count();

            return union == 0 ? 0 : (double)intersection / union;
        }

        private double StringSimilarity(string a, string b)
        {
            var aTokens = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var bTokens = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var setA = aTokens.ToHashSet();
            var setB = bTokens.ToHashSet();

            var intersection = setA.Intersect(setB).Count();
            var union = setA.Union(setB).Count();

            return union == 0 ? 0 : (double)intersection / union;
        }

        private string Normalize(string text)
        {
            return Regex.Replace(text, @"\d+", "")
                        .Replace("{value}", "")
                        .Replace("+", "")
                        .Replace("to", "")
                        .ToLowerInvariant()
                        .Replace("  ", " ")
                        .Trim();
        }

        private string NormalizeTemplate(string text)
        {
            return text.Replace("{value}", "")
                       .Replace("+", "")
                       .Replace("to", "")
                       .Trim()
                       .ToLowerInvariant();
        }

        private bool IsRelevant(ListingProperty p)
        {
            return p.Type == "number" && p.Number.HasValue;
        }
    }
}
