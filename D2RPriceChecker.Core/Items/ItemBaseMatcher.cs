using D2RPriceChecker.UI.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Core.Items
{
    public class ItemBaseMatcher
    {
        private readonly IItemBaseNameProvider _provider;

        public ItemBaseMatcher(IItemBaseNameProvider provider)
        {
            _provider = provider;
        }

        public string? Find(string itemName)
        {
            var itemTokens = ItemTextNormalizer.Tokenize(itemName);

            BaseNameEntry? best = null;
            int bestScore = 0;

            foreach (var entry in _provider.GetAllBaseNames())
            {
                int score = entry.Tokens.Count(t => itemTokens.Contains(t));

                if (score == entry.Tokens.Length)
                    return entry.Original;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = entry;
                }
            }

            return best?.Original;
        }
    }
    
}
