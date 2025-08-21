using System;
using System.Collections.Generic;
using System.Linq;
using Internal.Scripts;
using Internal.Scripts.Blocks;
using Internal.Scripts.Items;
using Internal.Scripts.Level;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Internal.Scripts.System.Combiner
{
    /// <summary>
    /// Combine manager
    /// </summary>
    public class CombinationManager
    {
        // List of combines for match-3 type combinations
        List<CombineClass> match3Combines = new List<CombineClass>();
        // List of combines used for prediction
        List<CombineClass> tempCombinesPredict = new List<CombineClass>();

        // Dictionaries to keep track of combines associated with items
        Dictionary<Item, CombineClass> dic = new Dictionary<Item, CombineClass>();
        Dictionary<Item, CombineClass> dicPredict = new Dictionary<Item, CombineClass>();

        private ItemsTypes prioritiseItem;
        private int maxCols;
        private int maxRows;
        bool vChecking; // Flag to check by vertical
        private PowerUpCombine _powerUpCombine;
        public Action<List<CombineClass>> onCombined;

        public CombinationManager()
        {
            _powerUpCombine = new PowerUpCombine();
        }

        public List<List<Item>> GetCombinedItems(FieldBoard field, bool setNextItemType = false)
        {
            var combinedItems = new List<List<Item>>();

            var combines = GetCombines(field);
            foreach (var cmb in combines)
            {
                if (cmb.nextType != ItemsTypes.NONE)
                {
                    var item = cmb.items[Random.Range(0, cmb.items.Count)];

                    var draggedItem = MainManager.Instance.lastDraggedItem;
                    if (draggedItem)
                    {
                        if (draggedItem.color != item.color)
                            draggedItem = MainManager.Instance.lastSwitchedItem;
                        // Check the dragged item found in this combine or not and change this type
                        if (cmb.items.IndexOf(draggedItem) >= 0)
                        {
                            item = draggedItem;
                            //Debug.Log($" CombineManager: Dragged item found in combine: {draggedItem}");
                        }
                    }
                    if (setNextItemType)
                    {
                        item.NextType = cmb.nextType;
                        //Debug.Log($" CombineManager: Set next item type for item at position {item} to {cmb.nextType}");
                    }
                }
                if (cmb.items != null && cmb.items.Count > 0)
                {
                    combinedItems.Add(cmb.items);
                    //Debug.Log($" CombineManager: Added combined items with count {cmb.items.Count}");
                }
            }
            return combinedItems;
        }

        public List<CombineClass> GetCombines(FieldBoard field, ItemsTypes _prioritiseItem = ItemsTypes.NONE)
        {
            List<CombineClass> allFoundCombines;
            return GetCombines(field, out allFoundCombines, _prioritiseItem);
        }

        public List<CombineClass> GetCombines(FieldBoard field, out List<CombineClass> allFoundCombines, ItemsTypes _prioritiseItem = ItemsTypes.NONE)
        {
            prioritiseItem = _prioritiseItem;
            maxCols = field.fieldData.maxCols;
            maxRows = field.fieldData.maxRows;
            match3Combines.Clear();
            tempCombinesPredict.Clear();

            dic.Clear();
            var color = -1;
            var combine = new CombineClass();

            vChecking = false;
            // Horizontal searching
            for (var row = 0; row < maxRows; row++)
            {
                color = -1;
                for (var col = 0; col < maxCols; col++)
                {
                    var square = field.GetSquare(col, row);
                    if (IsSquareNotNull(square))
                    {
                        CheckMatches(square.Item, color, ref combine);
                        color = square.Item.color;
                       // //Debug.Log($" CombineManager: Horizontal Check: Item at ({col}, {row}) with color {color}");
                    }
                }
            }
            vChecking = true;
            // Vertical searching
            for (var col = 0; col < maxCols; col++)
            {
                color = -1;
                for (var row = 0; row < maxRows; row++)
                {
                    var square = field.GetSquare(col, row);
                    if (IsSquareNotNull(square) && !square.Item.falling && !square.Item.destroying)
                    {
                        CheckMatches(square.Item, color, ref combine);
                        color = square.Item.color;
                       // //Debug.Log($" CombineManager: Vertical Check: Item at ({col}, {row}) with color {color}");
                    }
                }
            }

            allFoundCombines = match3Combines;
            return CheckCombines(dic, match3Combines);
        }
        public List<CombineClass> CheckCombines(Dictionary<Item, CombineClass> d, List<CombineClass> foundCombines)
        {
            var combines = new List<CombineClass>();
            prioritiseItem = ItemsTypes.NONE;

            foreach (var comb in foundCombines)
            {
                if (comb.items.Any())
                {
                    TemplateOfItem[] foundBonusCombine;
                    comb.nextType = SetNextItemType(comb, out foundBonusCombine);
                    if (comb.nextType != ItemsTypes.NONE)
                    {
                        comb.items = foundBonusCombine.Where(i => i.item).Select(i => i.itemRef).ToList();
                        combines.Add(comb);
                      //  //Debug.Log($" CombineManager: Added combine with next type {comb.nextType}");
                    }
                    else if (IsCombineMatchThree(comb))
                    {
                        // Determine if the combine is horizontal or vertical and add accordingly
                        if (comb.hCount > comb.vCount)
                        {
                            var items = comb.items.GroupBy(i => i.square.row).OrderByDescending(i => i.Count()).First().ToList();
                            comb.items = items;
                            if (items.Count >= 3)
                            {
                                combines.Add(comb);
                              //  //Debug.Log($" CombineManager: Horizontal combine items count: {items.Count}");
                            }
                        }
                        else if (comb.hCount < comb.vCount)
                        {
                            var items = comb.items.GroupBy(i => i.square.col).OrderByDescending(i => i.Count()).First().ToList();
                            comb.items = items;
                            if (items.Count >= 3)
                            {
                                combines.Add(comb);
                              //  //Debug.Log($" CombineManager: Vertical combine items count: {items.Count}");
                            }
                        }
                        else
                        {
                            var items = comb.items.GroupBy(i => i.square.row).OrderByDescending(i => i.Count()).First().ToList();
                            comb.items = items;
                            if (items.Count < 3)
                            {
                                items = comb.items.GroupBy(i => i.square.col).OrderByDescending(i => i.Count()).First().ToList();
                                comb.items = items;
                            }
                            if (items.Count >= 3)
                            {
                                combines.Add(comb);
                               // //Debug.Log($" CombineManager: Mixed combine items count: {items.Count}");
                            }
                        }
                    }
                }
            }

            // Optional: Here you can further process to avoid conflicting or overlapping matches
            // For now, just return the list of valid combines
            return combines;
        }


        public List<CombineClass> FindBonusCombines(FieldBoard field, ItemsTypes itemsTypes = ItemsTypes.NONE)
        {
            return _powerUpCombine.FindBonusCombine(field, itemsTypes);
        }

        CombineClass MergeCombines(CombineClass comb1, CombineClass comb2)
        {
            // This function is now not used in CheckCombines to avoid merging
            var combine = new CombineClass();
            combine.hCount = comb1.hCount + comb2.hCount - 1;
            combine.vCount = comb1.vCount + comb2.vCount - 1;
            combine.items.AddRange(comb1.items);
            combine.items.AddRange(comb2.items);
            combine.itemPositions.AddRange(combine.items.Select(i => new Vector2(i.square.col, i.square.row)));
            TemplateOfItem[] foundBonusCombine;
            combine.nextType = SetNextItemType(combine, out foundBonusCombine);
            //Debug.Log($" CombineManager: Merged combine hCount: {combine.hCount}, vCount: {combine.vCount}");
            return combine;
        }
        ItemsTypes SetNextItemType(CombineClass combineClass, out TemplateOfItem[] foundBonusCombine)
        {
            foundBonusCombine = null;
            var itemTemplates = _powerUpCombine.ConvertCombine(combineClass);
            var nextType = _powerUpCombine.GetBonusCombine(_powerUpCombine.SimplifyArray(itemTemplates).matrix.ToArray(), out foundBonusCombine, prioritiseItem);
            //Debug.Log($" CombineManager: Set next item type: {nextType}");
            return nextType;
        }

        public void CheckMatches(Item item, int color, ref CombineClass combineClass)
        {
            if (!item.destroying && !item.falling)
            {
                combineClass = FindCombine(item);
                AddItemToCombine(combineClass, item, dic, match3Combines);
                //Debug.Log($" CombineManager: Checked match for item at ({item.square.col}, {item.square.row}) with color {item.color}");
            }
        }

        void AddItemToCombine(CombineClass combineClass, Item item, Dictionary<Item, CombineClass> d, List<CombineClass> lCombines)
        {
            combineClass.AddingItem = item;
            d[item] = d.TryGetValue(item, out CombineClass combine1) ? MergeCombines(combine1, combineClass) : combineClass;

            if (IsCombineMatchThree(combineClass))
            {
                combineClass.color = item.color;
                if (lCombines.IndexOf(combineClass) < 0)
                {
                    lCombines.Add(combineClass);
                   // //Debug.Log($" CombineManager: Added combine with item {item} to list.");
                }
            }
        }

        bool IsCombineMatchThree(CombineClass combineClass)
        {
            if (combineClass.hCount > 2 || combineClass.vCount > 2)
            {
               // //Debug.Log($" CombineManager: Combine matches three: hCount={combine.hCount}, vCount={combine.vCount}");
                return true;
            }
            return false;
        }

        bool IsSquareNotNull(Rectangle rectangle)
        {
            bool notNull = rectangle != null && rectangle.Item != null;
            if (!notNull); //Debug.Log("Square is null or item is null.");
            return notNull;
        }

        public CombineClass FindCombine(Item item)
        {
            CombineClass combineClass = null;
            var leftItem = item.GetLeftItem();
            if (CheckColor(item, leftItem) && !vChecking)
            {
                combineClass = FindCombineInDic(leftItem, dic);
                if (combineClass != null)
                {
                  //  //Debug.Log($" CombineManager: Found combine with left item at ({leftItem.square.col}, {leftItem.square.row})");
                    return combineClass;
                }
            }
            var topItem = item.GetTopItem();
            if (CheckColor(item, topItem) && vChecking)
            {
                combineClass = FindCombineInDic(topItem, dic);
                if (combineClass != null)
                {
                    //Debug.Log($" CombineManager: Found combine with top item at ({topItem.square.col}, {topItem.square.row})");
                    return combineClass;
                }
            }

            return new CombineClass();
        }

        CombineClass FindCombineInDic(Item item, Dictionary<Item, CombineClass> d)
        {
            if (d.TryGetValue(item, out CombineClass combine))
            {
                return combine;
            }
            return new CombineClass();
        }

        bool CheckColor(Item item, Item nextItem)
        {
            if (nextItem && nextItem.Combinable && item.Combinable)
            {
                if (nextItem.color == item.color && nextItem.currentType != ItemsTypes.DiscoBall && nextItem.currentType != ItemsTypes.Gredient)
                {
                    return true;
                }
            }
            return false;
        }
    }

    [Serializable]
    public class CombineClass
    {
        private Item addingItem;
        public List<Item> items = new List<Item>();
        public List<Vector2> itemPositions = new List<Vector2>();
        public int vCount;
        public int hCount;
        Vector2 latestItemPositionH = new Vector2(-1, -1);
        Vector2 latestItemPositionV = new Vector2(-1, -1);
        public int color;
        public ItemsTypes nextType;
        public Item triggerItem;
        public Vector2 dir;
        public Item mainSearchItem;

        public Item AddingItem
        {
            get
            {
                return addingItem;
            }

            set
            {
                addingItem = value;
                color = addingItem.color;
                if (CompareColumns(addingItem))
                {
                    if ((int)latestItemPositionH.y != addingItem.square.row && latestItemPositionH.y > -1)
                        hCount = 0;
                    hCount++;
                    latestItemPositionH = new Vector2(addingItem.square.col, addingItem.square.row);

                }
                else if (CompareRows(addingItem))
                {
                    if ((int)latestItemPositionV.x != addingItem.square.col && latestItemPositionV.x > -1)
                        vCount = 0;
                    vCount++;
                    latestItemPositionV = new Vector2(addingItem.square.col, addingItem.square.row);

                }
                if (hCount > 0 && vCount == 0)
                {
                    vCount = 1;
                }
                items.Add(addingItem);
                itemPositions.Add(new Vector2(addingItem.square.col, addingItem.square.row));
                //Debug.Log($" CombineManager: Adding item to combine: ({addingItem.square.col}, {addingItem.square.row}) with hCount={hCount} and vCount={vCount}");
            }

        }

        public void CorrectCombine()
        {
            // Group items by column and count the number of groups
            // Count unique columns for horizontal groups
            var uniqueCols = new HashSet<int>();
            foreach (var item in items)
            {
                uniqueCols.Add(item.square.col);
            }
            hCount = uniqueCols.Count;

            // Count unique rows for vertical groups
            var uniqueRows = new HashSet<int>();
            foreach (var item in items)
            {
                uniqueRows.Add(item.square.row);
            }
            vCount = uniqueRows.Count;

            // If horizontal equals vertical and both greater than 0
            if (hCount == vCount && hCount > 0)
            {
                // Count items in each row to find maximum
                var rowCounts = new Dictionary<int, int>();
                foreach (var item in items)
                {
                    if (!rowCounts.ContainsKey(item.square.row))
                        rowCounts[item.square.row] = 0;
                    rowCounts[item.square.row]++;
                }
                hCount = 0;
                foreach (var count in rowCounts.Values)
                {
                    if (count > hCount) hCount = count;
                }

                // Count items in each column to find maximum
                var colCounts = new Dictionary<int, int>();
                foreach (var item in items)
                {
                    if (!colCounts.ContainsKey(item.square.col))
                        colCounts[item.square.col] = 0;
                    colCounts[item.square.col]++;
                }
                vCount = 0;
                foreach (var count in colCounts.Values)
                {
                    if (count > vCount) vCount = count;
                }
            }

            // Log the corrected counts for debugging purposes
            //Debug.Log($"CombineManager: CombineManager: Corrected combine hCount={hCount}, vCount={vCount}");
        }

        public int GetHorizontalCombinedCount()
        {
            // Group items by row, select the count of items in each group, and return the maximum count
            var maxCount = items.GroupBy(i => i.square.row)
                                .Select(group => new { Count = group.Count(), Item = group })
                                .Max(group => group.Count);

            // Log the maximum count found
            //Debug.Log($"CombineManager: GetHorizontalCombinedCount: Maximum horizontal combined count = {maxCount}");

            return maxCount;
        }

        bool CompareRows(Item item)
        {
            // Check if there are items to compare with
            if (items.Count > 0)
            {
                // Compare the row of the given item with the previous item's row
                bool result = item.square.row != PreviousItem().square.row;
                //Debug.Log($"CombineManager: CompareRows: Comparing item row {item.square.row} with previous item row {PreviousItem().square.row}. Result = {result}");
                return result; // Return the comparison result
            }
            else
            {
                //Debug.Log("CombineManager: CompareRows: No items to compare with. Returning true.");
                return true; // No items to compare with, so return true
            }
        }

        bool CompareColumns(Item item)
        {
            // Check if there are items to compare with
            if (items.Count > 0)
            {
                // Compare the column of the given item with the previous item's column
                bool result = item.square.col != PreviousItem().square.col;
                //Debug.Log($"CombineManager: CompareColumns: Comparing item column {item.square.col} with previous item column {PreviousItem().square.col}. Result = {result}");
                return result; // Return the comparison result
            }
            else
            {
                //Debug.Log("CombineManager: CompareColumns: No items to compare with. Returning true.");
                return true; // No items to compare with, so return true
            }
        }

        Item PreviousItem()
        {
            // Return the last item in the list
            var previousItem = items[items.Count - 1];
            //Debug.Log($"CombineManager: PreviousItem: Returning the last item with row {previousItem.square.row} and column {previousItem.square.col}");
            return previousItem;
        }

        public CombineClass ConvertToCombine(List<Item> list)
        {
            // If the provided list is null, return the current instance
            if (list == null)
            {
                //Debug.Log("CombineManager: ConvertToCombine: Provided list is null. Returning current instance.");
                return this;
            }

            // Add each item in the provided list to the AddingItem property
            foreach (var item in list)
            {
                AddingItem = item;
                //Debug.Log($"CombineManager: ConvertToCombine: Added item with row {item.square.row} and column {item.square.col} to AddingItem.");
            }

            // Correct the combination based on updated item list
            CorrectCombine();

            // Return the current instance after conversion
            //Debug.Log("CombineManager: ConvertToCombine: Conversion completed.");
            return this;
        }

    }
}