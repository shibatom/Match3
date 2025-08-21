

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Internal.Scripts.Level;
using Internal.Scripts.TargetScripts.TargetSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Target icons GUI handler. Appears on the top game panel 
    /// </summary>
    public class TargetGUIGroup : MonoBehaviour
    {
        public List<TargetGUI> list = new List<TargetGUI>();
        public TextMeshProUGUI description;

        private HorizontalLayoutGroup _group;


        private void OnEnable()
        {
            DisableImages();
            StartCoroutine(WaitForTarget());
            MainManager.OnLevelLoaded += OnLevelLoaded;

            if (MainManager.Instance && MainManager.GetGameStatus() > GameState.PrepareGame)
                OnLevelLoaded();
        }

        private void DisableImages()
        {
            ClearTargets();
            description.gameObject.SetActive(false);
            foreach (var item in list)
            {
                item.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            MainManager.OnLevelLoaded -= OnLevelLoaded;
        }

        private void OnLevelLoaded()
        {
            _group = GetComponent<HorizontalLayoutGroup>();
            if (_group != null)
            {
                if (LevelData.THIS.IsTargetByNameExist("TargetType2"))
                {
                    _group.spacing = 50; /*description.gameObject.SetActive(true);*/
                }
                else
                {
                    _group.spacing = 0; /*description.gameObject.SetActive(false);*/
                }
            }
            //levelLoaded = true;
        }

        IEnumerator WaitForTarget()
        {
            if (MainManager.Instance == null)
                yield return null;
            yield return new WaitUntil(() => MainManager.Instance.levelLoaded);
            yield return new WaitUntil(() => MainManager.Instance.levelData.GetTargetSprites().Length > 0);

            ClearTargets();
            SetTargets();
        }

        void SetTargets()
        {
            LevelData levelData = MainManager.Instance.levelData;
            SetDescription(MainManager.Instance.levelData.GetFirstTarget(true)?.GetDescription());
            var targets = levelData.GetTargetContainersForUI();
            if (transform.parent.parent.parent.name == "PreFailedPanel")
            {
                targets = levelData.GetTargetCounters().Where(i => !i.IsTotalTargetReached()).ToArray();
            }

            for (var i = 0; i < targets.Length; i++)
            {
                var subTargetContainer = targets[i];
                list[i].SetSprite((Sprite)targets[i].extraObject);
                list[i].gameObject.SetActive(true);
                list[i].BindTargetGUI(subTargetContainer);
            }
        }

        private void SetDescription(string descr)
        {
            description.text = descr;
            /*if (descr != "")
            {
                // description.gameObject.SetActive(true);
                hg.padding.left = 58;
                hg.padding.right = 63;
            }*/
        }

        void ClearTargets()
        {
            //hg.padding.left = 10;
            //hg.padding.right = 10;

            description.gameObject.SetActive(false);
            // for (var i = 1; i < list.Count; i++)
            // {
            // Destroy(list[i].gameObject);
            // list.Remove(list[i]);
            // }
        }

        private void SetPadding()
        {
            /*if (list.Count == 2)
            {
                hg.padding.left = 150;
                hg.padding.right = 150;
            }*/
        }
    }
}