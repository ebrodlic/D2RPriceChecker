using D2RPriceChecker.Core.Items;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace D2RPriceChecker.UI.Util
{
    public class FileItemBaseNameProvider : IItemBaseNameProvider
    {
        private readonly List<BaseNameEntry> _entries;

        public FileItemBaseNameProvider(string filePath)
        {
            _entries = File.ReadAllLines(filePath)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x =>
                {
                    var normalized = ItemTextNormalizer.Normalize(x);

                    return new BaseNameEntry
                    {
                        Original = x,
                        Normalized = normalized,
                        Tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    };
                })
                .ToList();
        }

        public IReadOnlyList<BaseNameEntry> GetAllBaseNames()
            => _entries;
    }
}
