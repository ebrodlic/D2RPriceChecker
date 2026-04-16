using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Features.Traderie.Model
{
    public class Trade
    {
        public string ItemName { get; set; } = "";
        public int Amount { get; set; }
        public List<ListingProperty> Properties { get; set; } = new();
        public List<string> Attributes { get; set; } = new();
        public List<string> Labels { get; set; } = new();
        public List<Price> Prices { get; set; } = new();
        public List<PriceGroup> PriceGroups { get; set; } = new();
        public DateTime UpdatedAt { get; set; }
        public string TimeAgo
        {
            get
            {
                var diff = DateTimeOffset.UtcNow - UpdatedAt;

                if (diff.TotalMinutes < 1) return "now";
                if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}h ago";
                return $"{(int)diff.TotalDays}d ago";
            }
        }

        public string Display =>
            PriceGroups == null
                ? "-"
                : string.Join(" OR ",
                    PriceGroups.Select(g => g.Display));
    }
}
