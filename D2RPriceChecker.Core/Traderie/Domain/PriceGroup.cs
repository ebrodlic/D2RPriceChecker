using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Core.Traderie.Domain
{
    public class PriceGroup
    {
        public int GroupId { get; set; }
        public List<Price> Prices { get; set; } = new();
        public string Display => string.Join(" + ", Prices.Select(p => $"{p.Quantity}x {p.Name}"));
    }
}
