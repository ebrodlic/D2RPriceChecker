using D2RPriceChecker.Core.Traderie.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Core.Pricing
{
    public class PriceConverter
    {
        private readonly RuneValueTable _runeValueTable;

        public PriceConverter(RuneValueTable runeValueTable)
        {
            _runeValueTable = runeValueTable;
        }

        public double Convert(Trade trade)
        {
            var values = new List<double>();

            foreach (var group in trade.PriceGroups)
            {
                if (!IsValidGroup(group))
                    continue;

                values.Add(ConvertPriceGroup(group));
            }

            return values.Count > 0 ? values.Average() : 0;
        }

        private double ConvertPriceGroup(PriceGroup group)
        {
            double total = 0;

            foreach (var price in group.Prices)
                total += _runeValueTable.GetValue(price.Name) * price.Quantity;

            return total;
        }

        private bool IsValidGroup(PriceGroup group)
        {
            foreach (var price in group.Prices)
            {
                if (!_runeValueTable.HasValue(price.Name))
                    return false;
            }

            return true;
        }
    }
}
