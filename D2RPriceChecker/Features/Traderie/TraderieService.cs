using D2RPriceChecker.Domain;
using D2RPriceChecker.Features.Traderie.DTO;
using D2RPriceChecker.Features.Traderie.Mapper;
using D2RPriceChecker.Features.Traderie.Model;
using D2RPriceChecker.Pipelines;
using D2RPriceChecker.Views;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace D2RPriceChecker.Features.Traderie
{
    internal class TraderieService
    {
        private readonly TraderieWindow _window;
        public TraderieService(TraderieWindow window)
        {
            _window = window;
        }
        public async Task<TradeStatistics> GetPriceStatisticsAsync(ItemMetadata metadata, string name)
        {
            var pricesJson = await GetPriceStatisticsDataAsync(metadata, name);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var dto = JsonSerializer.Deserialize<TradeStatisticsDto>(pricesJson, options)!;

            return TradeStatisticsMapper.Map(dto);
        }

        public async Task<List<Trade>> GetTradesDataAsync(ItemMetadata metadata, string name)
        {
            var offersJson = await GetOffersDataAsync(metadata, name);
            var offers = OffersParser.ParseOffers(offersJson);

            // Define Price Groups
            OffersPostProcessor.Process(offers);

            return offers;
        }

        private async Task<string> GetPriceStatisticsDataAsync(ItemMetadata metadata, string name)
        {
            var userId = _window.Session.UserId;

            if (metadata.Rarity != ItemRarity.Unique &&
                metadata.Rarity != ItemRarity.Set)
                return string.Empty;

            var encoded = Uri.EscapeDataString(name);

            var searchUrl =
                $"https://traderie.com/api/diablo2resurrected/items?variants=&search={encoded}&tags=true";

            var searchJson = await _window.RunFetchAsync(searchUrl, true);

            using var doc = JsonDocument.Parse(searchJson);

            var items = doc.RootElement.GetProperty("items");
            if (items.GetArrayLength() == 0)
                return string.Empty;

            var itemId = items[0].GetProperty("id").GetString();
            var itemSlug = items[0].GetProperty("slug").GetString();

            var limit = 100;

            var pricesUrl = $"https://traderie.com/api/diablo2resurrected/items/price-check?item={itemId}&limit={limit}&prop_Ladder=true&prop_Game%20version=reign%20of%20the%20warlock";

            return await _window.RunFetchAsync(pricesUrl, true);
        }
 

        private async Task<string> GetOffersDataAsync(ItemMetadata metadata, string name)
        {
            var userId = _window.Session.UserId;

            if (metadata.Rarity != ItemRarity.Unique &&
                metadata.Rarity != ItemRarity.Set)
                return string.Empty;

            var encoded = Uri.EscapeDataString(name);

            var searchUrl =
                $"https://traderie.com/api/diablo2resurrected/items?variants=&search={encoded}&tags=true";

            var searchJson = await _window.RunFetchAsync(searchUrl, true);

            using var doc = JsonDocument.Parse(searchJson);

            var items = doc.RootElement.GetProperty("items");
            if (items.GetArrayLength() == 0)
                return string.Empty;

            var itemId = items[0].GetProperty("id").GetString();
            var itemSlug = items[0].GetProperty("slug").GetString();

            var offersUrl =
                $"https://traderie.com/api/diablo2resurrected/offers?accepted=true&currBuyer={userId}&completed=true&properties=true&prop_Ladder=true&item={itemId}";

            return await _window.RunFetchAsync(offersUrl, true);
        }
    }
}
