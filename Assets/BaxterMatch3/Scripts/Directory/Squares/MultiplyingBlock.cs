

using System.Collections;
using Internal.Scripts;
using Internal.Scripts.Items;
using UnityEngine;

namespace Internal.Scripts.Blocks
{
    /// <summary>
    /// block expands constantly until you explode one
    /// </summary>
    public class MultiplyingBlock : Rectangle
    {
        static bool blockCreated;
        int lastMoveID = -1;
        public string Th;
        
        void OnEnable()
        {
            MainManager.OnTurnEnd += OnTurnEnd;
            blockCreated = false;
        }

        //Added_feature
        public void RegisterToIcicleSpawner(string key)
        {
            Th = key;
            if (MainManager.Instance.thrivingBlockMachine)
            {
                if (MainManager.Instance.IfanythrivingBlock.ContainsKey(Th))
                    MainManager.Instance.IfanythrivingBlock[Th].Add(this);

            }
        }
        
        void OnDisable()
        {
            MainManager.OnTurnEnd -= OnTurnEnd;
            MainManager.Instance.thrivingBlockDestroyed = true;
            //Added_feature
            if (MainManager.Instance.thrivingBlockMachine)
            {
                if (MainManager.Instance.IfanythrivingBlock.ContainsKey(Th))
                    MainManager.Instance.IfanythrivingBlock[Th].Remove(this);
            }
        }

        private void OnTurnEnd()
        {
            //Added_feature
            if (!MainManager.Instance.thrivingBlockMachine)
            {
                if (MainManager.Instance.moveID == lastMoveID) return;
                lastMoveID = MainManager.Instance.moveID;
                if (MainManager.Instance.thrivingBlockDestroyed || blockCreated ||
                    field != MainManager.Instance.field) return;
                MainManager.Instance.thrivingBlockDestroyed = false;
                var sqList = this.mainRectangle.GetAllNeghborsCross();
                foreach (var sq in sqList)
                {
                    if (!sq.CanGoInto() || Random.Range(0, 1) != 0 ||
                        sq.type != LevelTargetTypes.EmptySquare || sq.Item?.currentType != ItemsTypes.NONE) continue;
                    if (sq.Item == null) continue;
                    if (sq.Item.currentType != ItemsTypes.NONE) continue;
                    sq.CreateObstacle(LevelTargetTypes.GrowingGrass, 1);
                    blockCreated = true;
                    StartCoroutine(blockCreatedCD());
                    Destroy(sq.Item.gameObject);
                    break;
                }
              // StartCoroutine(AI.THIS.CheckPossibleCombines());
            }
        }

        IEnumerator blockCreatedCD()
        {
            yield return new WaitForSeconds(1);
            blockCreated = false;
        }
    }
}