

using System.Collections;
using System.Linq;
using Internal.Scripts;
using Internal.Scripts.Items;
using Internal.Scripts.System.Pool;
using UnityEngine;

namespace Internal.Scripts.System
{
    public class ItemMonoBehaviour : MonoBehaviour
    {
        //private bool quit;

        protected virtual void Start()
        {
            // Debug.Log(gameObject.name + " " + gameObject.GetInstanceID() + " created ");
        }

        public void DestroyBehaviour()
        {
            Debug.Log("DestroyBehaviour");
            var item = GetComponent<Item>();
            if (item == null || !gameObject.activeSelf) return;
            DestroyDelay(item);
        }

        public static int finishBonusCounter;

        void DestroyDelay(Item item)
        {
            bool changeTypeFinished=false;
            if (item.NextType != ItemsTypes.NONE)
            {
               // if(item.currentType != ItemsTypes.PACKAGE)
                   // yield return new WaitWhile(() => item.falling);
                if(MainManager.Instance.gameStatus == GameState.PreWinAnimations)
                {
                    finishBonusCounter++;
                    if (finishBonusCounter >= 5) item.NextType = item.NextType;
                }
               // if(LevelManager.THIS.gameStatus != GameState.PreWinAnimations || (LevelManager.THIS.gameStatus == GameState.PreWinAnimations && finishBonusCounter <5))
                {
                    item.ChangeType((x) => { changeTypeFinished = true; });
                 //   yield return new WaitUntil(() => changeTypeFinished);
                }
            }

            if (MainManager.Instance.DebugSettings.DestroyLog)
                DebugLogManager.Log(name + " dontDestroyOnThisMove " + item.dontDestroyOnThisMove + " dontDestroyForThisCombine " + gameObject.GetComponent<Item>()
                                       .dontDestroyForThisCombine,  DebugLogManager.LogType.Destroying);
            if (item.dontDestroyOnThisMove || gameObject.GetComponent<Item>().dontDestroyForThisCombine)
            {
                GetComponent<Item>().StopDestroy();
                return ;
            }
            if (MainManager.Instance.DebugSettings.DestroyLog)
                DebugLogManager.Log(gameObject.GetInstanceID() + " destroyed " + item.name + " " + item.GetInstanceID(), DebugLogManager.LogType.Destroying);
            OnDestroyItem(item);
            ObjectPoolManager.Instance.PutBack(gameObject);
           // yield return new WaitForSeconds(0);
        }

        public void OnDestroyItem(Item item)
        {
//        if (item.square && item == item.square.Item)
//            item.square.Item = null;
            item.square = null;
            item.field.squaresArray.Where(i => i.Item == item).ForEachY(i => i.Item = null);
            item.previousSquare = null;
            item.tutorialItem = false;
            item.NextType = ItemsTypes.NONE;
            if(transform.childCount>0)
            {
                transform.GetChild(0).transform.localScale = Vector3.one;
                transform.GetChild(0).transform.localPosition = Vector3.zero;
            }
        }

        void OnApplicationQuit()
        {
            //quit = true;
        }

        void OnDestroy()
        {
            // if (!quit) Debug.Log(gameObject.name + " " + gameObject.GetInstanceID() + " OnDestroyed ");
        }
    }
}