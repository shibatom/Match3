using UnityEngine;
using UnityEngine.UI;

namespace StorySystem
{
    public class layoutFitter : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            // LayoutRebuilder.ForceRebuildLayoutImmediate(this.gameObject.GetComponent<RectTransform>());
        }

        // Update is called once per frame
        void Update()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.gameObject.GetComponent<RectTransform>());
        }
    }

}