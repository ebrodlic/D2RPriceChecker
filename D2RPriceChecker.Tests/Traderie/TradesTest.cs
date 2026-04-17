using D2RPriceChecker.Core.Traderie.Domain;
using D2RPriceChecker.Core.Traderie.Mapping;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace D2RPriceChecker.Tests.Traderie
{
    public class TradesTest
    {
        [Fact]
        public void TestBasicParsing()
        {
            var offersJson = File.ReadAllText("Data/recent-trades.json");
            var offers = OffersMapper.ParseOffers(offersJson);  

            Assert.NotEmpty(offers);
            Assert.Equal(20, offers.Count);
        }

        [Fact]
        public void TestPriceGrouping()
        {
            var offersJson = File.ReadAllText("Data/recent-trades.json");
            var offers = OffersMapper.ParseOffers(offersJson);

            // Define Price Groups
            OffersPostProcessor.Process(offers);

            Assert.NotEmpty(offers);

            var offer = offers[2];

            Assert.NotEmpty(offer.PriceGroups);
            Assert.Equal(2, offer.PriceGroups.Count);
        }

        [Fact]
        public void SecondGroupShouldContainUmAndMal()
        {
            var offersJson = File.ReadAllText("Data/recent-trades.json");
            var offers = OffersMapper.ParseOffers(offersJson);

            // Define Price Groups
            OffersPostProcessor.Process(offers);

            Assert.NotEmpty(offers);

            var offer = offers[2];

            Assert.NotEmpty(offer.PriceGroups);
            Assert.Equal(2, offer.PriceGroups.Count);

            var secondGroup = offer.PriceGroups[1];

            var runeNames = secondGroup.Prices
                  .Select(p => p.Name)
                  .ToList();

            Assert.Contains("Um Rune", runeNames);
            Assert.Contains("Mal Rune", runeNames);
        }
    }
}
