using D2RPriceChecker.Core.Traderie;
using D2RPriceChecker.Core.Traderie.DTO;
using D2RPriceChecker.Core.Traderie.Mapping;
using D2RPriceChecker.Core.Traderie.Domain;
using D2RPriceChecker.Core.Items;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace D2RPriceChecker.UI.Traderie
{
    internal class TraderieService
    {
        private readonly TraderieWindow _window;

        private readonly JsonSerializerOptions _options;
        public TraderieService(TraderieWindow window)
        {
            _window = window;

            _options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };
        }
        public async Task<TradeStatistics> GetPriceStatisticsAsync(Item item)
        {
            (string itemId, _) = await ResolveItemAsync(item);

            var url = BuildPricesUrl(itemId, item);

            var json = await _window.RunFetchAsync(url, true);

            var dto = JsonSerializer.Deserialize<TradeStatisticsDto>(json, _options)!;

            return TradeStatisticsMapper.Map(dto);
        }

        public async Task<List<Trade>> GetTradesDataAsync(Item item)
        {
            var (itemId, _) = await ResolveItemAsync(item);

            var url = BuildOffersUrl(itemId, _window.Session.UserId, item);

            var json = await _window.RunFetchAsync(url, true);
            var offers = OffersMapper.ParseOffers(json);

            // Define Price Groups
            OffersPostProcessor.Process(offers);

            return offers;
        }

        private async Task<(string itemId, string slug)> ResolveItemAsync(Item item)
        {
            var searchUrl = BuildSearchUrl(item);

            var searchJson = await _window.RunFetchAsync(searchUrl, true);

            using var doc = JsonDocument.Parse(searchJson);

            var items = doc.RootElement.GetProperty("items");

            if (items.GetArrayLength() == 0)
                return (string.Empty, string.Empty);

            var itemId = items[0].GetProperty("id").GetString();
            var itemSlug = items[0].GetProperty("slug").GetString();

            return (itemId, itemSlug);
        }

        private bool ShouldUseBaseName(Item item)
        {
            return item.Rarity is
                ItemRarity.Superior or
                ItemRarity.Inferior or
                ItemRarity.Magic or
                ItemRarity.Rare;
        }

        private bool ShouldUseRarityFilter(Item item)
        {
            return item.Rarity is
                ItemRarity.Normal or
                ItemRarity.Superior or
                ItemRarity.Magic or
                ItemRarity.Rare;
        }

        private Dictionary<string, string> BuildItemProperties(Item item)
        {
            var props = new Dictionary<string, string>();

            // Ethereal applies to most base items
            if (item.IsEthereal)
            {
                props["prop_Ethereal"] = "true";
            }
            else
            {
                props["prop_Ethereal"] = "false";
            }

            // Base-item rarities → use rarity param
            if (item.Rarity is ItemRarity.Normal or ItemRarity.Superior or ItemRarity.Magic or ItemRarity.Rare)
            {
                props["prop_Rarity"] = item.Rarity.ToString().ToLower();
            }

            if(item.Rarity is ItemRarity.Unique or ItemRarity.Set)
            {
                //if identified, or unidentified - set flag
            }        

            return props;
        }

        private string BuildSearchUrl(Item item)
        {
            var name = item.Name;

            if (ShouldUseBaseName(item))
                name = item.BaseName;

            var encoded = Uri.EscapeDataString(name);

            var url = $"https://traderie.com/api/diablo2resurrected/items?variants=&search={encoded}&tags=true";

            return url;
        }

        private string BuildPricesUrl(string itemId, Item item)
        {
            // PROPS
            var platform = "PC";
            var mode = "softcore";
            var ladder = "true";
            var version = "reign of the warlock";


            var limit = 100;

            var query = new List<string>
            {
                $"item={itemId}",
                $"limit={limit}",
                $"prop_Ladder={ladder}",
                $"prop_Platform={platform}",
                $"prop_Mode={mode}"
            };

            var props = BuildItemProperties(item);

            foreach (var kv in props)
            {
                query.Add($"{kv.Key}={kv.Value}");
            }

            //prop_Game%20version=reign%20of%20the%20warlock"

            return $"https://traderie.com/api/diablo2resurrected/items/price-check?{string.Join("&", query)}";
        }
        private string BuildOffersUrl(string itemId, string userId, Item item)
        {
            // PROPS
            var platform = "PC";
            var mode = "softcore";
            var ladder = "true";
            var version = "reign of the warlock";

            var page = 0;

            var query = new List<string>
            {
                $"accepted=true",
                $"currBuyer={userId}",
                $"completed=true",
                $"page={page}",
                $"properties=true",
                $"prop_Platform={platform}",
                $"prop_Mode={mode}",
                $"prop_Ladder={ladder}",
                $"item={itemId}"
            };

            var props = BuildItemProperties(item);

            foreach (var kv in props)
            {
                query.Add($"{kv.Key}={kv.Value}");
            }

            return $"https://traderie.com/api/diablo2resurrected/offers?{string.Join("&", query)}";
        }
    }
}
