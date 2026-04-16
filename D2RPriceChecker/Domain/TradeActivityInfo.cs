using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Domain
{
    public class TradeActivityInfo
    {
        public int Last24h { get; set; }
        public int Last3d { get; set; }
        public int Last7d { get; set; }
        public int Older { get; set; }

        public double Score { get; set; }
        public double RecencyBoost { get; set; }
        public double FinalScore { get; set; }
        public double NormalizedScore { get; set;  }
        public ActivityLevel Level { get; set; }
        public string Display => $"{ActivityPercent} ({LevelDisplay})";
        public string Breakdown => $"{Last24h} (24h), {Last3d} (3d), {Last7d} (7d), {Older} (older)";
        public string ActivityPercent => $"{(NormalizedScore * 100):0}%";

        public string LevelDisplay => Level switch
        {
            ActivityLevel.Dead => "Dead",
            ActivityLevel.Low => "Low",
            ActivityLevel.Medium => "Medium",
            ActivityLevel.High => "High",
            ActivityLevel.VeryHigh => "Very High",
            _ => ""
        };
    }

    public enum ActivityLevel
    {
        Dead,
        Low,
        Medium,
        High,
        VeryHigh
    }

}
