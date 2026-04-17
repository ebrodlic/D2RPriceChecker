using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Core.Traderie.Domain
{
    public class Price
    {
        public int Quantity { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int Group {  get; set; }
    }
}
