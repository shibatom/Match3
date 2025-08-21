

using System.Collections.Generic;
using System.Linq;
using Internal.Scripts;
using Internal.Scripts.Blocks;

namespace Internal.Scripts.Effects
{
    /// <summary>
    /// Cloud animation effect for levels with not only down direction
    /// </summary>
    public static class MovingCloudEffect
    {
        public static void SetGroupSquares(Rectangle[] squaresArray)
        {
            var groups = new List<List<Rectangle>>();


            foreach (var square in squaresArray)
            {
                // groups = square.SetGroupSquares(groups);
                groups = square.GetGroupsSquare(groups);
            }

            groups.RemoveAll(i => i.Count() < MainManager.Instance.field.enterPoints);
            foreach (var group in groups)
            {
                foreach (var square in group)
                {
                    square.squaresGroup = group;
                }
            }

            var list = squaresArray.Where(i => i.squaresGroup.Count < MainManager.Instance.field.enterPoints);
            groups.Clear();
            foreach (var square in list)
            {
                // groups = square.SetGroupSquaresRest(list, groups);
                groups = square.GetGroupsSquare(groups, null, false);
            }

            foreach (var group in groups)
            {
                foreach (var square in group)
                {
                    square.squaresGroup = group;
                }
            }
        }
    }
}