

using System.Collections;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Target icon handler on the map
    /// </summary>
    public class FirstTargetIcon : MonoBehaviour
    {
        private int _num;
        public Sprite[] targetSprite;
        private TargetContainer _tar;
        private LIMIT _limitType;

        private void OnEnable()
        {
            StartCoroutine(LoadTarget());
        }

        private IEnumerator LoadTarget()
        {
            _num = int.Parse(transform.parent.name.Replace("Level", ""));
            //LoadLevel(_num);
            yield return new WaitForSeconds(0.1f);
            if (_limitType == LIMIT.TIME)
                GetComponent<SpriteRenderer>().sprite = targetSprite[4];
        }
    }
}