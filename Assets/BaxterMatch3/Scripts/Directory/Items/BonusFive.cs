

using UnityEngine;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// +5 seconds bonus item
    /// </summary>
    public class BonusFive : MonoBehaviour
    {
        public GameObject sprite;
        public new Animation animation;

        public void Destroy()
        {
            transform.parent = null;
            sprite.GetComponent<BindSortSetter>().enabled = false;
            animation.clip.legacy = true;
            animation.Play();
            Destroy(gameObject, 1);
            if (MainManager.Instance.gameStatus == GameState.Playing)
                MainManager.Instance.levelData.limit += 5;
        }
    }
}