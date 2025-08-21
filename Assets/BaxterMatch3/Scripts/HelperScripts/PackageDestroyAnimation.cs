using Internal.Scripts;
using Internal.Scripts.Items;
using Internal.Scripts.System.Pool;
using UnityEngine;

namespace HelperScripts
{
    public class PackageDestroyAnimation : MonoBehaviour
    {
        public ItemAnimationDestroyer itemDestroyAnimation;
        public Item item1;
        public BombItem _package;

        public void DestroyIt(int index)
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.bombExplodeEffect);

            //Debug.LogError($"DestroyIt{itemDestroyAnimation==null}{item1==null}");
            if (index == 1)
            {
                GameObject explosion = ObjectPoolManager.Instance.GetPooledObject("BombParticleLight");
                if (explosion != null)
                {
                    explosion.transform.position = transform.position;
                    explosion.SetActive(true);
                    explosion.GetComponent<BackToPool>().StartAnimation();
                }
            }

            itemDestroyAnimation.OnPackageAnimationFinished(item1, index == 1);
        }

        public void SetSortOrder()
        {
            GetComponent<MeshRenderer>().sortingOrder = 50;
        }

        public void AnimationPackageAndStrip()
        {
            _package?._stripedDestroy.Invoke();

            var effect = ObjectPoolManager.Instance.GetPooledObject("BombParticleLight");
            if (effect != null)
            {
                effect.transform.position = transform.position;
                effect.SetActive(true);
                effect.GetComponent<BackToPool>().StartAnimation();
            }
        }
    }
}