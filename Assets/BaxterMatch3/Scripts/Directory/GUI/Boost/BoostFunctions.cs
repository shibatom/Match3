

using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Internal.Scripts.Items;
using Internal.Scripts.Blocks;
using Internal.Scripts.TargetScripts.TargetSystem;

namespace Internal.Scripts.GUI.Boost
{
    /// <summary>
    /// Boost animation events and effects
    /// </summary>
    public class BoostFunctions : MonoBehaviour
    {
        [FormerlySerializedAs("square")] public Rectangle rectangle;
        public Animator animator;

        public HelperScripts.StripeEffect effect;

        private void Start()
        {
            MainManager.CanUseBoost = false;
        }

        public void ShowEffect()
        {
            MainManager.Instance.OnCooldownUpdate?.Invoke(true, 0f, BoostType.Bomb);
            var partcl = Instantiate(Resources.Load("Prefabs/Effects/Firework"), transform.position, Quaternion.identity) as GameObject;
            var main = partcl.GetComponent<ParticleSystem>().main;
            //        main.startColor = LevelManager.THIS.scoresColors[square.Item.color];
            if (name.Contains("area_explosion"))
                main.startColor = Color.white;
            Destroy(partcl, 1f);

            if (name.Contains("area_explosion"))
            {
                var p = Instantiate(Resources.Load("Prefabs/Effects/CircleExpl"), transform.position, Quaternion.identity) as GameObject;
                Destroy(p, 0.4f);
            }
        }

        public void OnStart()
        {
            MainManager.Instance.StopedAI();
            // effect.startDestroy = true;
            MainManager.Instance.ArrowEffect(rectangle, true);
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.arrowHitEffect);
            MainManager.Instance.OnCooldownUpdate?.Invoke(true, 0f, BoostType.Arrow);
        }

        public void OnShuffleStart()
        {
            MainManager.Instance.StopedAI();
            // effect.startDestroy = true;
            MainManager.Instance.ArrowEffect(rectangle, true);
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.shuffleEffect);
            MainManager.Instance.OnCooldownUpdate?.Invoke(true, 0f, BoostType.Arrow);
        }

        public void OnCanonStart()
        {
            // LevelManager.THIS.ArrowEffect(square, false);
            MainManager.Instance.StopedAI();
            effect.startDestroy = true;
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.cannonHitEffect);
            MainManager.Instance.OnCooldownUpdate?.Invoke(true, 0f, BoostType.Canon);
        }

        public void OnHammerHit()
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.hammerHitEffect);
            bool spreadTarget = MainManager.Instance.levelData.TargetCounters.Any(i => i.collectingAction == CollectingTypes.Spread);
            Debug.Log("Spread Target: " + spreadTarget);
            Debug.Log("Processing Bomb boost...");
            // Check if square is not null before accessing its Item property
            if (rectangle != null)
            {
                Debug.Log("Square is not null, checking Item...");
                if (rectangle.Item != null)
                {
                    Debug.Log("Destroying item for Bomb: " + rectangle.Item);
                    if (spreadTarget) rectangle.SetType(LevelTargetTypes.ExtraTargetType2, 1, LevelTargetTypes.NONE, 1);
                    rectangle.Item.DestroyItem(true);
                }
                else
                {
                    Debug.Log("Item is null for square: " + rectangle);
                    rectangle.DestroyBlock();
                }
            }
            else
            {
                Debug.Log("Square is null, cannot destroy item.");
            }

            MainManager.Instance.OnCooldownUpdate?.Invoke(false, 0.5f, BoostType.Bomb); // Trigger cooldown update
        }

        public void OnFinished(BoostType boostType)
        {
            Debug.Log("OnFinished called with BoostType: " + boostType);

            MainManager.CanUseBoost = true;

            bool spreadTarget = MainManager.Instance.levelData.TargetCounters.Any(i => i.collectingAction == CollectingTypes.Spread);
            Debug.Log("Spread Target: " + spreadTarget);

            if (boostType == BoostType.ExplodeArea)
            {
                Debug.Log("Processing ExplodeArea boost...");
                var list = MainManager.Instance.GetItemsAroundSquare(rectangle);
                Debug.Log("Items around square: " + list.Count);

                if (!MainManager.Instance.AdditionalSettings.MulticolorDestroyByBoostAndChopper)
                    list = list.Where(i => i.currentType != ItemsTypes.DiscoBall).ToList();

                var squares = list.Select(i => i.square);
                if (spreadTarget)
                    MainManager.Instance.levelData.GetTargetObject().CheckSquares(squares.ToArray());

                foreach (var item in list)
                {
                    if (item != null)
                    {
                        Debug.Log("Destroying item: " + item);
                        item.DestroyItem(true);
                    }
                    else
                    {
                        Debug.Log("Item is null, destroying block for square: " + item?.square);
                        if (item != null && item.square != null)
                            item.square.DestroyBlock();
                    }
                }
            }
            else if (boostType == BoostType.Bomb)
            {
            }

            Debug.Log("Starting FindMatchDelay coroutine.");
            MainManager.Instance.StartCoroutine(MainManager.Instance.FindMatchDelay());

            Debug.Log("Destroying current gameObject.");
            Destroy(gameObject);
            MainManager.Instance.OnCooldownUpdate?.Invoke(false, 0.5f, boostType); // Trigger cooldown update
            Debug.Log("update text on finished invoked" + boostType);
        }
    }
}