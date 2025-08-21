

using System.Collections;
using DG.Tweening;
using HelperScripts;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Internal.Scripts.System;
using Image = UnityEngine.UI.Image;

namespace Internal.Scripts.GUI.Boost
{
    /// <summary>
    /// Boost Inventory And GUI
    /// </summary>
    public class BoostInventory : MonoBehaviour
    {
        public TMP_Text boostCountText;

        private int AmountOwned
        {
            get => GlobalValue.GetItem(type);
            set => GlobalValue.SetItem(type, value);
        }

        public BoostType type;
        public GameObject counter;

        public GameObject check;
        // private BoostProduct boostProduct;

        private bool checkOn;
        private GameObject Lock;
        private GameObject Indicator;
        private static BoosterOffer BoostShop;

        public GameObject holder;

        public GameObject dimBack;

        public GameObject holyParticle;


        public Image imageRenderer; // Reference to the SpriteRenderer component

        private bool isRotated = false; // Flag to track rotation state


        public float rotationDuration = 1f;
        public Vector3 targetRotation = new Vector3(0, 0, 180);
        public float rotationDelay = 2f;

        private int currentObjectIndex = 0; // Keeps track of the current object being moved

        public GameObject[] SettingsObj; // Array to store the 4 image GameObjects
        public float moveAmount; // Amount to move each image (e.g., -1f for left)
        public float moveDuration;
        private bool reversed = false; // Flag to track movement direction (forward or reversed)

        public float cooldown = 0.2f; // Define cooldown duration in seconds
        private float lastActivationTime = 0.0f; // Initialize outside the method

        public float extraMoveDuration = 0.5f;
        public Vector3 extraMoveAmount = new Vector3(-20, 0, 0);

        public GameObject backMask;

        [SerializeField] private GameObject shufflePopup;

        private void Awake()
        {
            Lock = transform.Find("Lock")?.gameObject;
            Indicator = transform.Find("Indicator")?.gameObject;

            if (ReferencerUI.Instance != null)
            {
                BoostShop = ReferencerUI.Instance.GetBoostShop();
            }
            //		if (check != null) return;
            //		check = Instantiate(Resources.Load("Prefabs/Check")) as GameObject;
            //		check.transform.SetParent(transform.Find("Indicator"));
            //		check.transform.localScale = Vector3.one;
            //		check.GetComponent<RectTransform>().anchoredPosition = new Vector2(2,-67);
            //		check.SetActive(false);
        }

        public void LOGLOGLOG()
        {
            Debug.LogError(" Button Clicked ");
        }

        private void OnEnable()
        {
            if (name == "Main Camera") return;
            if (MainManager.Instance == null) return;
            //if (LevelManager.THIS.gameStatus == GameState.Map)
            //    check.SetActive(false);
            //if (!LevelManager.This.enableInApps)
            //gameObject.SetActive(false);
            // FindBoostProduct();
            // ShowPlus(BoostCount() <= 0);

            if (boostCountText != null)
            {
                boostCountText.text = AmountOwned == 0 ? "+" : AmountOwned.ToString();
            }

            counter.SetActive(true);
        }


        [UsedImplicitly]
        public void ActivateBoost()
        {
            Debug.Log("cooldown boost activated" + MainManager.Instance.isInCooldown);
            if (!MainManager.CanUseBoost) return; // Check for cooldown

            Debug.Log("ActivateBoost called.");
            Debug.Log($"Initial BoostHand check: LevelManager.THIS.tutorialTime = {MainManager.Instance.tutorialTime}");
            if (MainManager.Instance.tutorialTime) return;

            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);
            Debug.Log("Sound played for boost activation.");


            Debug.Log("uncheck " + checkOn + MainManager.Instance.ActivatedBoost + type);
            if (checkOn || MainManager.Instance.ActivatedBoost == type)
            {
                dimBack.SetActive(false);
                Debug.Log("Boost is already checked or activated. Unchecking.");
                UnCheckBoost();
                return;
            }

            if (MainManager.Instance.isInCooldown) return; // Check for cooldown
            Debug.Log($"IsLocked: {IsLocked()}, checkOn: {checkOn}, GameStatus: {MainManager.Instance.gameStatus}");
            if (IsLocked() || checkOn || (MainManager.Instance.gameStatus != GameState.Playing &&
                                          MainManager.Instance.gameStatus != GameState.Map))
            {
                Debug.Log("Cannot activate boost due to lock, checkOn status, or game state.");
                return;
            }

