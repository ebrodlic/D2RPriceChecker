using D2RPriceChecker.Core.Items;
using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.UI.Util
{
    public static class ItemTextNormalizer
    {
        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return input
                .ToLowerInvariant()
                .Replace("'", "")
                .Replace("-", " ")
                .Replace(",", " ")
                .Replace(".", " ")
                .Replace("(", " ")
                .Replace(")", " ")
                .Trim();
        }

        public static string[] Tokenize(string input)
        {
            return Normalize(input)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
