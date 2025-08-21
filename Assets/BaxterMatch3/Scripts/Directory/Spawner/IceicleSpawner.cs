

using System.Collections;
using System.Collections.Generic;
using Internal.Scripts.Blocks;
using Internal.Scripts.Items;
using UnityEngine;

namespace Internal.Scripts.Spawner
{
    public class IceicleSpawner : Rectangle
    {
        private int _lastMoveID = -1;
        private Rectangle _neghbourRectangle;
        string SpawnerKey = "";

        void OnEnable()
        {
            GetComponent<SpriteRenderer>().sortingOrder = 5;
            SpawnerKey = MainManager.Instance.SubScribeIcicleSpread();
            _neghbourRectangle = this;
            MainManager.OnTurnEnd += OnTurnEnd;
            MainManager.Instance.thrivingBlockMachine = true;
        }

        void OnDisable()
        {
            MainManager.Instance.UnSubscribeIcicleSpread(SpawnerKey);
            MainManager.OnTurnEnd -= OnTurnEnd;
            MainManager.Instance.thrivingBlockDestroyed = true;
            MainManager.Instance.thrivingBlockMachine = false;
        }

        private void OnTurnEnd()
        {
            if(SpawnerKey == "1")
            {
                MainManager.Instance.GenerateRandom();
            }

            if (MainManager.Instance.Getint.ToString() != SpawnerKey) return;

            if (MainManager.Instance.moveID == _lastMoveID) return;
            _lastMoveID = MainManager.Instance.moveID;
            if (MainManager.Instance.thrivingBlockDestroyed /*|| blockCreated*/ || field != MainManager.Instance.field) return;
            MainManager.Instance.thrivingBlockDestroyed = false;
            List<Rectangle> sqList = new List<Rectangle>();
            if (_neghbourRectangle == this || (MainManager.Instance.IfanythrivingBlock[SpawnerKey].Count == 0))
            {
                sqList = this.mainRectangle.GetAllNeghborsCross();
            }
            else
            {
                var res = TargetSquare();
                if(res != null)
                    _neghbourRectangle = res;
                else
                {
                    Debug.Log("Got Null");
                }
                sqList = _neghbourRectangle.GetAllNeghborsCross();
            }
            foreach (var sq in sqList)
            {
                if (!sq.CanGoInto() || Random.Range(0, 1) != 0 ||
                    sq.type != LevelTargetTypes.EmptySquare || sq.Item?.currentType != ItemsTypes.NONE) continue;
                if (sq.Item == null) continue;
                var SampleType = sq.Item.currentType;
                if (SampleType == ItemsTypes.Gredient || SampleType == ItemsTypes.Eggs  || SampleType == ItemsTypes.TimeBomb ) continue;
                sq.CreateObstacle(LevelTargetTypes.GrowingGrass, 1);
                var th = sq.GetComponentInChildren<MultiplyingBlock>();
                if (th != null)
                {
                    th.RegisterToIcicleSpawner(SpawnerKey);
                }
                //blockCreated = true;
                StartCoroutine(blockCreatedCD());
                _neghbourRectangle = sq;

                Destroy(sq.Item.gameObject);
                sq.item = null;
                break;
            }
        }

        private Rectangle TargetSquare()
        {
            Rectangle res = null;

            foreach (var item in MainManager.Instance.IfanythrivingBlock[SpawnerKey])
            {
                foreach (var item1 in item.mainRectangle.GetAllNeghborsCross())
                {
                    if(item1.Item != null && item1.Item.currentType == ItemsTypes.NONE)
                    {
                        res = item.mainRectangle;
                        break;
                    }
                }
            }


            return res;
        }

        IEnumerator blockCreatedCD()
        {
            yield return new WaitForSeconds(1);
            //blockCreated = false;
        }
    }
}   