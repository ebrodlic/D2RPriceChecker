using D2RPriceChecker.Core.Traderie;
using D2RPriceChecker.Core.Traderie.DTO;
using D2RPriceChecker.Core.Traderie.Mapping;
using D2RPriceChecker.Core.Traderie.Domain;
using D2RPriceChecker.Pipelines;
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
        public async Task<TradeStatistics> GetPriceStatisticsAsync(string name, ItemMetadata metadata)
        {
            (string itemId, _) = await ResolveItemAsync(name);

            var url = BuildPriceUrl(itemId);

            var json = await _window.RunFetchAsync(url, true);

            var dto = JsonSerializer.Deserialize<TradeStatisticsDto>(json, _options)!;

            return TradeStatisticsMapper.Map(dto);
        }     

        public async Task<List<Trade>> GetTradesDataAsync(string name, ItemMetadata metadata)
        {
            var (itemId, _) = await ResolveItemAsync(name);

            var url = BuildOffersUrl(itemId, _window.Session.UserId);

            var json = await _window.RunFetchAsync(url, true);
            var offers = OffersMapper.ParseOffers(json);

            // Define Price Groups
            OffersPostProcessor.Process(offers);

            return offers;
        }

        private async Task<(string itemId, string slug)> ResolveItemAsync(string name)
        {
            var encoded = Uri.EscapeDataString(name);

            var searchUrl =
                $"https://traderie.com/api/diablo2resurrected/items?variants=&search={encoded}&tags=true";

            var searchJson = await _window.RunFetchAsync(searchUrl, true);

            using var doc = JsonDocument.Parse(searchJson);

            var items = doc.RootElement.GetProperty("items");

            if (items.GetArrayLength() == 0)
                return (string.Empty, string.Empty);

            var itemId = items[0].GetProperty("id").GetString();
            var itemSlug = items[0].GetProperty("slug").GetString();

            return (itemId, itemSlug);
        }

        private string BuildPriceUrl(string itemId)
        {
            // PROPS
            var platform = "PC";
            var mode = "softcore";
            var ladder = "true";
            var version = "reign of the warlock";

            var limit = 100;

            //prop_Game%20version=reign%20of%20the%20warlock"

            var pricesUrl = $"https://traderie.com/api/diablo2resurrected/items/price-check" +
                            $"?item={itemId}&limit={limit}" +
                            $"&prop_Ladder={ladder}&prop_Platform={platform}&prop_Mode={mode}";

            return pricesUrl;
        }
        private string BuildOffersUrl(string itemId, string userId)
        {
            // PROPS
            var platform = "PC";
            var mode = "softcore";
            var ladder = "true";
            var version = "reign of the warlock";

            var page = 0;

            var offersUrl = $"https://traderie.com/api/diablo2resurrected/offers" +
                            $"?accepted=true&currBuyer={userId}&completed=true&page={page}&properties=true" +
                            $"&prop_Platform={platform}&prop_Mode={mode}&prop_Ladder={ladder}&item={itemId}";

            return offersUrl;
        }
    }
}
