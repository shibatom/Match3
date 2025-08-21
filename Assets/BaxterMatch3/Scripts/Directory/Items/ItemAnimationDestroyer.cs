using UnityEngine;
using Internal.Scripts.Blocks;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Package destroy animation helper
    /// </summary>
    public class ItemAnimationDestroyer : MonoBehaviour
    {
        private Item item;
        private bool started;

        private void Start()
        {
            item = GetComponent<Item>();
        }

        public void DestroyPackage(Item item1, bool isPackageCombined = false)
        {
            if (started) return;
            started = true;
            // var thisItem = GetComponent<Item>();

            //  GameObject go = Instantiate(Resources.Load("Prefabs/Effects/_ExplosionAround") as GameObject);//LevelManager.THIS.GetExplAroundPool();
            //  go.gameObject.transform.SetParent(transform, false);
            // if (go != null)
            // {
            //     go.transform.position = transform.position;
            //     var explosionAround = go.GetComponent<ExplAround>();
            //     explosionAround.item = thisItem;
            //     go.SetActive(true);
            // }

            var square = item1.square;
            square.Item = item1;

            //item.anim.enabled = true;
            //var audioBinding = item.director.playableAsset.outputs.Select(i => i.sourceObject).OfType<AudioTrack>().FirstOrDefault();
            //item.director.SetGenericBinding(audioBinding, SoundBase.Instance);
            //item.director.Play();
            OnPackageAnimationFinished(item1, isPackageCombined);
        }

        public void OnPackageAnimationFinished(Item item1, bool isBombCombined)
        {
            var package = item1.GetTopItemInterface().GetGameobject().GetComponent<BombItem>();

            if (package != null)
            {
                package.PackageAnimator.gameObject.SetActive(false);
            }

            var square = item1.square;
            DestroyItems(item1, square, isBombCombined, package.isCombinedwithMulti);
            MainManager.Instance.EndBusyOperation();

            item1.HideSprites(true);


            item1.DestroyBehaviour();
            started = false;
        }

        private void DestroyItems(Item item1, Rectangle rectangle, bool isPackageCombined = false, bool isCombinedWithDisco = false)
        {
            MainManager.Instance.field.DestroyItemsAround(rectangle, item, isPackageCombined, isCombinedWithDisco);

            rectangle.DestroyBlock();

            item1.destroying = false;
            item.square.Item = null;
        }
    }
}