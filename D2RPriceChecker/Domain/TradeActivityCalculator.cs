using D2RPriceChecker.Features.Traderie.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Domain
{
    public class TradeActivityCalculator
    {
        public TradeActivityInfo Calculate(List<Trade> trades)
        {
            var result = new TradeActivityInfo();

            foreach (var trade in trades)
            {
                var age = DateTimeOffset.UtcNow - trade.UpdatedAt;

                if (age.TotalDays <= 1)
                    result.Last24h++;
                else if (age.TotalDays <= 3)
                    result.Last3d++;
                else if (age.TotalDays <= 7)
                    result.Last7d++;
                else
                    result.Older++;
            }

            result.Score =
                1.0 * result.Last24h +
                0.6 * result.Last3d +
                0.3 * result.Last7d +
                0.1 * result.Older;

            result.RecencyBoost =
                Math.Min(1.5, 1.0 + (double)result.Last24h / Math.Max(trades.Count, 1));

            result.FinalScore = result.Score * result.RecencyBoost;

            // 🔥 NEW: normalize
            var normalized = result.FinalScore / 20.0;
            normalized = Math.Clamp(normalized, 0.0, 1.0);

            result.NormalizedScore = normalized;

            // 🔥 NEW: assign level
            result.Level = GetLevel(normalized);

            return result;
        }

        private ActivityLevel GetLevel(double n)
        {
            return n switch
            {
                < 0.10 => ActivityLevel.Dead,     // almost nothing
                < 0.30 => ActivityLevel.Low,      // weak
                < 0.55 => ActivityLevel.Medium,   // okay
                < 0.80 => ActivityLevel.High,     // good
                _ => ActivityLevel.VeryHigh       // very liquid
            };
        }
    }
}
