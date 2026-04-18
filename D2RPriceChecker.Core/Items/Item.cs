using System;
using System.Collections.Generic;
using System.Text;

namespace D2RPriceChecker.Core.Items
{
    public class Item
    {
        public string Name { get; set; } = "";
        public string BaseName { get; set; } = "";
        public ItemRarity Rarity { get; set; }
        public ItemType Type { get; set; }
        public bool IsEthereal { get; set; }
        public bool IsSocketed { get; set; }
        public int? Sockets { get; set; }
        public List<string> Attributes { get; set; } = new();
    }
}
