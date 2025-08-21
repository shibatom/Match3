using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace StorySystem
{
    public class DialogueScroller : MonoBehaviour
    {
        public GameObject lineHolderPrefab;
        public GameObject content;
        private RectTransform contentRect;
        public SpeakerUIController speaker;
        GameObject lineHolder;

        public int poolSize = 10; // Initial size of the pool

        private List<GameObject> objectPool = new List<GameObject>();

        private GameObject lastInstantiatedObject;
        //private GameObject secondToLastInstantiatedObject;


        public DialogueScroller ins;

        private void Awake()
        {
            ins = this;
        }

        private void Start()
        {
            contentRect = content.transform.GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            //contentRect.anchoredPosition = Vector3.zero;
        }

        public void InstansiateLineHolder(SpeakerUIController speaker)
        {
            if (lastInstantiatedObject != null)
            {
                float scrollMove = lastInstantiatedObject.transform.GetComponent<RectTransform>().sizeDelta.y;

                lastInstantiatedObject.transform.GetComponentInChildren<TextMeshProUGUI>().DOFade(0.3f, 0.3f);
                lastInstantiatedObject.transform.GetChild(0).GetComponentInChildren<Image>().DOFade(0.3f, 0.3f);
                //contentRect.DOLocalMoveY(contentRect.localPosition.y + scrollMove + 25, 0.3f);
                /*if (secondToLastInstantiatedObject != null)
                {
                    secondToLastInstantiatedObject.transform.GetComponentInChildren<TextMeshProUGUI>().DOFade(0.0f, 0.3f);
                    secondToLastInstantiatedObject.transform.GetChild(0).GetComponentInChildren<Image>().DOFade(0, 0.3f);
                }*/
            }

            lineHolder = Instantiate(speaker.lineHolderPrefab);

            lineHolder.transform.SetParent(content.transform);

            lineHolder.transform.localScale = new Vector3(1f, 1f, 1f);

            lineHolder.SetActive(true);

            Debug.Log(speaker.DialogHolder);
            speaker.DialogHolder =
                lineHolder.transform.Find("GameObject").Find("Text (TMP)").GetComponent<TextMeshProUGUI>();

            GameObject newObj = lineHolder;

            //secondToLastInstantiatedObject = lastInstantiatedObject;
            Destroy(lastInstantiatedObject);

            lastInstantiatedObject = newObj;
        }

        public void DestroyAllChildren()
        {
            contentRect.anchoredPosition = Vector3.zero;

            // Iterate through each child of the parent
            foreach (Transform child in content.transform)
            {
                // Destroy the child GameObject
                Destroy(child.gameObject);
            }


            lastInstantiatedObject = null;
            //secondToLastInstantiatedObject = null;
        }
    }
}