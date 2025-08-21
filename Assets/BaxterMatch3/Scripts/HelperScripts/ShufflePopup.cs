

using Internal.Scripts;
using Internal.Scripts.GUI.Boost;
using UnityEngine;

namespace HelperScripts
{
    public class ShufflePopup : MonoBehaviour
    {
        [SerializeField] private GameObject shuffleSpine;

        private void Start()
        {
            MainManager.Instance.DragBlocked = true;
        }

        public void ButtonFunc()
        {
            MainManager.Instance.dragBlocked = true;
            Debug.Log("Shuffle button clicked");
            MainManager.Instance.lastTouchedItem = null;
            if (!MainManager.Instance.Falling)
            {
                MainManager.Instance.StartCoroutine(MainManager.Instance.Shuffle());
                Instantiate(shuffleSpine);
                MainManager.Instance.ActivatedBoost = BoostType.Empty;
                //Close();
            }
        }

        public void Close()
        {
            // LevelManager.THIS.DragBlocked = false;
            MainManager.Instance.ActivatedBoost = BoostType.Empty;
            //MainManager.Instance.OnCooldownUpdate?.Invoke(false, 0, BoostType.Shuffle);
            Destroy(gameObject);
        }
    }
}