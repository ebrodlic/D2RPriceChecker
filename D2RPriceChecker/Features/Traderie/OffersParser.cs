using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace D2RPriceChecker.Features.Traderie
{
    public class Price
    {
        public int Quantity { get; set; }
        public string Name { get; set; } = "";
    }

    public class Trade
    {
        public List<Price> Prices { get; set; } = new();
    }

    public class OffersParser
    {
        public static List<Trade> ParseOffers(string json)
        {
            // Deserialize the root JSON as a list of offers
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var root = JsonSerializer.Deserialize<JsonElement>(json, options);
            var offers = root.GetProperty("offers").EnumerateArray();

            var trades = new List<Trade>();

            //if (offers == null) return trades;

            foreach (var offer in offers)
            {
                var trade = new Trade();

                // First, try to get offer.prices
                if (offer.TryGetProperty("prices", out var pricesElement) && pricesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var price in pricesElement.EnumerateArray())
                    {
                        trade.Prices.Add(new Price
                        {
                            Name = price.GetProperty("name").GetString() ?? "",
                            Quantity = price.GetProperty("quantity").GetInt32()
                        });
                    }
                }

                // If offer.prices is null or empty, fallback to listing.prices
                if (trade.Prices.Count == 0 && offer.TryGetProperty("listing", out var listingElement))
                {
                    if (listingElement.TryGetProperty("prices", out var listingPrices) && listingPrices.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var price in listingPrices.EnumerateArray())
                        {
                            trade.Prices.Add(new Price
                            {
                                Name = price.GetProperty("name").GetString() ?? "",
                                Quantity = price.GetProperty("quantity").GetInt32()
                            });
                        }
                    }
                }

                trades.Add(trade);
            }

            return trades;
        }
    }
}
