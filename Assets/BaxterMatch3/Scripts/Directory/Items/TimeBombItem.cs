

using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Internal.Scripts.Blocks;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Time bomb item
    /// </summary>
    public class TimeBombItem : Item, IItemInterface
    {
        [SerializeField] private bool ActivateByExplosion;
        [SerializeField] private bool StaticOnStart;
        public int timer;
        public int startTimer = 5;
        public TextMeshProUGUI timerLabel;
        [SerializeField] public GameObject sprite;
        public GameObject explosion;

        public void Destroy(Item item1, Item item2, bool isPackageCombined = false, bool destroyNeighboours = true, int color = 0, bool isCombinedwithMulti = false)
        {
            GetItem.square.DestroyBlock();
            if (GetItem.square.type == LevelTargetTypes.ExtraTargetType1)
            {
                GetItem.StopDestroy();
            }

            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.explosion);
            Instantiate(explosion, transform.position, Quaternion.identity);
            DestroyBehaviour();
        }

        private new Item GetItem => GetComponentInParent<Item>();

        public void OnEnable()
        {
            // Old Code
            /*
              if (LevelManager.THIS.gameStatus == GameState.PrepareGame)
            {
                startTimer = LevelManager.THIS.field.fieldData.bombTimer;
                timer = startTimer;
            }
             */


            //added_feature
            {
                startTimer = MainManager.Instance.field?.fieldData.bombTimer ?? 15;
                if (startTimer < 10)
                    startTimer = 10;
                timer = startTimer;
                // Debug.Log(timer + " : " + name);
            }


            MainManager.OnTurnEnd += OnTurnEnd;
            InitItem();
        }

        public override void InitItem()
        {
            timerLabel.color = new Color(73f / 255f, 73f / 255f, 73f / 255f);
            // sprite.GetComponent<SpriteRenderer>().sortingOrder = 2;
            GetComponentInChildren<Canvas>().enabled = true;
            sprite.SetActive(true);
            base.InitItem();
            UpdateTimer();
        }

        private void OnDisable()
        {
            MainManager.OnTurnEnd -= OnTurnEnd;
        }

        private void OnTurnEnd()
        {
            timer--;
            // if(timer == 1) Warning();
            if (timer <= 0)
            {
                timer = 0;
                // LevelManager.THIS.gameStatus = GameState.BombFailed;
            }

            UpdateTimer();
        }

        private void Warning()
        {
            timerLabel.color = Color.red;
            var seq = LeanTween.sequence();
            float t = 0.3f;
            seq.append(LeanTween.scale(timerLabel.gameObject, Vector3.one * 1.2f, t));
            seq.append(LeanTween.scale(timerLabel.gameObject, Vector3.one, t));
            seq.append(LeanTween.scale(timerLabel.gameObject, Vector3.one * 1.2f, t));
            seq.append(LeanTween.scale(timerLabel.gameObject, Vector3.one, t));
            CentralSoundManager.Instance.PlayLimitSound(CentralSoundManager.Instance.timeOut);
        }

        public UnityAction OnExlodeAnimationFinished;

        public void ExplodeAnimation(bool hide, UnityAction callback)
        {
            MainManager.Instance.levelData.limit = 0;

            if (callback == null)
                callback = OnExlodeAnimationFinished;
            anim.enabled = false;
            GetComponentInChildren<Canvas>().enabled = false;
            GetComponent<FailedBombAnimation>().BombFailed(Vector3.zero, 10, hide, callback);
        }

        void UpdateTimer()
        {
            timerLabel.text = timer.ToString();
        }

        public GameObject GetGameobject()
        {
            return gameObject;
        }

        public bool IsCombinable()
        {
            return Combinable;
        }

        public bool IsExplodable()
        {
            return ActivateByExplosion;
        }

        public void SetExplodable(bool setExplodable)
        {
            ActivateByExplosion = setExplodable;
        }

        public bool IsStaticOnStart()
        {
            return StaticOnStart;
        }

        public void SetOrder(int i)
        {
            var spriteRenderers = GetSpriteRenderers();
            var orderedEnumerable = spriteRenderers.OrderBy(x => x.sortingOrder).ToArray();
            for (int index = 0; index < orderedEnumerable.Length; index++)
            {
                var spr = orderedEnumerable[index];
                spr.sortingOrder = i + index;
            }
        }

        public Item GetParentItem()
        {
            return transform.GetComponentInParent<Item>();
        }
    }
}