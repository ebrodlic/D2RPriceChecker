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

        public async Task<string> GetPriceDataAsync(ItemMetadata metadata, List<string> text)
        {
            var userId = _window.Session.UserId;

            if (metadata.Rarity != ItemRarity.Unique &&
                metadata.Rarity != ItemRarity.Set)
                return string.Empty;

            var itemName = text[0].Trim();
            var encoded = Uri.EscapeDataString(itemName);

            var searchUrl =
                $"https://traderie.com/api/diablo2resurrected/items?variants=&search={encoded}&tags=true";

            var searchJson = await _window.RunFetchAsync(searchUrl, true);

            using var doc = JsonDocument.Parse(searchJson);

            var items = doc.RootElement.GetProperty("items");
            if (items.GetArrayLength() == 0)
                return string.Empty;

            var itemId = items[0].GetProperty("id").GetString();

            var offersUrl =
                $"https://traderie.com/api/diablo2resurrected/offers?accepted=true&currBuyer={userId}&completed=true&item={itemId}";

            return await _window.RunFetchAsync(offersUrl, true);
        }

    }
}