            Debug.Log($"BoostCount: {BoostCount()}");
            if (BoostCount() > 0)
            {
                Debug.Log($"Boost type: {type}, DragBlocked: {MainManager.Instance.DragBlocked}");
                if (/*type != BoostType.Shuffle &&*/ type != BoostType.DiscoBalls && type != BoostType.Bombs &&
                    type != BoostType.Rockets && type != BoostType.Chopper /*&& !LevelManager.THIS.DragBlocked*/) // for in-game boosts
                {
                    MainManager.Instance.DragBlocked = false;
                    Debug.Log("Activating in-game boost.");
                    DimTheBackForBooster(true);
                    MainManager.Instance.ActivatedBoost = type;
                    var canvas = holder.AddComponent<Canvas>();
                    canvas.overrideSorting = true;
                    canvas.sortingLayerName = "menu";

                    var sortingGroup = MainManager.Instance.Level.AddComponent<SortingGroup>();
                    sortingGroup.sortingLayerName = "menu";
                    sortingGroup.sortingOrder = -5;
                }
                /*else if (type == BoostType.Shuffle)
                {
                    Debug.Log("Activating in-game boost.");
                    Instantiate(shufflePopup, ReferencerUI.Instance.transform);
                    MainManager.Instance.ActivatedBoost = type;
                }*/
                else
                {
                    Debug.Log("Checking boost.");
                    Check(true);
                }
            }
            else
            {
                // Debug.Log($"Boost count is zero. Opening boost shop for product: {boostProduct}.");
                // OpenBoostShop(boostProduct, ActivateBoost);
                OpenBoostShop(type);
            }

            if (boostCountText != null)
            {
                Debug.Log($"Updating boost count display: {BoostCount()}.");
                boostCountText.text = AmountOwned == 0 ? "+" : AmountOwned.ToString();
            }

            Debug.Log($"ShowPlus called with condition: {BoostCount() <= 0}.");
            // ShowPlus(BoostCount() <= 0);
        }


        [UsedImplicitly]
        public void ActivateSetting()
        {
            // Check if cooldown is active
            if (Time.time - lastActivationTime < cooldown)
            {
                return;
            }

            // Update rotation state
            isRotated = !isRotated;

            if (MainManager.Instance.tutorialTime) return;
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);

            // Rotate the sprite based on the current state
            if (isRotated)
            {
                DimTheBack(true);
                imageRenderer.transform.DORotate(targetRotation, rotationDuration, RotateMode.FastBeyond360)
                    .SetEase(MyCustomEasingFunction).Play();
                StartCoroutine(MoveImagesCoroutine(true));
            }
            else
            {
                DimTheBack(false);
                imageRenderer.transform.DORotate(-targetRotation, rotationDuration, RotateMode.FastBeyond360)
                    .SetEase(MyCustomEasingFunction).Play();
                StartCoroutine(MoveImagesCoroutine(false));
            }

