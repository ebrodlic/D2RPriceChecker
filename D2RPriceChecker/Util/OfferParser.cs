using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace D2RPriceChecker.Util
{
    public static class OfferParser
    {
        public static List<string> ExtractOfferPrices(string json)
        {
            var results = new List<string>();

            using JsonDocument doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("offers", out JsonElement offers))
                return results;

            foreach (var offer in offers.EnumerateArray())
            {
                // Skip if "prices" is null or missing
                if (!offer.TryGetProperty("prices", out JsonElement prices) ||
                    prices.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var price in prices.EnumerateArray())
                {
                    // Extract quantity
                    int quantity = price.TryGetProperty("quantity", out JsonElement qtyEl)
                        ? qtyEl.GetInt32()
                        : 0;

                    // Extract name
                    string name = price.TryGetProperty("name", out JsonElement nameEl)
                        ? nameEl.GetString()
                        : string.Empty;

                    if (!string.IsNullOrEmpty(name))
                    {
                        results.Add($"{quantity} {name}");
                    }
                }
            }

            return results;
        }
    }
}
