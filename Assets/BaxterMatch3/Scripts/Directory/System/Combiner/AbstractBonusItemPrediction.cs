

using System.Collections.Generic;
using System.Linq;
using Internal.Scripts;
using Internal.Scripts.Items;
using Internal.Scripts.Level;

namespace Internal.Scripts.System.Combiner
{
    /// <summary>
    /// Bonus combine prediction for tutorial
    /// </summary>
    public abstract class AbstractBonusItemPrediction
    {

        public static TemplateOfItem[] strippedCombine = new TemplateOfItem[25];

        public static List<Item> IsItemPredicted(ItemsTypes itemType)
        {
            List<Item> predicted;
            var field = MainManager.Instance.field;
            predicted = PridictCombines(field, true, itemType);
            if (!predicted.Any())
                predicted = PridictCombines(field, false, itemType);

            return predicted;
        }

        private static List<Item> PridictCombines(FieldBoard field, bool right, ItemsTypes itemType)
        {
            var combineManager = MainManager.Instance.CombineManager;

            for (var i = 0; i < field.squaresArray.Count(); i++)
            {
                var item = field.squaresArray[i].Item;
                Item item1 = null;
                if (right)
                    item1 = (item?.square?.GetNeighborRight())?.Item;
                else
                    item1 = (item?.square?.GetNeighborBottom())?.Item;

                if (item1 != null && !item.destroying && !item1.destroying)
                {
                    var color = item.color;
                    var color1 = item1.color;

                    item.color = color1;
                    item1.color = color;

                    var combines = combineManager.GetCombines(field, itemType);
                    item.color = color;
                    item1.color = color1;

                    var combine = combines.Find(x => GetConditionByType(x, itemType));
                    if (combine != null)
                    {
                        if (item.color == combine.color)
                        {
                            ArtificialIntelligence.Instance.TipItem = item;
                            ArtificialIntelligence.Instance.vDirection = (item1.transform.position - item.transform.position).normalized;
                        }
                        else
                        {
                            ArtificialIntelligence.Instance.TipItem = item1;
                            ArtificialIntelligence.Instance.vDirection = (item.transform.position - item1.transform.position).normalized;
                        }
                        combine.items.Add(item);
                        combine.items.Add(item1);
                        return combine.items;
                    }

                }
            }

            return new List<Item>();

        }

        private static bool GetConditionByType(CombineClass x, ItemsTypes itemType)
        {
            if (itemType == ItemsTypes.RocketHorizontal || itemType == ItemsTypes.RocketVertical)
                return x.nextType == ItemsTypes.RocketHorizontal || x.nextType == ItemsTypes.RocketVertical;
            return x.nextType == itemType;
        }

    }
}