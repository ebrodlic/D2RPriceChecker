using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using D2RPriceChecker.Core.Pipelines;

namespace D2RPriceChecker.Core.Items
{
    public class ItemAnalysisPipeline
    {
        private readonly IItemBaseNameProvider _itemBaseNameProvider;

        public ItemAnalysisPipeline(IItemBaseNameProvider itemBaseNameProvider)
        {
            _itemBaseNameProvider = itemBaseNameProvider;
        }


        public Item Run(List<string> itemText, TooltipLineSegmentationPipelineResult segmentationResult)
        {
            var item = new Item();

            item.Name = itemText[0];

            // TODO: Optimize - run single loop
            bool hasSuperior = itemText.Any(x => x.Contains("Superior"));
            bool hasInferior = itemText.Any(x => x.Contains("Low Quality"));
            bool hasEthereal = itemText.Any(x => x.Contains("Ethereal"));
            bool hasSocketed = itemText.Any(x => x.Contains("Socketed"));
            
            bool isEquipment = itemText.Any(x => x.Contains("Required"));

            var firstImage = segmentationResult.TooltipLines[0];
            var visualClass = new ItemVisualClassDetector().Detect(firstImage);


            var rarity = visualClass switch
            {
                ItemVisualClass.Blue => ItemRarity.Magic,
                ItemVisualClass.Yellow => ItemRarity.Rare,
                ItemVisualClass.Green => ItemRarity.Set,
                ItemVisualClass.Tan => ItemRarity.Unique,

                ItemVisualClass.White when hasSuperior => ItemRarity.Superior,
                ItemVisualClass.White when hasInferior => ItemRarity.Inferior,

                ItemVisualClass.Gray when hasEthereal || hasSocketed
                    => ItemRarity.EtherealOrSocketed,

                ItemVisualClass.White => ItemRarity.Normal,

                _ => ItemRarity.None
            };
      
            if (isEquipment)
            {
                item.Type = ItemType.Equipment;
            }

            if (rarity == ItemRarity.Superior || rarity == ItemRarity.Inferior)
            {
                item.BaseName = NormalizeItemName(item.Name);
            }

            if (rarity == ItemRarity.Magic)
            {
                var matcher = new ItemBaseMatcher(_itemBaseNameProvider);

                item.BaseName = matcher.Find(item.Name);
            }

            if(rarity == ItemRarity.Rare)
            {
                item.BaseName = itemText[1];
            }

            if (visualClass == ItemVisualClass.Gold)
            {
                item.Type = ItemType.Rune;
            }


            item.IsSocketed = hasSocketed;
            item.IsEthereal = hasEthereal;
            item.Rarity = rarity;

            return item;           
        }

        public static string NormalizeItemName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            name = name.Trim();

            string[] prefixes =
           {
            "Superior ",
            "Low Quality "
            };

            foreach (var prefix in prefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(prefix.Length).Trim();
                    break; // only one prefix expected
                }
            }

            return name;
        }
    }
}
