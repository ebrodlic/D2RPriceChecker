using D2RPriceChecker.Core.Traderie.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Core.Pricing
{
    public class RecencyWeighter
    {
        public double GetWeight(Trade trade)
        {
            var days = (DateTime.UtcNow - trade.UpdatedAt).TotalDays;

            return Math.Exp(-0.15 * days);
        }
    }
}
