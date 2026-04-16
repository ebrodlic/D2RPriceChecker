using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Features.Traderie.Model
{
    public class Price
    {
        public int Quantity { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int Group {  get; set; }
    }

    public class PriceGroup
    {
        public int GroupId {  get; set; }
        public List<Price> Prices { get; set; } = new();

        public string Display => string.Join(" + ", Prices.Select(p => $"{p.Quantity}x {p.Name}"));
    }
}