            // Update last activation time
            lastActivationTime = Time.time;
        }

        private float MyCustomEasingFunction(float time, float duration, float overshootOrAmplitude, float period)
        {
            // Custom easing function that slows down as it approaches the end
            float t = time / duration;
            return Mathf.Lerp(0, 1, Mathf.Sin(t * Mathf.PI * 0.5f));
        }


        IEnumerator MoveImagesCoroutine(bool moveBackwards)
        {
            reversed = moveBackwards; // Set direction flag based on input

            foreach (GameObject obj in SettingsObj)
            {
                Vector3 targetPosition;

                if (reversed)
                {
                    // Calculate target position for backward movement
                    targetPosition = obj.transform.localPosition + new Vector3(moveAmount, 0f, 0f);
                    obj.SetActive(true);
                }
                else
                {
                    // Calculate target position for forward movement
                    targetPosition = obj.transform.localPosition - new Vector3(moveAmount, 0f, 0f);
                }

                // Smoothly move the image using DOTween
                obj.transform.DOLocalMove(targetPosition + extraMoveAmount, moveDuration)
                    .SetEase(DG.Tweening.Ease.Linear).OnComplete(() =>
                    {
                        obj.transform.DOLocalMove(targetPosition, extraMoveDuration).SetEase(DG.Tweening.Ease.Linear)
                            // Adjust ease function for desired movement style
                            .Play();
                        if (!moveBackwards)
                        {
                            obj.SetActive(false);
                        }
                    });

                // Wait for the movement to finish before moving the next image
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void DimTheBack(bool turn)
        {
            Image background = backMask?.GetComponent<Image>();
            if (backMask != null)
            {
                if (turn)
                {
                    backMask.SetActive(true);
                    background.DOFade(0.7f, 0.2f);
                }
                else
                {
                    background.DOFade(0.0f, 0.2f).OnComplete(() => backMask?.SetActive(false));
                }
            }
        }

        private void DimTheBackForBooster(bool turn)
        {
            //  var holyParticle = .transform.Find("Holy hit")?.gameObject;

            holyParticle.SetActive(turn);
            Image background = dimBack.GetComponent<Image>();
            if (turn)
            {
                ReferencerUI.Instance.topPanelCanvas.sortingOrder = 0;
                dimBack.SetActive(true);
                background.DOFade(0.7f, 0.2f);
            }
            else
            {
                dimBack.SetActive(false);
                if (backMask != null) background.DOFade(0.0f, 0.2f).OnComplete(() => backMask?.SetActive(false));
                ReferencerUI.Instance.topPanelCanvas.sortingOrder = 3;
            }
        }

        private void UnCheckBoost()
        {
            checkOn = false;
            if (MainManager.Instance.gameStatus == GameState.Map)
                Check(false);
            else
            {
                MainManager.Instance.ActivatedBoost = BoostType.Empty;
                MainManager.Instance.UnLockBoosts(); //for in-game boosts
            }
        }

        public void InitBoost()
        {
            check.SetActive(false);
            MainManager.Instance.BoostColorfullBomb = 0;
            MainManager.Instance.BoostPackage = 0;
            MainManager.Instance.BoostStriped = 0;
            if (boostCountText != null)
                boostCountText.text = AmountOwned == 0 ? "+" : AmountOwned.ToString();


            checkOn = false;
        }

        private void Check(bool checkIt)
        {
            switch (type)
            {
                case BoostType.DiscoBalls:
                    MainManager.Instance.BoostColorfullBomb = checkIt ? 1 : 0;
                    break;
                case BoostType.Bombs:
                    MainManager.Instance.BoostPackage = checkIt ? 2 : 0;
                    break;
                case BoostType.Rockets:
                    MainManager.Instance.BoostStriped = checkIt ? 2 : 0;
                    break;
                case BoostType.Chopper:
                    MainManager.Instance.BoostChopper = checkIt ? 1 : 0;
                    break;
                case BoostType.ExtraMoves:
                    if (checkIt) checkIt = false;
                    break;
                case BoostType.ExtraTime:
                    break;
                case BoostType.Bomb:
                    break;
                case BoostType.FreeMove:
                    break;
                case BoostType.ExplodeArea:
                    break;
                case BoostType.None:
                    break;
            }

            checkOn = checkIt;
            if (check != null)
            {
                check.SetActive(checkIt);
            }

            //boostCountText.gameObject.SetActive(!checkIt);
            //InitScript.Instance.SpendBoost(type);
        }

        public void LockBoost()
        {
            if (Lock != null)
            {
                Lock.SetActive(true);
            }

            if (Indicator != null)
            {
                Indicator.SetActive(false);
            }
        }

        public void UnLockBoost()
        {
            if (dimBack != null)
            {
                DimTheBackForBooster(false);
                Destroy(holder.GetComponent<Canvas>());
                Destroy(MainManager.Instance.Level.GetComponent<SortingGroup>());
            }

            if (Lock != null)
            {
                Lock.SetActive(false);
            }

            if (Indicator != null)
            {
                Indicator.SetActive(true);
            }

            if (boostCountText != null)
                boostCountText.text = AmountOwned == 0 ? "+" : AmountOwned.ToString();
            // ShowPlus(BoostCount() <= 0);
        }

        private bool IsLocked()
        {
            return Lock.activeSelf;
        }

        private int BoostCount()
        {
            Debug.Log("boost count " + AmountOwned);
            return AmountOwned; //PlayerPrefs.GetInt("" + type , 0);
            // commented out for test
            // return 10;
        }

        private static void OpenBoostShop(BoostType boost)
        {
            // BoostShop.SetBoost(boost, callback);
            BoostShop.SetBoosterOffer(boost);
            ReferencerUI.Instance.OnToggleShop?.Invoke(true);
        }

        private void ShowPlus(bool show)
        {
            //boostCountText?.gameObject.SetActive(show);
            //counter?.gameObject.SetActive(!show);
        }

        public void UpdateBoosterText()
        {
            if (!boostCountText) return;
            Debug.Log("update text update text" + AmountOwned);
            boostCountText.text = AmountOwned == 0 ? "+" : AmountOwned.ToString();
        }
    }
}