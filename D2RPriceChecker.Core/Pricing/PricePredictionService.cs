using D2RPriceChecker.Core.Traderie.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace D2RPriceChecker.Core.Pricing
{
    public class PricePredictionService
    {
        private RuneValueTable _runeValueTable;

        private readonly PriceConverter _priceConverter;
        private readonly SimilarityCalculator _similarityCalculator;
        private readonly RecencyWeighter _recencyWeighter;
        public PricePredictionService(RuneValueTable table)
        {
            _runeValueTable = table;

            _priceConverter = new PriceConverter(_runeValueTable);
            _similarityCalculator = new SimilarityCalculator();
            _recencyWeighter = new RecencyWeighter();

        }
        public double Predict(List<string> itemText, List<Trade> trades)
        {
            double weightedSum = 0;
            double weightTotal = 0;

            foreach (var trade in trades)
            {
                double price = _priceConverter.Convert(trade);
                double similarity = _similarityCalculator.Compute(trade, itemText);
                double recency = _recencyWeighter.GetWeight(trade);

                double weight = Math.Pow(similarity, 2) * recency;

                System.Diagnostics.Debug.WriteLine(
                    $"Trade: {trade.ItemName} | Price={price} | Sim={similarity} | Rec={recency} | Weight={weight}"
                );

                weightedSum += price * weight;
                weightTotal += weight;
            }

            return weightTotal > 0 ? weightedSum / weightTotal : 0;
        }
    }
}
