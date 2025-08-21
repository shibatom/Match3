

using System.Collections.Generic;
using UnityEngine;
using Internal.Scripts.Blocks;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Color generator, checks available colors
    /// </summary>
    public class ColorGetter : IColorGettable
    {
        public int GenColor(Rectangle rectangle, int maxColors = 6, int exceptColor = -1, bool onlyNONEType = false)
        {
            List<int> exceptColors = new List<int>();
            List<int> remainColors = new List<int>();
            var thisColorLimit = MainManager.Instance.levelData.colorLimit;
            for (int i = 0; i < MainManager.Instance.levelData.colorLimit; i++)
            {
                //bool canGen = true;
                if (rectangle.GetMatchColorAround(i) > 1)
                {
                    exceptColors.Add(i);
                }
            }

            int randColor = 0;
            do
            {
                randColor = Random.Range(0, thisColorLimit);
            } while (exceptColors.Contains(randColor) && exceptColors.Count < thisColorLimit - 1);

            if (remainColors.Count > 0)
                randColor = remainColors[Random.Range(0, remainColors.Count)];
            if (exceptColor == randColor)
                randColor = Mathf.Clamp(randColor++, 0, thisColorLimit);
            return randColor;
        }
    }
}