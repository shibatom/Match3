

using System.Linq;
using Internal.Scripts;
using Internal.Scripts.Blocks;
using Internal.Scripts.Level;
using Internal.Scripts.System;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace Internal.Scripts.TargetScripts
{
    /// <summary>
    /// Grass target
    /// </summary>
    public class Grass : Target
    {
        public override int CountTarget()
        {
            return GetDestinationCount();
        }

        public override int CountTargetSublevel()
        {
            return GetDestinationCountSublevel();
        }
        public override void InitTarget(LevelData levelData)
        {
            subTargetContainers[0].count = levelData.fields.Sum(x => x.levelSquares.Count(i =>  i.block == LevelTargetTypes.Grass));
        }
        public override int GetDestinationCountSublevel()
        {
            var count = 0;
            var field = MainManager.Instance.field;
            count += field.CountSquaresByType(GetType().Name.ToString());
            return count;
        }

        public override int GetDestinationCount()
        {
            var count = 0;
            var fieldBoards = MainManager.Instance.fieldBoards;
            foreach (var item in fieldBoards)
            {
                // count += item.CountSquaresByType(this.GetType().Name.ToString());
                count += item.GetTargetObjects().Count();
            }
            return count;
        }

        public override void FulfillTarget<T>(T[] _items)
        {
            if (_items.TryGetElement(0)?.GetType() != typeof(Rectangle)) return;
            var items = _items as Rectangle[];
            var tempList = items?.Where(i => i.type.ToString() == GetType().Name.ToString());
            var pos = TargetGUI.GetTargetGUIPosition(LevelData.THIS.GetFirstTarget(true).name);
            foreach (var tempBlock in tempList)
            {
                Vector2 scale = tempBlock.subSquares[0].transform.localScale;
                var targetContainer = subTargetContainers.Where(i => tempBlock.type.ToString().Contains(i.targetPrefab.name)).FirstOrDefault();
                amount++;
                var itemAnim = new GameObject();
                var animComp = itemAnim.AddComponent<AnimateItems>();
                MainManager.Instance.animateItems.Add(animComp);
                animComp.InitAnimation(tempBlock.gameObject, pos, scale, () => { targetContainer.changeCount(-1); }, tempBlock.GetSubSquare().GetComponentInChildren<SpriteRenderer>().sprite);
                // square.DestroyBlock();
            }
        }

        public override void DestroyEvent(GameObject obj)
        {
            // Debug.Log(obj);
        }

        public override int GetCount(string spriteName)
        {
            // foreach (var item in subTargetContainers)
            // {
            //     if (item.targetPrefab.GetComponent<SpriteRenderer>()?.sprite.name == spriteName)
            //         return item.GetCount();
            // }

            return CountTarget();
        }

        public override bool IsTotalTargetReached()
        {
            return CountTarget() <= 0;
        }

        public override bool IsTargetReachedSublevel()
        {
            return CountTargetSublevel() <= 0;
        }
    }
}