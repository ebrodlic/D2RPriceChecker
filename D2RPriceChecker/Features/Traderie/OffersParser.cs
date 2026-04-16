using D2RPriceChecker.Features.Traderie.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace D2RPriceChecker.Features.Traderie
{
    public class OffersParser
    {
        public static List<Trade> ParseOffers(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var root = JsonSerializer.Deserialize<JsonElement>(json, options);
            var offers = root.GetProperty("offers").EnumerateArray();

            var trades = new List<Trade>();

            foreach (var offer in offers)
            {
                var trade = new Trade();

                ParseListing(offer, trade);
                ParsePrices(offer, trade);
                ParseProperties(offer, trade);
                ParseMetadata(offer, trade);

                trades.Add(trade);
            }

            return trades;
        }

        #region Listing (item + amount)

        private static void ParseListing(JsonElement offer, Trade trade)
        {
            if (!offer.TryGetProperty("listing", out var listing))
                return;

            if (listing.TryGetProperty("item", out var item))
            {
                trade.ItemName = item.GetProperty("name").GetString() ?? "";
            }

            if (listing.TryGetProperty("amount", out var amount))
            {
                trade.Amount = amount.ValueKind == JsonValueKind.Number
                    ? amount.GetInt32()
                    : 1;
            }
        }

        #endregion

        #region Prices

        private static void ParsePrices(JsonElement offer, Trade trade)
        {
            // 1. direct offer.prices
            if (offer.TryGetProperty("prices", out var prices) &&
                prices.ValueKind == JsonValueKind.Array)
            {
                ExtractPrices(prices, trade);
                return;
            }

            // 2. fallback listing.prices
            if (offer.TryGetProperty("listing", out var listing) &&
                listing.TryGetProperty("prices", out var listingPrices) &&
                listingPrices.ValueKind == JsonValueKind.Array)
            {
                ExtractPrices(listingPrices, trade);
            }
        }

        private static void ExtractPrices(JsonElement pricesArray, Trade trade)
        {
            foreach (var price in pricesArray.EnumerateArray())
            {
                trade.Prices.Add(new Price
                {
                    Name = price.GetProperty("name").GetString() ?? "",
                    Quantity = price.GetProperty("quantity").GetInt32(),
                    Type = price.GetProperty("type").GetString() ?? "",
                    Group = price.GetProperty("group").GetInt32()

                });
            }
        }

        #endregion

        #region Properties

        private static void ParseProperties(JsonElement offer, Trade trade)
        {
            if (!offer.TryGetProperty("listing", out var listing))
                return;

            Debug.WriteLine("--- LISTING RAW ---");
            Debug.WriteLine(listing.ToString());

            // OPTIONAL ENRICHMENT: properties
            if (listing.TryGetProperty("properties", out var props) &&
                props.ValueKind == JsonValueKind.Array)
            {
                Debug.WriteLine("PROPERTIES FOUND");

                foreach (var prop in props.EnumerateArray())
                {
                    trade.Properties.Add(ParseProperty(prop));
                }

                // build derived data ONLY if properties exist
                trade.Attributes = trade.Properties
                    .Where(p => p.Type == "number" && p.Number.HasValue)
                    .Select(p => p.Property.Replace("{{value}}", p.Number.Value.ToString()))
                    .ToList();

                trade.Labels = trade.Properties
                    .Where(p => p.Type == "string" && !string.IsNullOrEmpty(p.String))
                    .Select(p => p.String!)
                    .Concat(
                        trade.Properties
                            .Where(p => p.Type == "bool")
                            .Select(p => p.Property)
                    )
                    .ToList();
            }
            else
            {
                Debug.WriteLine("NO PROPERTIES FIELD");

                // ensure safe empty lists (important for UI binding safety)
                trade.Attributes = new List<string>();
                trade.Labels = new List<string>();
            }
        }

        private static ListingProperty ParseProperty(JsonElement prop)
        {
            var result = new ListingProperty
            {
                Type = prop.GetProperty("type").GetString() ?? "",
                Property = prop.GetProperty("property").GetString() ?? ""
            };

            if (prop.TryGetProperty("number", out var number) &&
                number.ValueKind == JsonValueKind.Number)
            {
                result.Number = number.GetInt32();
            }

            if (prop.TryGetProperty("string", out var str) &&
                str.ValueKind == JsonValueKind.String)
            {
                result.String = str.GetString();
            }

            if (prop.TryGetProperty("bool", out var b))
            {
                result.Bool = b.ValueKind == JsonValueKind.True || b.ValueKind == JsonValueKind.False
                    ? b.GetBoolean()
                    : (bool?)null;
            }

            if (prop.TryGetProperty("options", out var options) &&
                options.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();

                foreach (var opt in options.EnumerateArray())
                {
                    var val = opt.GetString();
                    if (val != null)
                        list.Add(val);
                }
            }

            return result;
        }

        #endregion

        #region Metadata

        private static void ParseMetadata(JsonElement offer, Trade trade)
        {
            if (offer.TryGetProperty("updated_at", out var updated))
            {
                trade.UpdatedAt = updated.GetDateTime();
            }
        }

        #endregion
    }
}