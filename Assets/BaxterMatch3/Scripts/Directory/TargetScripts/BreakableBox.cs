

using System.Linq;
using Internal.Scripts.System;
using Internal.Scripts.Blocks;
using Internal.Scripts.Effects;
using Internal.Scripts.Level;
using Internal.Scripts.System.Pool;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace Internal.Scripts.TargetScripts
{
    /// <summary>
    /// Solid blocks target
    /// </summary>
    public class BreakableBox : Target
    {
        // Calculates the total number of solid blocks for the entire level
        public override int CountTarget()
        {
            return GetDestinationCount();
        }

        // Calculates the number of solid blocks for a sublevel
        public override int CountTargetSublevel()
        {
            return GetDestinationCountSublevel();
        }

        // Initializes the target based on the level data
        public override void InitTarget(LevelData levelData)
        {
            subTargetContainers[0].count = levelData.fields.Sum(x => x.levelSquares.Count(i => i.block == LevelTargetTypes.BreakableBox || i.obstacle == LevelTargetTypes.BreakableBox));
        }

        // Calculates the number of solid blocks for a sublevel
        public override int GetDestinationCountSublevel()
        {
            var count = 0;
            var field = MainManager.Instance.field;
            count += field.CountSquaresByType(GetType().Name.ToString());
            return count;
        }

        // Calculates the total number of solid blocks for the entire level
        public override int GetDestinationCount()
        {
            var count = 0;
            var fieldBoards = MainManager.Instance.fieldBoards;
            foreach (var item in fieldBoards)
            {
                // count += item.CountSquaresByType(this.GetType().ToString());
                count += item.GetTargetObjects().Count();
            }

            return count;
        }

        // Handles the destruction of solid blocks when matched
        public override void FulfillTarget<T>(T[] _items)
        {
            if (_items.TryGetElement(0)?.GetType() != typeof(Rectangle)) return;
            var items = _items as Rectangle[];
            var grassList = items?.Where(i => i.type.ToString() == GetType().Name.ToString());
            var pos = TargetGUI.GetTargetGUIPosition(LevelData.THIS.GetFirstTarget(true).name);
            foreach (var grassBlock in grassList)
            {
                Rectangle grassBlockSubRectangle = grassBlock.GetSubSquare();
                Vector2 scale = grassBlockSubRectangle.transform.localScale;
                var targetContainer = subTargetContainers.Where(i => grassBlock.type.ToString().Contains(i.targetPrefab.name)).FirstOrDefault();
                amount++;
                var itemAnim = new GameObject();
                //var animComp = itemAnim.AddComponent<AnimateItems>();
                //LevelManager.THIS.animateItems.Add(animComp);
                // square.DestroyBlock();
                Debug.Log("destroyBlok");
                var partcl2 = ObjectPoolManager.Instance.GetPooledObject("FireworkSplash2");
                if (partcl2 != null)
                {
                    partcl2.transform.position = grassBlockSubRectangle.transform.position;
                    SplashEffectParticles splashEffectParticles = partcl2.GetComponent<SplashEffectParticles>();
                    splashEffectParticles.SetColor(0);
                    splashEffectParticles.RandomizeParticleSeed();
                }
            }
        }

        // Checks if the total target count has been reached
        public override void DestroyEvent(GameObject obj)
        {
            Debug.Log("sallog DestroyEvent" + obj);
        }

        // Checks if the target count for a sublevel has been reached
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