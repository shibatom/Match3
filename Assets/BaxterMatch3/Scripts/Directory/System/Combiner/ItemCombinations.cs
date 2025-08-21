

using System;
using System.Collections.Generic;
using Internal.Scripts.Items;
using UnityEngine;

namespace Internal.Scripts.System.Combiner
{
    /// <summary>
    /// Item combine editor component
    /// </summary>
    public class ItemCombinations : ItemMonoBehaviour
    {
        public ItemsTypes itemType;
        public int priority;

        public static int maxCols = 5;
        public static int maxRows = 5;
        [HideInInspector]
        [SerializeField]
        public List<TemplatesOfItem> matrix = new List<TemplatesOfItem>();
        public void Init()
        {
            if (matrix.Count == 0)
            {
                Debug.Log("init");
                AddItem();
                for (var col = 0; col < maxCols; col++)
                {
                    for (var row = 0; row < maxRows; row++)
                    {
                        matrix[0].items[row * maxCols + col] = GetItemTemplate(col, row, matrix[0].items);
                    }
                }
            }
        }

        public void AddItem()
        {
            // ItemTemplate[] items = new ItemTemplate[maxCols * maxRows];
            // items = FillMatrix(items);
            matrix.Add(new TemplatesOfItem());
        }

        public void RemoveItem()
        {
            matrix.RemoveAt(matrix.Count - 1);
        }

        int GetPositionArray(int col, int row)
        {
            return row * maxCols + col;
        }

        public TemplateOfItem GetItemTemplate(int col, int row, TemplateOfItem[] currentMatrix)
        {
            var itemTemplate = currentMatrix[GetPositionArray(col, row)];
            if (itemTemplate != null)
                itemTemplate.position = new Vector2(col, row);
            else itemTemplate = new TemplateOfItem(new Vector2(col, row), false);
            return itemTemplate;
        }

        [Serializable]
        public class TemplatesOfItem
        {
            public TemplateOfItem[] items = new TemplateOfItem[maxCols * maxRows];
        }

    }
}