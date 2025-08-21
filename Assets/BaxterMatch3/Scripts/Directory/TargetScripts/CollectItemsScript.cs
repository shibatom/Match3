

using System.Linq;
using Internal.Scripts;
using Internal.Scripts.Items;
using Internal.Scripts.Items.Interfaces;
using Internal.Scripts.Level;
using Internal.Scripts.System;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace Internal.Scripts.TargetScripts
{
    /// <summary>
    /// collect items target
    /// </summary>
    public class CollectItemsScript : Target
    {
        public override int CountTarget()
        {
             Debug.Log("sallog CollectItems");
            return amount;
        }

        public override int CountTargetSublevel()
        {
            Debug.Log("sallog CollectItems");
            return amount;
        }

        public override void InitTarget(LevelData levelData)
        {
            Debug.Log("sallog CollectItems");
            foreach (var item in subTargetContainers)
            {
                amount += item.GetCount();
            }

        }
         // Calculates the total number of solid blocks for the entire level
        // public override int GetDestinationCount()
        // {
        //     Debug.Log("sallog CollectItems");
        //     var count = 0;
        //     var fieldBoards = LevelManager.THIS.fieldBoards;
        //     foreach (var item in fieldBoards)
        //     {
        //          count += item.CountSquaresByType(this.GetType().ToString());
        //        // count += item.GetTargetObjects().Count();
        //     }
        //     return count;
        // }

        public override void DestroyEvent(GameObject obj)
        {


        }

        public override void FulfillTarget<T>(T[] _items)
        {
            if (_items.Length>0 && _items[0].GetType().BaseType != typeof(Item)) return;
            var items = _items as Item[];
            foreach (var item in subTargetContainers)
            {
                foreach (var obj in items)
                {
                    if (obj == null) continue;
                    var sprite = obj.GetComponent<ColorReciever>().directSpriteRenderer.sprite;
                    if ((Sprite)item.extraObject == sprite && item.preCount > 0)
                    {
                        amount--;
                        item.preCount--;
                        var pos = TargetGUI.GetTargetGUIPosition(obj.GetComponent<ColorReciever>().directSpriteRenderer.sprite.name);
                        var itemAnim = new GameObject();
                        var animComp = itemAnim.AddComponent<AnimateItems>();
                        MainManager.Instance.animateItems.Add(animComp);
                        item.changeCount(-1);
                       // animComp.InitAnimation(obj.gameObject, pos, obj.transform.localScale, () => { item.changeCount(-1); });
                    }
                }
            }
        }

        public override int GetDestinationCount()
        {
            Debug.Log("sallog CollectItems");
            return destAmount;
        }

        public override int GetDestinationCountSublevel()
        {
            Debug.Log("sallog CollectItems");
            return destAmount;
        }

        public override bool IsTargetReachedSublevel()
        {
            Debug.Log("sallog CollectItems");
            return amount <= 0;
        }

        public override bool IsTotalTargetReached()
        {
            Debug.Log("sallog CollectItems");
            return amount <= 0;
        }

        public override int GetCount(string spriteName)
        {
            Debug.Log("sallog CollectItems");
            for (var index = 0; index < subTargetContainers.Length; index++)
            {
                var item = subTargetContainers[index];
                if (item.extraObject.name == spriteName)
                    return item.GetCount();
            }

            return CountTarget();
        }
    }
}