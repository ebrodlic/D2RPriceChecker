using D2RPriceChecker.Core.Pricing;
using D2RPriceChecker.Core.Traderie.DTO;
using D2RPriceChecker.Core.Traderie.Mapping;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace D2RPriceChecker.Tests.Pricing
{
    public class PricingTest
    {
        [Fact]
        public void TestBasicParsing()
        {
            var statsJson = File.ReadAllText("Data/price-statistics.json");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var dto = JsonSerializer.Deserialize<TradeStatisticsDto>(statsJson, options)!;
            var stats = TradeStatisticsMapper.Map(dto);

            Assert.NotEmpty(stats.RuneValues);
        }

        [Fact]
        public void TestMaraPricing()
        {
            var statsJson = File.ReadAllText("Data/price-statistics.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var dto = JsonSerializer.Deserialize<TradeStatisticsDto>(statsJson, options)!;
            var stats = TradeStatisticsMapper.Map(dto);

            Assert.NotEmpty(stats.RuneValues);

            var table = new RuneValueTable(stats.RuneValues);
            var prediction = new PricePredictionService(table);


            var itemText = File.ReadAllText("Data/item-description-mara.txt");
            var lines = itemText.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();
            Assert.NotEmpty(lines);

            var offersJson = File.ReadAllText("Data/recent-trades.json");
            var offers = OffersMapper.ParseOffers(offersJson);

            // Define Price Groups
            OffersPostProcessor.Process(offers);

            Assert.NotEmpty(offers);

            var service = new PricePredictionService(table);
            var result = service.Predict(lines, offers);

            Assert.NotEqual(0, result.Value);
        }
    }
}
