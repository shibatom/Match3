using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using DG.Tweening;
using HelperScripts;
using Internal.Scriptable.Rewards;
using Internal.Scripts.Blocks;
using Internal.Scripts.Effects;
using Internal.Scripts.GUI;
using Internal.Scripts.GUI.Boost;
using Internal.Scripts.Items;
using Internal.Scripts.Level;
using Internal.Scripts.MapScripts;
using Internal.Scripts.System;
using Internal.Scripts.System.Combiner;
using Internal.Scripts.System.Orientation;
using Internal.Scripts.System.Pool;
using Internal.Scripts.TargetScripts.TargetSystem;

namespace Internal.Scripts
{
    //game state enum
    public enum GameState
    {
        Map,
        PrepareGame,
        RegenLevel,
        Tutorial,
        Pause,
        Playing,
        PreFailed,
        GameOver,
        ChangeSubLevel,
        PreWinAnimations,
        Win,
        WaitForPopup,
        WaitAfterClose,
        BlockedGame,
        BombFailed,
        ButlersGifts
    }


    /// <summary>
    /// core-game class, using for handle game states, blocking, sync animations and search mathing and map
    /// </summary>
    public class MainManager : MonoBehaviour
    {
        [Header(" Enable Logs")] public bool logEnabled = true;

        // Counter for falling items in each column
        [Space] private int[] _fallingItemsPerColumn;
        public static MainManager Instance;

        //life shop reference
        public LifeRefillPopup lifeShop;

        //true if Unity in-apps is enable and imported
        public bool enableInApps;

        //square width for border placement
        public float squareWidth = 1.2f;

        //item which was dragged recently
        public Item lastDraggedItem;

        //item which was switched succesfully recently
        public Item lastSwitchedItem;

        //makes scores visible in the game
        public GameObject popupScore;

        // Congratz Words
        public ComboPopupWords popupWords;

        //current game level
        public int currentLevel = 1;

        //current sub-level
        private int currentSubLevel = 1;

        public int CurrentSubLevel
        {
            get { return currentSubLevel; }
            set
            {
                currentSubLevel = value;
                levelData.currentSublevelIndex = currentSubLevel - 1;
            }
        }

        //current field reference
        public FieldBoard field => fieldBoards.Count > 0 ? fieldBoards[CurrentSubLevel - 1] : null;

        //EDITOR: cost of continue after failing
        public int FailedCost;

        //EDITOR: moves gived to continue
        public int ExtraFailedMoves = 5;

        //EDITOR: time gived to continue
        public int ExtraFailedSecs = 30;

        //true if thriving block destroyed on current move
        public bool thrivingBlockDestroyed;

        //menuCanvas for settingsUI

        //SettingsUI gameObject
        public GameObject _settingsUI;


        //Added_feature
        public GameObject dispenserSpawner;
        public bool thrivingBlockMachine;
        public Dictionary<string, List<Rectangle>> IfanythrivingBlock = new Dictionary<string, List<Rectangle>>();
        private int Res;

        public bool isInCooldown = false; // Cooldown flag
        public Action<bool, float, BoostType> OnCooldownUpdate; // Action to update cooldown status

        public void GenerateRandom()
        {
            Res = Random.Range(1, IfanythrivingBlock.Count + 1);
        }

        public int Getint
        {
            get { return Res; }
        }

        public string SubScribeIcicleSpread()
        {
            string res = "" + (IfanythrivingBlock.Count + 1);

            IfanythrivingBlock.Add(res, new List<Rectangle>());

            return res;
        }

        public void UnSubscribeIcicleSpread(string Key)
        {
            if (IfanythrivingBlock.ContainsKey(Key))
                IfanythrivingBlock.Remove(Key);
        }


        //variables for boost which place bonus candies on the field
        public int BoostColorfullBomb;
        public int BoostPackage;
        public int BoostStriped;

        public int BoostChopper;

        public string androidSharingPath;

        public string iosSharingPath;

        //put some Chopper bears to the field on start
        public bool enableChopper => levelData.enableChopper;

        //empty boost reference for system
        public BoostType emptyBoostIcon;

        //debug settings reference
        public DebugSettings DebugSettings;

        //additional gameplay settings reference
        public AdditionalSettings AdditionalSettings;

        public static bool CanUseBoost = true;

        //activate boost in game
        private BoostType _activatedBoost;

        public BoostType ActivatedBoost
        {
            get => _activatedBoost == BoostType.Empty ? emptyBoostIcon : _activatedBoost;
            set
            {
                if (value == BoostType.Empty)
                {
                    if (_activatedBoost != BoostType.Empty && gameStatus == GameState.Playing)
                    {
                        Initiations.Instance.SpendBoost(_activatedBoost);
                        UnLockBoosts();
                    }
                }

                //        if (activatedBoost != null) return;
                _activatedBoost = value;

                if (value != BoostType.Empty)
                {
                    LockBoosts();
                }

                if (_activatedBoost == BoostType.Empty) return;
                if (_activatedBoost != BoostType.ExtraMoves && _activatedBoost != BoostType.ExtraTime) return;
                if (Instance.levelData.limitType == LIMIT.MOVES)
                    Instance.levelData.limit += 5;
                else
                    Instance.levelData.limit += 30;

                ActivatedBoost = BoostType.Empty;
            }
        }

        //score gain on this game
        public static int Score;

        //stars gain on this game
        public int stars;

        //striped effect reference
        public GameObject stripesEffect;

        //show popup score on field
        public bool showPopupScores;

        //popup score color
        public Color[] scoresColors;

        //popup score outline
        public Color[] scoresColorsOutline;

        //Level gameobject reference
        public GameObject Level;

        //Gameobject reference
        public GameObject LevelsMap;

        //Gameobject reference
        public GameObject FieldsParent;

        //in game boost reference
        public BoostInventory[] InGameBoosts;
        public GameObject explosionPrefab;
        public GameObject self;

        //levels passed for the current session
        //reference to orientation handler
        public GameCameraOrientationHandler orientationGameCameraHandle;

        [HideInInspector] public List<AnimateItems> animateItems = new List<AnimateItems>();

        //blocking to drag items for a time
        public bool dragBlocked;
        private Coroutine resetBlockCourotine;
        private float blockTime;
        public bool _clickBoosterShop;

        public bool DragBlocked
        {
            get
            {
                if (dragBlocked && blockTime > 0 && Time.time - blockTime >= 3 && gameStatus == GameState.Playing && _clickBoosterShop == false)
                {
                }

                //  dragBlocked = false;
                return dragBlocked;
            }
            set
            {
                if (!value) Item.usedItem = null;
                dragBlocked = value;
                blockTime = Time.time;
                var moveID = Instance.moveID;
                // if (dragBlocked){}
                //  resetBlockCourotine = StartCoroutine(ResetBlock(moveID));
                //else if (resetBlockCourotine != null){}
                // StopCoroutine(resetBlockCourotine);
            }
        }

        private IEnumerator ResetBlock(int moveID)
        {
            yield return new WaitForSeconds(3);
            if (moveID == Instance.moveID && dragBlocked)
            {
                dragBlocked = false;
                Item.usedItem = null;
            }
        }

        //current move ID
        public int moveID;

        //value for regeneration, items with falling or not
        public bool onlyFalling;

        //level loaded, wait until true for some courotines
        public bool levelLoaded;

        //true if Facebook plugin installed
        public bool FacebookEnable;

        //combine manager listener
        public CombinationManager combineManager;

        //true if search of matches has started
        public bool findMatchesStarted;

        //true if need to check matches again
        private bool checkMatchesAgain;

        //if true - start the level avoind the map for debug
        public bool testByPlay;

        //game events

        #region EVENTS

        public delegate void GameStateEvents();

        public static event GameStateEvents OnMapState;
        public static event GameStateEvents OnEnterGame;
        public static event GameStateEvents OnLevelLoaded;
        public static event GameStateEvents OnWaitForTutorial;
        public static event GameStateEvents OnMenuPlay;
        public static event GameStateEvents OnSublevelChanged;
        public static event GameStateEvents OnMenuComplete;
        public static event GameStateEvents OnStartPlay;
        public static event GameStateEvents OnWin;
        public static event GameStateEvents OnLose;
        public static event GameStateEvents OnTurnEnd;
        public static event GameStateEvents OnCombo;
        public static event GameStateEvents OnStoryShow;

        public static event GameStateEvents OnMove;

        public delegate void HandlerEvents();

        public static event HandlerEvents onClick;
        public static event HandlerEvents onDragStart;
        public static event HandlerEvents onDragEnd;

        public delegate void ShakeAction(float duration, float strength);

        public static event ShakeAction OnShakeRequested;

        private bool isDragging = false;
        private Vector3 startPosition;

        //current game state
        private GameState GameStatus;

        public GameState gameStatus
        {
            get { return GameStatus; }
            set
            {
                GameStatus = value;
                Debug.LogError("Set GameStatus " + GameStatus);
                //AdsManager.Instance?.CheckAdsEvents(value);
                switch (value)
                {
                    case GameState.PrepareGame: //preparing and initializing  the game
                        //StartCoroutine(AI.THIS.CheckPossibleCombines());
                        PersistantData.PassLevelCounter++;
                        PrepareGame();

                        // var firstItemPrefab = THIS.levelData.target.prefabs.FirstOrDefault();
                        // if (firstItemPrefab && firstItemPrefab.GetComponent<Item>() && !firstItemPrefab.GetComponent<ItemSimple>())
                        //     collectIngredients = true;
                        _colorGetter = new ColorGetter();
                        GenerateLevel(_colorGetter);
                        levelLoaded = true;
                        OnLevelLoaded?.Invoke();
                        _settingsUI.SetActive(false);
                        break;
                    case GameState.WaitForPopup: //waiting for pre game banners
                        StopCoroutine(IdleItemsDirection());
                        StartCoroutine(IdleItemsDirection());
                        var find = GameObject.Find("CanvasBack");
                        if (find != null) find.GetComponent<GraphicRaycaster>().enabled = false;
                        if (orientationGameCameraHandle != null)
                        {
                            GameCameraOrientationHandler.CameraParameters cameraParameters = orientationGameCameraHandle.GetCameraParameters();
                            Vector2 cameraCenter = orientationGameCameraHandle.GetCenterOffset();

                            StartCoroutine(AnimateField(field.GetPosition() + cameraCenter, cameraParameters.size));
                        }

                        break;

                    case GameState.ButlersGifts:
                        StartCoroutine(ShowButlersGifts());
                        break;

                    case GameState.Tutorial: //tutorial state
                        OnWaitForTutorial?.Invoke();
                        break;
                    case GameState.PreFailed: //chance to continue the game, shows menu PreFailed
                        GlobalValue.WinCounter = 0;
                        //AdsManager.Instance.CacheRewarded();
                        GlobalValue.Life -= 1;
                        LeanTween.delayedCall(1, () => popupWords.ShowGratzWord(GratzWordState.LevelFailed));
                        LeanTween.delayedCall(3, () =>
                        {
                            if (counterTarget == 0)
                                gameStatus = GameState.PreWinAnimations;

                            else
                            {
                                var preFailedGameObject = ReferencerUI.Instance.PreFailed.gameObject;

                                preFailedGameObject.GetComponent<PreFailedPopup>().SetFailed();

                                preFailedGameObject.SetActive(true);
                            }
                        });
                        break;
                    case GameState.BombFailed:
                        LeanTween.delayedCall(0.3f, () =>
                        {
                            var preFailedGameObject = ReferencerUI.Instance.PreFailed.gameObject;
                            preFailedGameObject.GetComponent<Pre_FailedPanel>().SetBombFailed();
                            preFailedGameObject.SetActive(true);
                        });
                        break;
                    case GameState.Map: //map state
                        //open map or test level

                        /*if (MenuReference.THIS.roadmapItemsManager.CheckForLevelItemActions())
                            return;*/

                        if (PlayerPrefs.GetInt("OpenLevelTest") <= 0 || FindObjectOfType<HandleLevelRestart>())
                        {
                            EnableMap(true);
                            OnMapState?.Invoke();
                        }
                        else
                        {
                            Instance.gameStatus = GameState.PrepareGame;
                            if (!testByPlay)
                                PlayerPrefs.SetInt("OpenLevelTest", 0);
                            PlayerPrefs.Save();
                        }

                        // if (CrosssceneData.passLevelCounter > 0 && InitScript.Instance.ShowRateEvery > 0)
                        // {
                        //     if (CrosssceneData.passLevelCounter % InitScript.Instance.ShowRateEvery == 0 &&
                        //         InitScript.Instance.ShowRateEvery > 0 && !GlobalValue.UserHasRated)
                        //         InitScript.Instance.ShowRate();
                        // }


                        break;
                    case GameState.Playing: //playing state
                        Screen.sleepTimeout = SleepTimeout.NeverSleep;
                        // StartCoroutine(AI.THIS.CheckPossibleCombines());
                        break;
                    case GameState.GameOver: //game over
                        Screen.sleepTimeout = SleepTimeout.SystemSetting;
                        ReferencerUI.Instance.MenuFailed.gameObject.SetActive(true);
                        OnLose?.Invoke();
                        break;
                    case GameState.PreWinAnimations: //animations after win
                        GlobalValue.WinCounter++;
                        //TopBarAnimationController.OnTopBarStateChange(TopBarAnimationState.Win);
                        StartCoroutine(PreWinAnimationsCor());
                        break;
                    case GameState.ChangeSubLevel: //changing sub level state
                        if (CurrentSubLevel != GetLastSubLevel())
                            ChangeSubLevel();
                        break;
                    case GameState.Win: //shows MenuComplete
                        if (PlayerPrefs.GetInt("LastLevelPassed", 0) < currentLevel)
                            PlayerPrefs.SetInt("LastLevelPassed", currentLevel);
                        OnMenuComplete?.Invoke();
                        ReferencerUI.Instance.MenuComplete.gameObject.SetActive(true);
                        //MenuReference.THIS.cardHolder.gameObject.SetActive(true);
                        CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.complete[1]);
                        if (winRewardAmount > 0)
                            Initiations.Instance.ShowGemsReward(winRewardAmount); // InitScript.Instance.ShowGemsReward(10);
                        OnWin();
                        break;
                }
            }
        }

        [SerializeField] private ButlersGiftsController butlersGiftsController;

        private IEnumerator ShowButlersGifts()
        {
            //for test
            //GlobalValue.WinCounter = 3;
            dragBlocked = true;
            if (butlersGiftsController.CheckAndShowGifts(GlobalValue.WinCounter))
                yield return new WaitForSeconds(3);
            dragBlocked = false;
            gameStatus = GameState.Playing;
        }

        //Combine manager reference
        public CombinationManager CombineManager
        {
            get
            {
                if (combineManager == null) combineManager = new CombinationManager();
                return combineManager;
            }
        }

        //if true - pausing all falling animations
        public bool StopFall => _stopFall.Count > 0;

        //returns last sub-level of this level
        private int GetLastSubLevel()
        {
            return fieldBoards.Count;
        }

        //returns current game state
        public static GameState GetGameStatus()
        {
            return Instance.gameStatus;
        }

        //menu play enabled invokes event
        public void MenuPlayEvent()
        {
            OnMenuPlay?.Invoke();
        }

        //Switch sub level to next
        private void ChangeSubLevel()
        {
            CurrentSubLevel++;

            GameCameraOrientationHandler.CameraParameters cameraParameters = orientationGameCameraHandle.GetCameraParameters();
            Vector2 cameraCenter = orientationGameCameraHandle.GetCenterOffset();
            StartCoroutine(AnimateField(field.GetPosition() + cameraCenter, cameraParameters.size));
        }

        #endregion

        //Lock boosts
        private void LockBoosts()
        {
            //Debug.Log("LockBOsst");
            foreach (var item in InGameBoosts)
            {
                if (item.type != ActivatedBoost)
                    item.LockBoost();
            }
        }

        //unlock boosts
        public void UnLockBoosts()
        {
            foreach (var item in InGameBoosts)
            {
                item.UnLockBoost();
            }
        }

        //Load the level from "OpenLevel" player pref
        public void LoadLevel()
        {
            currentLevel = GlobalValue.CurrentLevel;
            if (currentLevel == 0)
                currentLevel = 1;
            LoadLevel(currentLevel);
        }

        //enable map
        public void EnableMap(bool enable)
        {
            bool isRun = false;
            if (Camera.main == null)
                return;
            Camera.main.orthographicSize = 5.3f;
            if (enable)
            {
                ReferencerUI.Instance.menuBottomPanelCanvas.SetActive(true);
                Camera.main.GetComponent<MapCamera>().SetPosition(new Vector2(0, GetComponent<Camera>().transform.position.y));
                if (FindObjectOfType<HandleLevelRestart>() == null && DebugSettings.AI && DebugSettings.testLevel > 0)
                {
                    GlobalValue.CurrentLevel = DebugSettings.testLevel;
                    RestartLevel();
                }
                else
                {
                    if (Camera.main.GetComponent<MapCamera>().enabled == enable)
                    {
                        isRun = true;
                        setNumbers();
                    }
                }
            }
            else
            {
                ReferencerUI.Instance.menuBottomPanelCanvas.SetActive(false);
                GetComponent<Camera>().orthographicSize = 4;
                if (OnlineTime.THIS.dateReceived)
                    Initiations.DateOfExit = OnlineTime.THIS.serverTime.ToString();
                ReferencerUI.Instance.GetComponent<GraphicRaycaster>().enabled = false;
                ReferencerUI.Instance.GetComponent<GraphicRaycaster>().enabled = true;
                Level.transform.Find("Canvas").GetComponent<GraphicRaycaster>().enabled = false;
                Level.transform.Find("Canvas").GetComponent<GraphicRaycaster>().enabled = true;
            }

            Camera.main.GetComponent<MapCamera>().enabled = enable;
            LevelsMap.SetActive(!enable);
            LevelsMap.SetActive(enable);
            Level.SetActive(!enable);

            if (!isRun && Camera.main.GetComponent<MapCamera>().isActiveAndEnabled)
                setNumbers();

            if (!enable)
                Camera.main.transform.position = new Vector3(0, 0, -10) - (Vector3)orientationGameCameraHandle.offsetFieldPosition;

            foreach (var item in fieldBoards)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }

            isRun = false;
        }

        private void setNumbers()
        {
            var numberObject = Resources.Load<GameObject>("Prefabs/Number");
            var parentObj = FindObjectOfType<Path>();
            //StartCoroutine(SetLevelNumberToMapObject(numberObject, parentObj.Waypoints));
        }

        private IEnumerator SetLevelNumberToMapObject(GameObject numberObj, List<Transform> levelParentObject)
        {
            yield return new WaitForSeconds(0.01f);
            int counter = 0;
            foreach (var t in levelParentObject)
            {
                counter++;
                if (Camera.main.GetComponent<MapCamera>().isActiveAndEnabled)
                {
                    var obj = Instantiate(numberObj, t.parent, true);
                    obj.GetComponent<Canvas>().worldCamera = Camera.main;
                    obj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "" + counter;
                    var rt = obj.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0.011f, 0.235f); /*new Rect(, , );*/
                    rt.sizeDelta = new Vector2(30.25f, 28.47f);
                }
                else
                    break;
            }
        }

        private void Awake()
        {
            Instance = this;
            testByPlay = false;
            // testByPlay = true;//enable to instant level run
        }

        // Use this for initialization
        private void Start()
        {
#if UNITY_EDITOR
            Debug.unityLogger.logEnabled = logEnabled;
            //   Debug.unityLogger.filterLogType = LogType.Error;
#else
            Debug.unityLogger.logEnabled = false;
            Debug.unityLogger.filterLogType = LogType.Error;

#endif
            // GlobalValue.Coin = 10000;
            PlayerPrefs.Save();
            _manageInput = gameObject.AddComponent<ManageInput>();
            ManageInput.OnDown += MouseDown;
            ManageInput.OnUp += MouseUp;
            ManageInput.OnDownRight += MouseDownRight;
            DebugSettings = Resources.Load<DebugSettings>("Scriptable/DebugSettings");
            AdditionalSettings = Resources.Load<AdditionalSettings>("Scriptable/AdditionalSettings");
            LeanTween.init(800);
            LeanTween.reset();
#if FACEBOOK
            FacebookEnable = true;
            /*if (FacebookEnable && (!NetworkManager.THIS?.IsLoggedIn ?? false))
                FacebookManager.THIS.CallFBInit();
            else Debug.LogError("Facebook not initialized, please, install database service");*/
#else
            FacebookEnable = false;

#endif
#if UNITY_INAPPS
            //gameObject.AddComponent<UnityInAppsIntegration>();
            enableInApps = true;
#else
            enableInApps = false;

#endif

            //        if (!THIS.enableInApps)
            //            GameObject.Find("CanvasMap/SafeArea/Gems").gameObject.SetActive(false);

            gameStatus = GameState.Map;
            winRewardAmount = Resources.Load<WinReward>("Scriptable/WinReward").winRewardAmount;
            if (squareBoundaryLine == null)
            {
                squareBoundaryLine = FindAnyObjectByType<SquareBoundaryLine>(); // Cache for later use
            }

            OnCooldownUpdate += UpdateCooldownStatus;
        }

        private void OnDisable()
        {
            ManageInput.OnDown -= MouseDown;
            ManageInput.OnUp -= MouseUp;
            ManageInput.OnDownRight -= MouseDownRight;
        }

        public int Limit = 0;

        private void PrepareGame()
        {
            ActivatedBoost = BoostType.Empty;

            Score = 0;
            stars = 0;
            moveID = 0;
            fieldBoards = new List<FieldBoard>();
            transform.position += Vector3.down * 1000;
            // targetUIObject.SetActive(false);
            Instance.thrivingBlockDestroyed = false;

            EnableMap(false);

            LoadLevel();
            if (levelData != null)
            {
                _fallingItemsPerColumn = new int[levelData.maxCols]; // NEW: Initialize array
            }
            else
            {
                Debug.LogError("LevelData is null during PrepareGame, cannot initialize falling counters.");
                _fallingItemsPerColumn = new int[0]; // Initialize empty to avoid errors
            }

            CurrentSubLevel = 1;
            if (ProgressBarController.Instance != null)
                ProgressBarController.Instance.InitBar();

            if (levelData.limitType == LIMIT.MOVES)
            {
                InGameBoosts.Where(i => i.type == BoostType.ExtraMoves).ToList().ForEach(i => i.gameObject.SetActive(true));
                InGameBoosts.Where(i => i.type == BoostType.ExtraTime).ToList().ForEach(i => i.gameObject.SetActive(false));
            }
            else
            {
                InGameBoosts.Where(i => i.type == BoostType.ExtraMoves).ToList().ForEach(i => i.gameObject.SetActive(false));
                InGameBoosts.Where(i => i.type == BoostType.ExtraTime).ToList().ForEach(i => i.gameObject.SetActive(true));
            }

            OnEnterGame?.Invoke();
        }

        // NEW: Method to increment falling count for a column
        public void IncrementFallingCount(int col)
        {
            if (col >= 0 && col < _fallingItemsPerColumn.Length)
            {
                _fallingItemsPerColumn[col]++;
                // Optional: Log changes
                // Debug.Log($"Column {col} falling count incremented to: {fallingItemsPerColumn[col]}");
            }
            else
            {
                Debug.LogWarning($"Attempted to increment falling count for invalid column: {col}");
            }
        }

        public List<FieldBoard> fieldBoards = new List<FieldBoard>();
        public GameObject FieldBoardPrefab;
        public LevelData levelData;
        internal bool tutorialTime;

        //Generate loaded level
        public void GenerateLevel(IColorGettable colorGettable)
        {
            var fieldPos = new Vector3(-0.9f, 0, -10);
            var latestFieldPos = Vector3.right * ((GetLastSubLevel() - 1) * 10) + Vector3.back * 10;

            var i = 0;
            foreach (var item in fieldBoards)
            {
                var _field = item.gameObject;
                _field.transform.SetParent(FieldsParent?.transform);
                _field.transform.position = fieldPos + Vector3.right * (i * 15);
                var fboard = _field.GetComponent<FieldBoard>();

                fboard.CreateField(colorGettable);
                latestFieldPos = fboard.GetPosition();

                i++;
            }

            levelData.TargetCounters.RemoveAll(x => x.targetLevel.setCount == SetCount.FromLevel && x.GetCount() == 0);

            if (orientationGameCameraHandle != null)
            {
                transform.position = latestFieldPos + Vector3.right * 10 + Vector3.back * 10 - (Vector3)orientationGameCameraHandle.offsetFieldPosition;
            }

            SetPreGamePowerUps();
            counterTarget = levelData.TargetCounters.Count;
            // print(counterTarget+"CounterAmir");
            //SetPreBoosts();
        }

        public bool animStarted;


        /// <summary>
        /// Move camera to the field
        /// </summary>
        /// <param name="destPos">Position of the field</param>
        /// <param name="cameraParametersSize">Camera size</param>
        /// <returns></returns>
        private IEnumerator AnimateField(Vector3 destPos, float cameraParametersSize)
        {
            var _camera = GetComponent<Camera>();
            if (animStarted) yield break;
            animStarted = true;
            //var duration = 2f;
            var speed = 25f;
            var startPos = transform.position;
            var distance = Vector2.Distance(startPos, destPos);
            var time = distance / speed;
            var curveX = new AnimationCurve(new Keyframe(0, startPos.x), new Keyframe(time, destPos.x));
            var startTime = Time.time;
            float distCovered = 0;
            orientationPanels.ShowMenu();
            while (distCovered < distance)
            {
                distCovered = (Time.time - startTime) * speed;
                transform.localPosition = new Vector3(curveX.Evaluate(Time.time - startTime), transform.position.y, 0);
                _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, cameraParametersSize, Time.deltaTime * 5);
                yield return new WaitForEndOfFrame();
            }

            _camera.orthographicSize = cameraParametersSize;
            transform.position = destPos;
            yield return new WaitForSeconds(0.5f);
            animStarted = false;
            GameStart();
        }


        public PanelOrientationHandler orientationPanels;

        //game start
        private void GameStart()
        {
            /*if (CurrentSubLevel - 1 == 0)
                MenuReference.THIS.Tutorials.gameObject.SetActive(true);
               // orientationPanels.ShowMenu();
            else*/

            OnSublevelChanged?.Invoke();
            gameStatus = GameState.Playing;
        }

        /// Cloud effect animation for different direction levels
        private IEnumerator IdleItemsDirection()
        {
            if (field.squaresArray.Select(i => i.direction).Distinct().Count() > 1)
            {
                while (true)
                {
                    yield return new WaitForSeconds(3);
                    if (gameStatus == GameState.Playing && !findMatchesStarted)
                    {
                        // var orderedEnumerableCol = DirectionCloudEffect.GetItems();
                        var orderedEnumerableCol = Instance.field.GetItems()
                            .GroupBy(i => i.square.squaresGroup).ToList()
                            .Select(x => new
                            {
                                items = x,
                                Num = x.Max(i => i.square.orderInSequence),
                                Count = x.Count(),
                                x.Key
                            }).OrderByDescending(i => i.Num).ToList();

                        // Debug.WatchInstance(orderedEnumerableCol);
                        foreach (var items in orderedEnumerableCol)
                        {
                            var animationFinished = false;
                            foreach (var item in items.items)
                            {
                                if (item.destroying) continue;
                                StartCoroutine(item.DirectionAnimation(() => { animationFinished = true; }));
                            }

                            yield return new WaitUntil(() => animationFinished);
                        }
                    }

                    yield return new WaitForSeconds(1);
                }
            }
        }

        public int counterTarget = 0;

        public void TargetFinished()
        {
            counterTarget--;
            if (counterTarget <= 0)
                CheckWinLose();
        }

        private bool gameFinished = false;

        //Check win or lose conditions
        public void CheckWinLose()
        {
            if (gameFinished)
                return;
            var lose = false;
            var win = false;

            if (Limit <= 0)
            {
                Limit = 0;

                if (!levelData.IsTotalTargetReached())
                    lose = true;
                else win = true;
            }

            else
            {
                // if (levelData.limit <= 5)
                // {
                //     TopBarAnimationController.OnTopBarStateChange(TopBarAnimationState.Worried);
                // }

                if (levelData.IsTotalTargetReached() && !levelData.WaitForMoveOut())
                {
                    win = true;
                }
                else if (levelData.IsTargetReachedSublevel() && fieldBoards.Count > 1)
                    gameStatus = GameState.ChangeSubLevel;
                else if (noTip && !lose && fieldBoards.Count > 1 && levelData.GetField().switchSublevelNoMatch)
                {
                    noTip = false;
                    gameStatus = GameState.ChangeSubLevel;
                }
            }

            if (lose && !win)
            {
                gameFinished = true;
                gameStatus = GameState.PreFailed;
            }

            else if (!lose && win)
            {
                gameFinished = true;
                gameStatus = GameState.PreWinAnimations;
            }
            // else if (!win && !lose && FindObjectsOfType<itemTimeBomb>().Any(i => i.timer <= 0))
            // {
            //     LevelManager.THIS.gameStatus = GameState.BombFailed;
            // }

            // if (DebugSettings.AI && (win || lose))
            // {
            //     //Debug.Log((win ? "win " : "lose ") + " score " + Score + " stars " + stars + " moves/time rest " + THIS.levelData.limit);
            //     RestartLevel();
            // }
        }

        public GameObject winTrail;
        public Transform movesTransform;

        public int destLoopIterations;

        //Animations after win
        private IEnumerator PreWinAnimationsCor()
        {
            Debug.LogError("sallog PreWinLoop 1 ");
            //Debug.Log("sallog TapToskip");
            tapToSkip = Instantiate((GameObject)Resources.Load("Prefabs/TapToSkip"), HandleOrientation.ActiveOrientationCanvas.transform);
            if (!Initiations.Instance.losingLifeEveryGame && GlobalValue.Life < Initiations.Instance.CapOfLife)
                Initiations.Instance.AddLife(1);
            popupWords.ShowGratzWord(GratzWordState.LevelCompleted);
            var limit = Mathf.Clamp(Limit, 0, 20);

            if (!skipWin)
            {
                var c1 = PreWinLoop(limit);
                yield return StartCoroutine(c1);
            }

            if (skipWin)
            {
                Score += limit * Random.Range(500, 3000) / levelData.colorLimit;
                CheckStars();
                skipWin = false;
                Destroy(tapToSkip);
            }

            if (PlayerPrefs.GetInt($"Level.{currentLevel:000}.StarsCount", 0) < stars)
                PlayerPrefs.SetInt($"Level.{currentLevel:000}.StarsCount", stars);
            if (Score > PlayerPrefs.GetInt("Score" + currentLevel))
            {
                PlayerPrefs.SetInt("Score" + currentLevel, Score);
            }

            if (PlayerPrefs.GetInt("ReachedLevel") <= currentLevel)
                PlayerPrefs.SetInt("ReachedLevel", currentLevel + 1);
            PlayerPrefs.Save();
            if (Application.isEditor)
                //Debug.Log("Level " + currentLevel + " score " + Score + " stars " + stars);
                PersistantData.Win = true;
#if PLAYFAB || GAMESPARKS
            NetworkManager.dataManager.SetPlayerScore(currentLevel, Score);
            NetworkManager.dataManager.SetPlayerLevel(currentLevel + 1);
            NetworkManager.dataManager.SetStars(currentLevel);
#elif EPSILON
              NetworkManager.dataManager.SetPlayerLevel(new EpsilonLevel(currentLevel, stars, Score));
#endif
            if (!skipWin)
                yield return new WaitForSeconds(1f);
            gameStatus = GameState.Win;
        }

        private IEnumerator PreWinLoop(int limit)
        {
            Debug.LogError("PreWinLoop - Started");

            SetTopPanelSorting(1);
            List<Item> stripes = new();
            yield return FinalizePreWinAnimation();
            if (skipWin) yield break;


            yield return HandleInitialWinTrails(limit, stripes);
            if (skipWin) yield break;
            levelData.limit = 0;
            Limit = 0;
            yield return HandleExtraItems(stripes);

            //FindMatches();
            // yield return WaitUntilDragUnblocked();

            if (skipWin) yield break;


            Debug.LogError("PreWinLoop - Completed");
        }

        private void SetTopPanelSorting(int order)
        {
            ReferencerUI.Instance.topPanelCanvas.sortingOrder = order;
            //MenuReference.THIS.topPanelMiloMesh.sortingOrder = order + 1;
            Debug.LogError($"Top panel sorting set to {order}");
        }

        private IEnumerator HandleInitialWinTrails(int limit, List<Item> stripes)
        {
            var items = field.GetRandomItems(levelData.limitType == LIMIT.MOVES ? limit : 10);
            int counter = items.Count;
            Debug.LogError($"HandleInitialWinTrails - Retrieved {items.Count} items");
            //yield return new WaitForSeconds(1f);
            var trails = new List<GameObject>();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                {
                    Debug.LogError($"Item {i + 1} is null, skipping.");
                    continue;
                }

                var go = Instantiate(winTrail);
                go.transform.position = movesTransform.position;

                var trail = go.GetComponent<ImplementationOfTrailEffect>();
                trail.target = items[i];

                trails.Add(go);

                trail.StartAnim(target =>
                {
                    if (levelData.limitType == LIMIT.MOVES)
                    {
                        levelData.limit--;
                        Debug.LogError($"Limit decreased: {levelData.limit}");
                    }

                    if (target != null && target.gameObject.activeSelf)
                    {
                        Debug.LogError($"Changing type for item {i + 1}");
                        target.NextType = (ItemsTypes)Random.Range(4, 6);
                        target.ChangeType(newItem =>
                        {
                            Debug.LogError("stripesAddedTToList " + newItem.name + " " + newItem.GetInstanceID() + " " + newItem.currentType);
                            stripes.Add(newItem);
                        });
                        // counter--;
                    }
                });

                if (skipWin)
                {
                    Debug.LogError("skipWin true during trail animation. Exiting.");
                    SetTopPanelSorting(3);
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            // while(counter>0)
            // yield return null;
            yield return new WaitForSeconds(.5f);

            // if (trails.Count > 0)
            // {
            //     Debug.LogError("Waiting for trail effects to complete...");
            //     yield return new WaitForListNull(trails.Cast<object>().ToList());
            // }
        }

        private IEnumerator HandleExtraItems(List<Item> stripes)
        {
            Debug.LogError(stripes.Count + " stripes count");
            yield return new WaitForSeconds(.5f);
            foreach (Item item in stripes)
            {
                //  if (item == null)
                //     {
                //         Debug.LogError("Null item in extra items, skipping.");
                //         continue;
                //     }
                //                     Debug.LogError("Destroying non-multicolor item.");
                Debug.LogError("Destroying non-multicolor item." + item.currentType.ToString() + "" + item.GetHashCode() + "" + item.GetInstanceID());

                item.DestroyItem(CheckJustInItem: false);


                if (skipWin)
                {
                    Debug.LogError("skipWin true while handling extra items. Exiting.");
                    SetTopPanelSorting(3);
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            // {{}
            var extraItems = field.GetAllExtaItems().Where((x) => x.currentType == ItemsTypes.DiscoBall).ToList();
            if (extraItems.Count > 0)
            {
                foreach (var item in extraItems)
                {
                    var randomTarget = field.GetRandomItems(1).First();
                    //Debug.Log($"Multicolor item detected. Checking against random target: {randomTarget}");
                    item.Check(item, randomTarget);
                    yield return new WaitForSeconds(.8f);
                }
            }
            // while (field.GetAllExtaItems().Count > 0 && gameStatus != GameState.Win && !skipWin)
            // {
            //     counter++;
            //     var extraItems = field.GetAllExtaItems();
            //     Debug.LogError($"Processing {extraItems.Count} extra items");

            //     foreach (var item in extraItems.ToList())
            //     {
            //         if (item == null)
            //         {
            //             Debug.LogError("Null item in extra items, skipping.");
            //             continue;
            //         }
            //         if(item.currentType == ItemsTypes.MULTICOLOR)
            //         {
            //                                         var randomTarget = field.GetRandomItems(1).First();
            //             //Debug.Log($"Multicolor item detected. Checking against random target: {randomTarget}");
            //             item.Check(item, randomTarget);
            //         }
            //         else{

            //             Debug.LogError("Destroying non-multicolor item.");
            //             item.DestroyItem();
            //         }

            //         yield return new WaitForSeconds(0.1f);
            //     }

            //     Debug.LogError("Drag blocked after extra item processing.");

            //     // FindMatches();
            //     CreatePower=false;
            //      yield return new WaitForSeconds(0.5f);
            //     // yield return new WaitWhile(() => findMatchesStarted);
            //     Debug.LogError("Finished match finding pass.");
            // }
        }

        private IEnumerator WaitUntilDragUnblocked()
        {
            Debug.LogError("Waiting for drag to unblock...");
            while (dragBlocked)
                yield return new WaitForFixedUpdate();
            Debug.LogError("Drag unblocked.");
        }

        private IEnumerator FinalizePreWinAnimation()
        {
            yield return new WaitForSeconds(1f);

            if (skipWin)
            {
                Debug.LogError("skipWin true before final animations. Exiting.");
                SetTopPanelSorting(3);
                yield break;
            }

            if (_sheypoor)
            {
                Debug.LogError("Sheypoor animation already active. Exiting.");
                yield break;
            }

            Debug.LogError("Spawning PreCompleteAnimations...");
            SetTopPanelSorting(3);

            _sheypoor = Instantiate(ReferencerUI.Instance.PreCompleteAnimations.gameObject, ReferencerUI.Instance.transform);
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.complete[0]);

            int counter = 4;
            while (!skipWin && counter-- > 0)
            {
                Debug.LogError($"Waiting... {counter} seconds remaining.");
                yield return new WaitForSeconds(1);
            }

            SetTopPanelSorting(1);
            Destroy(_sheypoor);
            _sheypoor = null;
            Debug.LogError("PreCompleteAnimations complete and cleaned up.");
        }


        private GameObject _sheypoor;


        private float lastTipTime;

        Coroutine _showTips = null;

        private void Update()
        {
            lastTipTime += Time.deltaTime;
            ////Debug.Log("Game faling: " + fallingDownFlag);
            if (inactiveTime > 4 && !Falling && gameStatus == GameState.Playing && !tutorialTime && multicolorWorking == false && lastTipTime > 3 && _showTips == null)
            {
                lastTipTime = 0;

                //ShowTipsRepeatedly();
                _showTips = StartCoroutine(ArtificialIntelligence.Instance.CheckPossibleCombines());
            }


            //  AvctivatedBoostView = ActivatedBoost;
            if (Input.GetKeyDown(DebugSettings.Regen) && DebugSettings.enableHotkeys)
            {
                NoMatches();
            }

            if (Input.GetKeyDown(DebugSettings.Win) && DebugSettings.enableHotkeys)
            {
                stars = Mathf.Clamp(stars, 1, stars);
                gameStatus = GameState.PreWinAnimations;
            }

            if (Input.GetKeyDown(DebugSettings.Lose) && DebugSettings.enableHotkeys)
            {
                levelData.limit = 1;
            }

            if (Input.GetKeyDown(DebugSettings.Restart) && DebugSettings.enableHotkeys)
            {
                Initiations.Instance.AddLife(1);
                RestartLevel();
            }

            // if (gameStatus == GameState.Playing)
            //     Time.timeScale = DebugSettings.TimeScaleItems;
            // else
            //     Time.timeScale = DebugSettings.TimeScaleUI;

            if (Input.GetKeyDown(DebugSettings.SubSwitch) && DebugSettings.enableHotkeys)
            {
                gameStatus = GameState.ChangeSubLevel;
            }

            if (Input.GetKeyUp(DebugSettings.Back) && DebugSettings.enableHotkeys)
            {
                if (Instance.gameStatus == GameState.Playing)
                    GameObject.Find("CanvasGlobal").transform.Find("MenuPause").gameObject.SetActive(true);
                else if (Instance.gameStatus == GameState.Map)
                    Application.Quit();
            }
        }

        //private void ShowTipsRepeatedly()
        //{


        //}

        private float lastTouchTime;
        public float inactiveTime => Time.time - lastTouchTime;


        void MouseDown(Vector2 pos)
        {
            lastTouchTime = Time.time; // Reset the inactive timer on touch
            onDragStart.Invoke();
            //if (_showTips != null)
            //{
            //    //StopCoroutine(_showTips);
            //    AI.THIS.StopAllCoroutines();
            //    _showTips = null;
            //    //AI.THIS.StopCoroutineShowTipsPeriodically();

            //    //StopCoroutine(AI.THIS._showTipPeriodicly);
            //    //StartCoroutine(AI.THIS.ClearAndCheckBoardReadiness());
            //    //StopCoroutine(AI.THIS._showTipPeriodicly);
            //    //AI.THIS._showTipPeriodicly = null;
            //    //AI.THIS.StopTipAnimation();
            //}
            if (gameStatus == GameState.PreWinAnimations)
            {
                skipWin = true;
                StopAllCoroutines();
                Destroy(_sheypoor?.gameObject);

                gameStatus = GameState.Win;
            }

            if (gameStatus != GameState.Playing && gameStatus != GameState.Tutorial)
                return;
            if (EventSystem.current.IsPointerOverGameObject(-1) && gameStatus == GameState.Playing)
                return;

            var layerMask = LayerMask.GetMask("Item");
            var hit = Physics2D.OverlapPoint(pos, layerMask);
            //Debug.Log($"LevelManager : hit {hit}");

            if (hit == null /* && AdditionalSettings.SelectableBreakableBox*/)
            {
                //Debug.Log($"Position: {pos}");
                //Debug.Log($"Layer mask: {1 << LayerMask.NameToLayer("Square")}");
                Debug.DrawRay(pos, Vector3.forward, Color.red, 2f);

                hit = DetectSquare(pos);
                if (hit != null)
                {
                    // ProcessFakeItem(hit);
                }
            }

            if (hit != null)
            {
                HandleValidHit(hit, pos);
            }
            else
            {
                //Debug.Log("Hit is null.");
            }
        }

        int CreateLayerMask(params string[] layers)
        {
            int mask = 0;
            foreach (var layer in layers)
            {
                mask |= 1 << LayerMask.NameToLayer(layer);
            }

            return mask;
        }

        Collider2D DetectSquare(Vector2 pos)
        {
            var squareMask = 1 << LayerMask.NameToLayer("Square");
            var hit = Physics2D.OverlapPoint(pos, squareMask);
            if (hit != null)
            {
                //Debug.Log("Detected square: " + hit.gameObject.name, hit.gameObject);
            }

            return hit;
        }

        void ProcessFakeItem(Collider2D squareHit)
        {
            var square = squareHit.GetComponent<Rectangle>();
            if (square != null && (square.type == LevelTargetTypes.BreakableBox || square.type == LevelTargetTypes.Eggs || square.type == LevelTargetTypes.GrowingGrass ||
                                   square.type == LevelTargetTypes.Pots))
            {
                var fakeItem = square.GenItem(false);
                fakeItem.HideSprites(true);
                fakeItem.square = square;
                square.Item = fakeItem;
                //Debug.Log("Generated fake item for square: " + square.name);
            }
        }

        void HandleValidHit(Collider2D hit, Vector2 pos)
        {
            //Debug.Log($"Hit object: {hit.gameObject.name} and activated boost is {THIS.ActivatedBoost}");
            var item = hit.GetComponent<Item>();
            var square = hit.GetComponent<Rectangle>();

            if (item != null)
            {
                lastTouchedItem = item;
                //Debug.Log($"Last touched item: {lastTouchedItem?.name ?? "null"}");
            }

            if (tutorialTime && ((item == null || !item.tutorialUsableItem) && square == null))
            {
                //Debug.Log("Exiting due to tutorialTime or unusable item.");
                return;
            }

            if (!Instance.DragBlocked && (gameStatus == GameState.Playing || gameStatus == GameState.Tutorial))
            {
                //Debug.Log("Game is in Playing or Tutorial state. Proceeding with drag logic.");
                OnStartPlay?.Invoke();
                tutorialTime = false;

                HandleBoostOrDrag(item, square, pos);
            }
            else
            {
                //Debug.Log("Drag is blocked or game is not in a valid state.");
            }
        }

        void HandleBoostOrDrag(Item item, Rectangle rectangle, Vector2 pos)
        {
            //Debug.Log($"Square boost: {square?.name ?? item?.name ?? "null"}");
            GlobalValue.SpendItem(ActivatedBoost, 1);
            if (Instance.ActivatedBoost == BoostType.ExplodeArea)
            {
                if (item != null && item.currentType != ItemsTypes.DiscoBall && item.currentType != ItemsTypes.Gredient)
                {
                    ActivateExplodeAreaBoost(item);
                }
                else if (rectangle != null)
                {
                    ActivateExplodeAreaBoostForSquare(rectangle);
                }
            }
            else if (Instance.ActivatedBoost == BoostType.Bomb)
            {
                if (item != null)
                {
                    ActivateBombBoost(item);
                }
                else if (rectangle != null)
                {
                    ActivateBombBoostForSquare(rectangle);
                }
            }
            else if (Instance.ActivatedBoost == BoostType.Arrow)
            {
                if (item != null)
                {
                    ActivateArrowBoost(item);
                }
                else if (rectangle != null)
                {
                    ActivateArrowBoostForSquare(rectangle);
                }
            }
            else if (Instance.ActivatedBoost == BoostType.Canon)
            {
                if (item != null)
                {
                    ActivateCanonBoost(item);
                }
                else if (rectangle != null)
                {
                    ActivateCanonBoostForSquare(rectangle);
                }
            }
            else if (Instance.ActivatedBoost == BoostType.Shuffle)
            {
                lastTouchedItem = null;
                //StartCoroutine(NoMatchesCor());
            }

            else if (item != null && item.square != null && item.square.GetSubSquare().CanGoOut())
            {
                //Debug.Log($"Item {item.name} can go out and is draggable.");
                item.dragThis = true;
                item.mousePos = pos;
                item.deltaPos = Vector3.zero;
            }
            else if (rectangle != null && rectangle.GetSubSquare().CanGoOut())
            {
                //Debug.Log($"Square {square.name} can go out and is draggable.");
                // Add square-specific drag logic here if needed.
            }
            else if (item == null && rectangle == null)
            {
                Debug.LogWarning("Hit object is neither an item nor a square. Ensure correct collider or layer setup.");
            }
        }

        void ActivateExplodeAreaBoost(Item item)
        {
            //Debug.Log("Activated boost: ExplodeArea for Item");
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.boostBomb);
            HapticAndShake(2);
            Instance.DragBlocked = true;

            var obj = Instantiate(Resources.Load("Boosts/area_explosion"), item.transform.position,
                item.transform.rotation) as GameObject;
            obj.GetComponent<SpriteRenderer>().sortingOrder = 4;
            obj.GetComponent<BoostFunctions>().rectangle = item.square;

            //Debug.Log($"Instantiated area explosion at position {item.transform.position}");
            Instance.ActivatedBoost = BoostType.Empty;
        }

        void ActivateExplodeAreaBoostForSquare(Rectangle rectangle)
        {
            //Debug.Log("Activated boost: ExplodeArea for Square");
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.boostBomb);
            HapticAndShake(2);
            Instance.DragBlocked = true;

            var obj = Instantiate(Resources.Load("Boosts/area_explosion"), rectangle.transform.position,
                rectangle.transform.rotation) as GameObject;
            obj.GetComponent<SpriteRenderer>().sortingOrder = 4;
            obj.GetComponent<BoostFunctions>().rectangle = rectangle;

            //Debug.Log($"Instantiated area explosion at position {square.transform.position}");
            Instance.ActivatedBoost = BoostType.Empty;
        }

        void ActivateBombBoost(Item item)
        {
            //Debug.Log("Activated boost: Bomb for Item");
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.boostColorReplace);
            Instance.DragBlocked = true;

            var obj = Instantiate(Resources.Load("Boosts/HammerSpine"), item.transform.position,
                item.transform.rotation) as GameObject;
            obj.GetComponent<BoostFunctions>().rectangle = item.square;
            obj.GetComponent<SpriteRenderer>().sortingOrder = 4;

            FlipTheHammer(item.square, obj);

            //Debug.Log($"Instantiated simple explosion at position {item.transform.position}");
            Instance.ActivatedBoost = BoostType.Empty;
        }


        void ActivateBombBoostForSquare(Rectangle rectangle)
        {
            //Debug.Log("Activated boost: Bomb for Square");
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.boostColorReplace);
            Instance.DragBlocked = true;

            var obj = Instantiate(Resources.Load("Boosts/HammerSpine"), rectangle.transform.position, rectangle.transform.rotation) as GameObject;
            obj.GetComponent<SpriteRenderer>().sortingOrder = 4;
            obj.GetComponent<BoostFunctions>().rectangle = rectangle;

            FlipTheHammer(rectangle, obj);

            //Debug.Log($"Instantiated simple explosion at position {square.transform.position}");
            Instance.ActivatedBoost = BoostType.Empty;
        }

        private static void FlipTheHammer(Rectangle rectangle, GameObject obj)
        {
            Vector3 screenCenter = new Vector3(Screen.width / 2, Screen.height / 2, Camera.main.nearClipPlane);
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenCenter);
            if (rectangle.transform.position.x > worldPosition.x)
            {
                var scale = obj.transform.localScale;
                obj.transform.localScale = new Vector3(scale.x * -1, scale.y, scale.z);
            }
        }

        void ActivateArrowBoost(Item item)
        {
            //Debug.Log("Activated boost: Bomb for Item");
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.boostColorReplace);
            Instance.DragBlocked = true;
            var obj = Instantiate(Resources.Load("Boosts/ArrowSpine"), new Vector3((fieldBoards[0].fieldData.maxCols)switch
            {
                <= 3 => 0, 4 => 1, 5 => 1.5f, 6 => 2.5f, 7 => 3f, 8 => 4, 9 => 4.5f, >= 10 => 5
            }, item.transform.position.y, 0), item.transform.rotation) as GameObject;

            obj.GetComponentInChildren<BoostFunctions>().rectangle = item.square;
            // obj.GetComponent<SpriteRenderer>().sortingOrder = 4;
            //obj.GetComponentInChildren<BoostAnimation>().animator.Play();

            //Debug.Log($"Instantiated simple explosion at position {item.transform.position}");
            Instance.ActivatedBoost = BoostType.Empty;
        }

        void ActivateArrowBoostForSquare(Rectangle rectangle)
        {
            //  ArrowEffect(square);
            //Debug.Log("Activated boost: Bomb for Item");
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.boostColorReplace);
            Instance.DragBlocked = true;

            var obj = Instantiate(Resources.Load("Boosts/ArrowSpine"), new Vector3((fieldBoards[0].fieldData.maxCols)switch
                {
                    <= 3 => 0, 4 => 1, 5 => 1.5f, 6 => 2.5f, 7 => 3f, 8 => 4, 9 => 4.5f, >= 10 => 5
                }, rectangle.transform.position.y, 0),
                rectangle.transform.rotation) as GameObject;

            obj.GetComponentInChildren<BoostFunctions>().rectangle = rectangle;
            // obj.GetComponent<SpriteRenderer>().sortingOrder = 4;
            //obj.GetComponentInChildren<BoostAnimation>().animator.StartPlayback();

            //Debug.Log($"Instantiated simple explosion at position {square.transform.position}");
            Instance.ActivatedBoost = BoostType.Empty;
        }

        void ActivateCanonBoost(Item item)
        {
            //Debug.Log("Activated boost: Bomb for Item");
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.boostColorReplace);
            Instance.DragBlocked = true;

            var obj = Instantiate(Resources.Load("Boosts/CanonSpine"), new Vector3(item.transform.position.x, fieldBoards[0].fieldData.maxRows switch
            {
                <= 3 => -2, 4 => -3f, 5 => -3.5f, 6 => -3.8f, 7 => -4.1f, 8 => -4.5f, 9 => -5, >= 10 => -5.5f
            }, 0), item.transform.rotation) as GameObject;

            obj.GetComponentInChildren<BoostFunctions>().rectangle = item.square;
            // obj.GetComponent<SpriteRenderer>().sortingOrder = 4;
            //obj.GetComponentInChildren<BoostAnimation>().animator.Play();

            //Debug.Log($"Instantiated simple explosion at position {item.transform.position}");
            Instance.ActivatedBoost = BoostType.Empty;
        }

        void ActivateCanonBoostForSquare(Rectangle rectangle)
        {
            //  ArrowEffect(square);
            //Debug.Log("Activated boost: Bomb for Item");
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.boostColorReplace);
            Instance.DragBlocked = true;

            var obj = Instantiate(Resources.Load("Boosts/CanonSpine"), new Vector3(rectangle.transform.position.x, fieldBoards[0].fieldData.maxRows switch
                {
                    <= 3 => -2, 4 => -3f, 5 => -3.5f, 6 => -3.8f, 7 => -4.1f, 8 => -4.5f, 9 => -5, >= 10 => -5.5f
                }, 0),
                rectangle.transform.rotation) as GameObject;

            obj.GetComponentInChildren<BoostFunctions>().rectangle = rectangle;
            // obj.GetComponent<SpriteRenderer>().sortingOrder = 4;
            //obj.GetComponentInChildren<BoostAnimation>().animator.StartPlayback();

            //Debug.Log($"Instantiated simple explosion at position {square.transform.position}");
            Instance.ActivatedBoost = BoostType.Empty;
        }


        void MouseUp(Vector2 pos)
        {
            //Debug.Log("LevelManager : onMouseUp");
            onDragEnd.Invoke();
            if (gameStatus != GameState.Playing && gameStatus != GameState.Tutorial)
                return;
            if (EventSystem.current.IsPointerOverGameObject(-1) && gameStatus == GameState.Playing)
                return;
            if (lastTouchedItem != null)
            {
                //Debug.Log("sallog levelmanager lasttouchitem" + lastTouchedItem);
                if (lastTouchedItem.currentType != ItemsTypes.NONE && lastTouchedItem.currentType != ItemsTypes.Eggs && lastTouchedItem.currentType != ItemsTypes.Pots &&
                    !Instance.DragBlocked)
                {
                    //Debug.Log("sallog tap on item");
                    lastTouchedItem.DestroyItem(true);
                    if (lastTouchedItem.currentType is ItemsTypes.Bomb or ItemsTypes.Chopper or ItemsTypes.DiscoBall or ItemsTypes.RocketHorizontal or ItemsTypes.RocketVertical)
                        ChangeCounter(-1);
                }

                lastTouchedItem.dragThis = false;
                lastTouchedItem.switchDirection = Vector3.zero;
            }
        }

        void MouseDownRight(Vector2 pos)
        {
            if (gameStatus != GameState.Playing && gameStatus != GameState.Tutorial)
                return;
            if (EventSystem.current.IsPointerOverGameObject(-1) && gameStatus == GameState.Playing)
                return;
            var hit = Physics2D.OverlapPoint(pos,
                1 << LayerMask.NameToLayer("Item"));
            if (hit != null)
            {
                var item = hit.gameObject.GetComponent<Item>();
                Camera.main.GetComponent<TestItemChanger>().ShowMenuItems(item);
            }
        }

        private void RestartLevel()
        {
            GameObject gm = new GameObject();
            gm.AddComponent<HandleLevelRestart>();
            string scene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(scene);
        }

        #region RegenerateLevel

        //No matches detected, regenerate level
        public void NoMatches()
        {
            if (field.fieldData.noRegenLevel)
            {
                if (GameStatus == GameState.Playing)
                {
                    noTip = true;
                    // CheckWinLose();
                }

                return;
            }

            StartCoroutine(NoMatchesCor());
        }

        public IEnumerator NoMatchesCor()
        {
            if (gameStatus == GameState.Playing)
            {
                DragBlocked = true;

                CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.noMatch);

                popupWords.ShowGratzWord(GratzWordState.NoMoreMatch);
                gameStatus = GameState.RegenLevel;
                yield return new WaitForSeconds(1);
                ReGenLevel();
            }
        }

        public IEnumerator Shuffle()
        {
            Debug.Log("Starting Shuffle coroutine");
            dragBlocked = true; // Block input during shuffle
            yield return new WaitForSeconds(1.4f);
            Instance.ShuffleBoard(true);
        }

        /// <summary>
        /// Shuffles the items on the current game board.
        /// </summary>
        /// <param name="animate">Whether to animate the shuffle.</param>
        public void ShuffleBoard(bool animate = true)
        {
            if (field == null || (gameStatus != GameState.Playing && gameStatus != GameState.RegenLevel && gameStatus != GameState.Tutorial))
            {
                Debug.LogWarning("Cannot shuffle board: Field is null or game not in a playable state.");
                return;
            }

            Debug.Log("Starting board shuffle...");
            DragBlocked = true; // Block input during shuffle

            // 1. Get eligible squares (type EmptySquare or grass) and their current items
            List<Rectangle> eligibleSquares = field.squaresArray
                .Where(sq => sq != null && sq.Item != null && (sq.type == LevelTargetTypes.EmptySquare || sq.type == LevelTargetTypes.Grass || sq.type == LevelTargetTypes.GrassType2))
                .ToList();

            if (eligibleSquares.Count < 2)
            {
                Debug.LogWarning("Not enough eligible items/squares (Empty or grass) to shuffle.");
                DragBlocked = false;
                return;
            }

            List<Item> itemsToShuffle = eligibleSquares.Select(sq => sq.Item).ToList();

            // 2. Perform Fisher-Yates shuffle on the items list
            int n = itemsToShuffle.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                (itemsToShuffle[k], itemsToShuffle[n]) = (itemsToShuffle[n], itemsToShuffle[k]); // Tuple swap
            }

            // 3. Reassign shuffled items back to eligible squares
            for (int i = 0; i < eligibleSquares.Count; i++)
            {
                Rectangle rectangle = eligibleSquares[i];
                Item newItem = itemsToShuffle[i];

                // Update references
                rectangle.Item = newItem;
                if (newItem != null)
                {
                    newItem.square = rectangle;

                    // 4. Update item position (animated or instant)
                    if (animate)
                    {
                        // Calculate direction and backward position
                        Vector3 targetPosition = rectangle.transform.position;
                        Vector3 currentPosition = newItem.transform.position;
                        Vector3 direction = (targetPosition - currentPosition).normalized;
                        Vector3 backwardPosition = currentPosition - direction * 0.4f; // Move 0.2 units back

                        // Create a sequence for the animation
                        DG.Tweening.Sequence shuffleSequence = DOTween.Sequence();
                        shuffleSequence.Append(newItem.transform.DOMove(backwardPosition, 0.15f).SetEase(DG.Tweening.Ease.OutQuad)); // Short backward move
                        shuffleSequence.Append(newItem.transform.DOMove(targetPosition, 0.35f).SetEase(DG.Tweening.Ease.OutQuad)); // Move to final target
                        shuffleSequence.Play();
                    }
                    else
                    {
                        newItem.transform.position = rectangle.transform.position + Vector3.zero;
                    }
                }
            }

            Debug.Log($"Shuffled {eligibleSquares.Count} items on Empty/grass.");

            // 5. Delay unblocking and check for matches
            StartCoroutine(PostShuffleActions());
        }

        private IEnumerator PostShuffleActions()
        {
            // Wait a short time for animations/visual updates
            yield return new WaitForSeconds(0.6f); // Adjust based on animation duration

            // Process matches for all items after shuffling
            var items = field.GetItems();
            foreach (var item in items)
            {
                if (item != null)
                {
                    item.ProcessMatches();
                }
            }

            DragBlocked = false; // Unblock input

            // 6. Trigger match finding after shuffle
            FindMatches();
        }

        public void ReGenLevel()
        {
            DragBlocked = true;
            //if (gameStatus != GameState.Playing && gameStatus != GameState.RegenLevel)
            //DestroyAnimatedItems();
            //        if (gameStatus == GameState.RegenLevel)
            //            DestroyItems(true);
            StopedAI();
            StartCoroutine(RegenMatches());
            OnLevelLoaded();
        }

        public IEnumerator RegenMatches(bool onlyFalling = false)
        {
            if (gameStatus == GameState.RegenLevel)
            {
                yield return new WaitForSeconds(0.5f);
            }

            if (!onlyFalling)
                field.RegenItems(false);
            else
                Instance.onlyFalling = true;

            var items = field.GetItems();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                    items[i].Hide(true);
            }

            yield return new WaitForFixedUpdate();

            var combs = new List<List<Item>>();
            do
            {
                yield return new WaitingFallingDuration();
                combs = CombineManager.GetCombinedItems(field);
                var chopperItems = ArtificialIntelligence.Instance.GetChopperCombines();
                if (chopperItems != null)
                    combs.Add(chopperItems.items);

                // Filter empty and null lists
                var filteredCombs = new List<List<Item>>();
                for (int i = 0; i < combs.Count; i++)
                {
                    if (combs[i] != null && combs[i].Count > 0)
                        filteredCombs.Add(combs[i]);
                }

                combs = filteredCombs;

                yield return new WaitForEndOfFrame();

                for (int i = 0; i < combs.Count; i++)
                {
                    var comb = combs[i];
                    for (int j = 0; j < comb.Count; j++)
                    {
                        if (comb[j] != null)
                            comb[j].GenColor(_colorGetter);
                    }
                }
            } while (combs.Count > 0);

            SetPreBoosts();
            if (!onlyFalling)
                DragBlocked = false;
            Instance.onlyFalling = false;
            if (gameStatus == GameState.RegenLevel)
                gameStatus = GameState.Playing;

            yield return new WaitForEndOfFrame();

            items = field.GetItems();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item != null && (item.square.type == LevelTargetTypes.EmptySquare || item.square.type == LevelTargetTypes.Grass))
                {
                    var transformLocalScale = item.transform.localScale;
                    item.transform.localScale = Vector3.zero;
                    LeanTween.scale(item.gameObject, transformLocalScale, .3f);
                    item.Hide(false);
                }
            }
        }

        public void DestroyItems(bool withoutEffects = false)
        {
            var items = field.GetItems();
            foreach (var item in items)
            {
                if (item != null)
                {
                    if (item.GetComponent<Item>().currentType != ItemsTypes.Gredient &&
                        item.GetComponent<Item>().currentType == ItemsTypes.NONE)
                    {
                        if (!withoutEffects)
                            item.GetComponent<Item>().DestroyItem();
                        else
                            item.GetComponent<Item>().anim.SetTrigger("disappear");
                    }
                }
            }
        }

        #endregion

        public static List<List<T>> FilterNonEmptyAndNotNull<T>(List<List<T>> list)
        {
            var filteredList = new List<List<T>>();

            // Use WhereNotNull() to filter out null lists first
            var notNullLists = list.WhereNotNull();

            // Then check for non-empty lists
            foreach (var item in notNullLists)
            {
                if (item.Count > 0)
                {
                    filteredList.Add(item);
                }
            }

            return filteredList;
        }


        /// <summary>
        /// Place boosts in the field which bought before in Play menu
        /// </summary>
        private void SetPreBoosts()
        {
            if (BoostPackage > 0)
            {
                Initiations.Instance.SpendBoost(BoostType.Bombs);
                foreach (var item in field.GetRandomItems(BoostPackage))
                {
                    item.NextType = ItemsTypes.Bomb;
                    item.ChangeType(null, true, false);
                }

                BoostPackage = 0;
            }

            if (BoostColorfullBomb > 0)
            {
                Initiations.Instance.SpendBoost(BoostType.DiscoBalls);
                foreach (var item in field.GetRandomItems(BoostColorfullBomb))
                {
                    item.NextType = ItemsTypes.DiscoBall;
                    item.ChangeType(null, true, false);
                }

                BoostColorfullBomb = 0;
            }

            if (BoostStriped > 0)
            {
                Initiations.Instance.SpendBoost(BoostType.Rockets);
                foreach (var item in field.GetRandomItems(BoostStriped))
                {
                    item.NextType = (ItemsTypes)Random.Range(4, 6);
                    item.ChangeType(null, true, false);
                }

                BoostStriped = 0;
            }

            if (BoostChopper > 0)
            {
                Initiations.Instance.SpendBoost(BoostType.Chopper);
                foreach (var item in field.GetRandomItems(BoostChopper))
                {
                    item.NextType = ItemsTypes.Chopper;
                    item.ChangeType(null, true, false);
                }

                BoostStriped = 0;
            }
        }

        /// <summary>
        /// Place boosts in the field which bought before in Play menu New Version
        /// </summary>
        private void SetPreGamePowerUps()
        {
            foreach (var item in field.GetRandomItems(3))
            {
                if (GlobalValue.GetPreGameBooster(BoostType.Rocket))
                {
                    item.NextType = Random.Range(0, 2) == 1 ? ItemsTypes.RocketVertical : ItemsTypes.RocketHorizontal;
                    item.ChangeType(null, true, false);
                    GlobalValue.SetPreGameBooster(BoostType.Rocket, false);
                    GlobalValue.SpendItem(BoostType.Rocket, 1);
                    continue;
                }
                else if (GlobalValue.GetPreGameBooster(BoostType.BombBoostType))
                {
                    item.NextType = ItemsTypes.Bomb;
                    item.ChangeType(null, true, false);
                    GlobalValue.SetPreGameBooster(BoostType.BombBoostType, false);
                    GlobalValue.SpendItem(BoostType.BombBoostType, 1);
                    continue;
                }
                else if (GlobalValue.GetPreGameBooster(BoostType.DiscoBall))
                {
                    item.NextType = ItemsTypes.DiscoBall;
                    item.ChangeType(null, true, false);
                    GlobalValue.SetPreGameBooster(BoostType.DiscoBall, false);
                    GlobalValue.SpendItem(BoostType.DiscoBall, 1);
                    continue;
                }
            }
        }


        //Find matches with delay
        public IEnumerator FindMatchDelay()
        {
            yield return new WaitForSeconds(0.2f);
            FindMatches();
        }

        //start sync matches search then wait while items destroy and fall
        private float lastFindMatchesTime = 0f;
        private const float FIND_MATCHES_COOLDOWN = 0.1f; // Minimum time between calls

        public void StopedAI()
        {
            if (_showTips != null)
            {
                ArtificialIntelligence.Instance.StopCoroutineShowTipsPeriodically();
                squareBoundaryLine.ClearLineInstantly();
                ArtificialIntelligence.Instance.StopTipAnimation();
                StopCoroutine(_showTips);
                _showTips = null;
            }
        }

        public void FindMatches()
        {
            if (Time.time - lastFindMatchesTime < FIND_MATCHES_COOLDOWN)
                return;

            StopedAI();

            if (Falling)
            {
                checkMatchesAgain = true;
                return;
            }

            findMatchesStarted = true;
            lastFindMatchesTime = Time.time;

            Falling = true;
            StartBusyOperation(true);

            if (fallingDownRoutine != null)
                StopCoroutine(fallingDownRoutine);

            fallingDownRoutine = StartCoroutine(FallingDown());
        }


        internal List<In_GameBlocker> _stopFall = new List<In_GameBlocker>();

        public int combo;

        //[HideInInspector]
        public AnimationCurve fallingCurve = AnimationCurve.Linear(0, 10, 1, 10);
        public float waitAfterFall = 0.02f;
        [HideInInspector] private bool collectIngredients;

        public Item lastTouchedItem;
        private int winRewardAmount;
        public bool skipWin;
        private GameObject tapToSkip;
        private bool noTip;
        public bool checkTarget;
        private ManageInput _manageInput;
        private ColorGetter _colorGetter;


        private List<Item> itemsToDestroy = new List<Item>();
        private SquareBoundaryLine squareBoundaryLine;
        private static readonly float checkInterval = 0.1f;
        private float lastCheckTime;
        public bool isFallingDownActive;
        private int loopCounter;

        private void StartFallingDown()
        {
            InitializeFallingProcess();
            // Uncomment the following lines if item destruction logic is required:
            //var destroyItemsListed = GatherItemsToDestroy();
            // yield return HandleItemDestruction(destroyItemsListed);

            destLoopIterations = 0;
            loopCounter = 0;
            lastCheckTime = Time.time;
            isFallingDownActive = true;
        }

        private float fallingCooldownTime = 0.2f;
        private Coroutine fallingCooldownRoutine;
        private Coroutine fallingDebounceRoutine;
        private float fallingDebounceDelay = 0.2f; // Adjust as needed

        public bool _falling;

        public bool Falling
        {
            get
            {
                return _falling; // Return the current value of _falling
            }
            private set
            {
                if (_falling == value) return;

                if (value)
                {
                    // Cancel any pending debounce-off
                    if (fallingDebounceRoutine != null)
                    {
                        StopCoroutine(fallingDebounceRoutine);
                        fallingDebounceRoutine = null;
                    }

                    _falling = true;
                    DragBlocked = true; // Block drag when falling starts
                }
                else
                {
                    // Start debounce-off only if not already running
                    if (fallingDebounceRoutine == null)
                        fallingDebounceRoutine = StartCoroutine(FallingDebounceOff());
                }
            }
        }

        private IEnumerator FallingDebounceOff()
        {
            yield return new WaitForSeconds(fallingDebounceDelay);
            // Only turn off if nothing set it back to true
            if (_falling)
            {
                _falling = false;
                DragBlocked = false; // Unblock drag when falling ends
                EndBusyOperation(true);
            }

            fallingDebounceRoutine = null;
        }

        public bool multicolorWorking = false;
        private int fallingSessionId = 0; // Add this field to track falling sessions

        private IEnumerator FallingCooldown(int sessionId)
        {
            yield return new WaitForSeconds(fallingCooldownTime);

            // Only set Falling = false if no new falling session has started
            if (fallingSessionId == sessionId)
            {
                Falling = false;
                EndBusyOperation(true);
            }

            fallingCooldownRoutine = null;
        }

        private Coroutine fallingDownRoutine; // Add this field
        public bool fallingDownFlag;
        private readonly List<Item> _destroyItemsCache = new List<Item>(50); // Pre-allocate list
        private readonly Func<bool> _stopFallPredicate; // Cache delegate

        // Constructor or Awake to initialize the delegate
        public MainManager()
        {
            _stopFallPredicate = () => StopFall;
        }

        private IEnumerator FallingDown()
        {
            OnMove?.Invoke();
            Instance.thrivingBlockDestroyed = false;
            combo = 0;
            ArtificialIntelligence.Instance.allowShowTip = false;

            bool matchesFoundThisCycle;
            int loopGuard = 0;
            const int maxLoops = 100;

            do
            {
                loopGuard++;
                if (loopGuard > maxLoops)
                {
                    Debug.LogError("FallingDown loop limit exceeded! Breaking.");
                    yield break;
                }

                matchesFoundThisCycle = false;
                checkMatchesAgain = false;

                // Use pre-allocated list instead of LINQ query
                _destroyItemsCache.Clear();
                var items = field.GetItems();
                foreach (var item in items)
                {
                    if (item != null && item.destroyNext)
                    {
                        _destroyItemsCache.Add(item);
                    }
                }

                if (_destroyItemsCache.Count > 0)
                {
                    matchesFoundThisCycle = true;
                    // foreach (var itemToDestroy in _destroyItemsCache) itemToDestroy.destroyNext = false; // Consider if this logic is still needed
                    yield return new WaitUntilPipelineIsDestroyed(_destroyItemsCache, new Delays());
                }

                Debug.LogError("Start Waiting ");
                yield return new WaitDestroyingDuration();
                yield return new WaitWhile(_stopFallPredicate); // Use cached delegate
                yield return new WaitingFallingDuration();
                yield return new WaitCollectingDuration();
                Debug.LogError("End Waiting ");

                // TODO: Consider optimizing CombineManager.GetCombinedItems if it allocates heavily
                // var newMatches = CombineManager.GetCombinedItems(field);
                // if (newMatches.Count > 0)
                // {
                //     matchesFoundThisCycle = true;
                //     combo++;
                //     // foreach (var list in newMatches)
                //     //     foreach (var item in list.WhereNotNull()) // WhereNotNull might still allocate if not optimized
                //     //         //item.destroyNext = true;
                // }
                if (checkMatchesAgain)
                {
                    matchesFoundThisCycle = true;
                }
            } while (matchesFoundThisCycle);

            DragBlocked = false;
            findMatchesStarted = false;

            Falling = false; // Will debounce off

            fallingDownRoutine = null;

            // if (gameStatus == GameState.Playing)
            // {
            //     OnTurnEnd?.Invoke();
            //     CheckWinLose();
            // }
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitializeFallingProcess()
        {
            findMatchesStarted = true;
            Instance.thrivingBlockDestroyed = false;
            combo = 0;
            ArtificialIntelligence.Instance.allowShowTip = false;

            if (squareBoundaryLine == null)
            {
                squareBoundaryLine = FindAnyObjectByType<SquareBoundaryLine>(); // Cache for later use
            }

            squareBoundaryLine.ClearLineInstantly();

            var items = field.GetItems();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item?.anim != null)
                {
                    item.anim.StopPlayback();
                }
            }
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsProcessComplete()
        {
            {
                if (field.DestroyingItemsExist())
                    return false;

                var emptySquares = field.GetEmptySquares();
                int emptyCount = emptySquares.Count;
                for (int i = 0; i < emptyCount; i++)
                {
                    if (emptySquares[i] != null)
                        return false;
                }

                return !checkMatchesAgain;
            }
        }

        private void UpdateTargetCountersIfNecessary()
        {
            // Update target counters only if there are no animations (Avoid LINQ Any())
            if (Instance.animateItems.Count == 0) // Avoid LINQ .Any()
            {
                int counterLength = levelData.TargetCounters.Count;
                for (int i = 0; i < counterLength; i++)
                {
                    levelData.TargetCounters[i].GetCount();
                }

                //Debug.Log("LevelManager: Updated target counters.");
            }
        }

        private int busyOperation = 0;
        private int busyOperationFalling = 0;

        public bool IsAnyPowerUpActive()
        {
            return busyOperation > 0;
        }

        public void StartBusyOperation(bool falling = false)
        {
            if (falling)
            {
                if (busyOperationFalling < 0)
                    busyOperationFalling = 0;
                busyOperationFalling++;
            }
            else
            {
                if (busyOperation < 0)
                    busyOperation = 0;
                busyOperation++;
            }
        }

        private Coroutine _waitforLose;

        public void EndBusyOperation(bool falling = false)
        {
            if (falling)
                busyOperationFalling--;
            else
                busyOperation--;


            if (busyOperation < 0) busyOperation = 0;
            if (busyOperationFalling < 0) busyOperationFalling = 0;
            if (Limit <= 0)
            {
                if (_waitforLose != null)
                {
                    StopCoroutine(_waitforLose);
                    _waitforLose = null;
                }

                _waitforLose = StartCoroutine(WaitingForLosingGame());
            }
        }

        private IEnumerator WaitingForLosingGame()
        {
            yield return new WaitForSeconds(1f);

            if (busyOperation > 0 || busyOperationFalling > 0)
            {
                yield break;
            }

            if (busyOperation == 0 && busyOperationFalling == 0 && Limit == 0)
            {
                CheckWinLose();
            }
        }

        // [MethodImpl(MethodImplOptions.NoInlining)]
        private void HandleComboAndEndTurn()
        {
            if (combo > 2 && gameStatus == GameState.Playing)
            {
                popupWords.ShowGratzWord(GratzWordState.Grats);
                combo = 0;
                OnCombo?.Invoke();
                //TopBarAnimationController.OnTopBarStateChange(TopBarAnimationState.Happy);
            }

            dragBlocked = false;
            findMatchesStarted = false;
            checkMatchesAgain = false;

            if (gameStatus == GameState.Playing)
            {
                if (Instance.animateItems.Count == 0)
                {
                    WaitForAnimationAndEndTurn();
                }
            }
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WaitForAnimationAndEndTurn()
        {
            // Wait until animations and target checks are finished
            OnTurnEnd?.Invoke();
            Instance.CheckWinLose();
        }

        private List<Item> GatherItemsToDestroy()
        {
            var destroyItemsListed = new List<Item>();

            // Gather all items that are marked for destruction
            foreach (var item in field.GetItems())
            {
                if (item != null && item.destroyNext)
                {
                    destroyItemsListed.Add(item);
                    //Debug.Log("LevelManager: Added item to destroy list " + item);
                }
            }

            return destroyItemsListed;
        }

        private IEnumerator HandleItemDestruction(List<Item> destroyItemsListed)
        {
            // If there are items to destroy, start the destruction process asynchronously
            if (destroyItemsListed.Count > 0)
            {
                //Debug.Log("LevelManager: Destroying " + destroyItemsListed.Count + " items.");
                yield return StartCoroutine(DestroyItemsAsync(destroyItemsListed));
            }

            // Wait for various processes related to item destruction and falling to complete
            yield return new WaitDestroyingDuration();
            yield return new WaitWhile(() => StopFall);
            yield return new WaitingFallingDuration();
            yield return new WaitCollectingDuration();
        }

        private IEnumerator DestroyItemsAsync(List<Item> items)
        {
            // Asynchronously destroy items using object pooling
            foreach (var item in items)
            {
                if (item != null)
                {
                    // Implement object pooling destruction logic here
                    // Example: item.DestroyItemAsync();
                }
            }

            yield return null; // Ensure coroutine ends
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckBigBlocksCleared()
        {
            // Check each square on the field to see if any large blocks have been cleared
            field.GetSquares().ForEach(i => i.CheckBigBlockCleared());
        }

        /// <summary>
        /// Get square by position
        /// </summary>
        public Rectangle GetSquare(int col, int row, bool safe = false)
        {
            return field.GetSquare(col, row, safe);
        }

        /// <summary>
        /// Get bunch of squares by row number
        /// </summary>
        public List<Rectangle> GetRowSquare(int row)
        {
            var itemsList = new List<Rectangle>();
            for (var rowIndex = 0; rowIndex <= levelData.maxRows; rowIndex++)
            {
                Rectangle rectangle = GetSquare(rowIndex, row, true);
                if (rectangle.type == LevelTargetTypes.ExtraTargetType3 && Instance.AdditionalSettings.StripedStopByUndestroyable) break;
                if (!rectangle.IsNone())
                    itemsList.Add(rectangle);
            }

            return itemsList;
        }

        /// Get bunch of squares by column number
        public List<Rectangle> GetColumnSquare(int col)
        {
            var itemsList = new List<Rectangle>();
            for (var column = 0; column <= levelData.maxCols; column++)
            {
                Rectangle rectangle = GetSquare(col, column, true);
                if (rectangle.type == LevelTargetTypes.ExtraTargetType3 && Instance.AdditionalSettings.StripedStopByUndestroyable) break;
                if (!rectangle.IsNone())
                    itemsList.Add(rectangle);
            }

            return itemsList;
        }

        /// Get bunch of items by row number
        public List<Item> GetRow(Rectangle rectangle)
        {
            var itemsList = new List<Item>();
            for (var row = rectangle.col; row <= levelData.maxRows; row++)
            {
                var square1 = GetSquare(row, rectangle.row, true);
                if (square1.type == LevelTargetTypes.ExtraTargetType3 && AdditionalSettings.StripedStopByUndestroyable) break;
                itemsList.Add(square1.Item);
            }

            for (var col = rectangle.col; col >= 0; col--)
            {
                var square1 = GetSquare(col, rectangle.row, true);
                if (square1.type == LevelTargetTypes.ExtraTargetType3 && AdditionalSettings.StripedStopByUndestroyable) break;
                itemsList.Add(square1.Item);
            }

            return GetNonNullItems(itemsList);
        }

        /// Get bunch of items by column number
        public List<Item> GetColumn(Rectangle rectangle)
        {
            var itemsList = new List<Item>();
            for (var row = rectangle.row; row < levelData.maxRows; row++)
            {
                var square1 = GetSquare(rectangle.col, row, true);
                if (square1.type == LevelTargetTypes.ExtraTargetType3 && AdditionalSettings.StripedStopByUndestroyable) break;
                itemsList.Add(square1.Item);
            }

            for (var row = rectangle.row; row >= 0; row--)
            {
                var square1 = GetSquare(rectangle.col, row, true);
                if (square1.type == LevelTargetTypes.ExtraTargetType3 && AdditionalSettings.StripedStopByUndestroyable) break;
                itemsList.Add(square1.Item);
            }

            return GetNonNullItems(itemsList);
        }

        public List<Item> GetColumn(int col)
        {
            var itemsList = new List<Item>();
            for (var row = 0; row < levelData.maxRows; row++)
            {
                var square1 = GetSquare(col, row, true);
                itemsList.Add(square1.Item);
            }

            return GetNonNullItems(itemsList);
        }

        public static List<Item> GetNonNullItems(List<Item> itemsList)
        {
            var nonNullItems = new List<Item>();
            foreach (var item in itemsList.WhereNotNull())
            {
                nonNullItems.Add(item);
            }

            return nonNullItems;
        }

        /// <summary>
        /// Get squares around the square
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public List<Rectangle> GetSquaresAroundSquare(Rectangle rectangle, bool isPackageCombined = false)
        {
            int multiplied;
            if (isPackageCombined == true)
                multiplied = 4;
            else
                multiplied = 2;

            var col = rectangle.col;
            var row = rectangle.row;
            var itemsList = new List<Rectangle>();
            for (var r = row - multiplied; r <= row + multiplied; r++)
            {
                for (var c = col - multiplied; c <= col + multiplied; c++)
                {
                    itemsList.Add(GetSquare(c, r, true));
                }
            }

            return itemsList;
        }

        /// <summary>
        /// Get squares around the square
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public List<Rectangle> GetSquaresAroundSquareSpiral(Rectangle rectangle)
        {
            var col = rectangle.col;
            var row = rectangle.row;
            var itemsList = new List<Rectangle>();
            for (var r = row - 1; r <= row + 1; r++)
            {
                for (var c = col - 1; c <= col + 1; c++)
                {
                    itemsList.Add(GetSquare(c, r, true));
                }
            }

            return itemsList;
        }


        /// <summary>
        /// get 9 items around the square
        /// </summary>
        /// <param name="rectangle"></param>
        /// <returns></returns>
        public List<Item> GetItemsAroundSquare(Rectangle rectangle, bool isPackageCombined = false)
        {
            int multiplied;
            if (isPackageCombined == true)
                multiplied = 4;
            else
                multiplied = 2;

            var itemsList = GetItemsAround(rectangle, multiplied);
            itemsList.Add(rectangle.Item);
            return itemsList;
        }

        /// <summary>
        /// Get items around the square.
        /// </summary>
        /// <param name="rectangle">The reference square.</param>
        /// <param name="multiplied">Number of items to search around.</param>
        /// <returns>A list of items around the specified square.</returns>
        public List<Item> GetItemsAround(Rectangle rectangle, int multiplied)
        {
            var col = rectangle.col;
            var row = rectangle.row;
            var itemsList = new List<Item>();

            // Iterate through the 5x5 grid around the square
            for (var r = row - multiplied; r <= row + multiplied; r++)
            {
                for (var c = col - multiplied; c <= col + multiplied; c++)
                {
                    var pos = GetSquare(c, r, true).transform.position;

                    RaycastHit2D[] result = new RaycastHit2D[4 * multiplied]; // Increase raycast capacity for potential multiple hits
                    Physics2D.LinecastNonAlloc(rectangle.transform.position, pos, result, 1 << LayerMask.NameToLayer("Item"));

                    // Get all valid hit items
                    for (int i = 0; i < result.Length; i++)
                    {
                        var hit = result[i];
                        if (hit.transform != null)
                        {
                            var item = hit.transform.GetComponent<Item>();
                            if (item != null && !itemsList.Contains(item))
                            {
                                // Find the insertion point for sorting
                                int j = 0;
                                while (j < itemsList.Count && (hit.transform.position - pos).magnitude >= (itemsList[j].transform.position - pos).magnitude)
                                {
                                    j++;
                                }

                                // Insert the item at the found index
                                itemsList.Insert(j, item);
                            }
                        }
                    }
                }
            }

            return itemsList;
        }


        //striped effect
        public void StripedShow(GameObject obj, Rectangle thisRectangle, bool horizontal, bool destroyNeighbours = false)
        {
            //Debug.LogError("Sadegh StripedShow ");
            //StartBusyOperation();

            if (stripesEffect != null)
            {
                var effect = ObjectPoolManager.Instance.GetPooledObject("StripesEffect", this);

                var objPosition = obj.transform.position;
                effect.transform.position = objPosition;
                //  StartCoroutine(PauseFalling(.5f, obj));

                if (horizontal)
                {
                    RaycastDestroy(objPosition, thisRectangle, Vector3.right, destroyNeighbours, effect, horizontal);
                    RaycastDestroy(objPosition, thisRectangle, Vector3.left, destroyNeighbours, effect, horizontal);
                }

                else
                {
                    StartCoroutine(PauseFalling(.6f, null));
                    effect.transform.Rotate(Vector3.back, 90);
                    RaycastDestroy(objPosition, thisRectangle, Vector3.up, destroyNeighbours, effect, horizontal);
                    RaycastDestroy(objPosition, thisRectangle, Vector3.down, destroyNeighbours, effect, horizontal);
                    // effect.transform.Rotate(Vector3.back, 0f);
                }

                //Destroy(effect, 1);
            }
        }

        bool started = false;
        private int pendingMatches = 0;

        public void ShowDestroyPackage(Rectangle rectangle, bool isPackageCombined = false)
        {
            StartCoroutine(OnPackageAnimationFinished(rectangle, isPackageCombined));
        }

        private IEnumerator OnPackageAnimationFinished(Rectangle rectangle, bool isPackageCombined)
        {
            HapticAndShake(2);
            var effect = ObjectPoolManager.Instance.GetPooledObject("BombParticleLight");
            if (effect != null)
            {
                effect.transform.position = transform.position;
                effect.SetActive(true);
                effect.GetComponent<BackToPool>().StartAnimation();
            }

            OnShakeRequested?.Invoke(0.2f, 0.35f); // Trigger screen shake
            DestroyItems(rectangle.item, rectangle, isPackageCombined);

            yield return new WaitForSeconds(0.4f);
        }

        private void DestroyItems(Item item1, Rectangle rectangle, bool isPackageCombined = false)
        {
            field.DestroyItemsAround(rectangle, null, isPackageCombined);
            var sqList = GetSquaresAroundSquare(rectangle);
            rectangle.DestroyBlock();
            if (rectangle.type == LevelTargetTypes.ExtraTargetType2)
                levelData.GetTargetObject().CheckSquares(sqList.ToArray());
//            item1.destroying = false;
            // item.square.Item = null;
        }

        public CounterHelperClass counter;

        public void ChangeCounter(int i)
        {
            Limit += i;
            Limit = Mathf.Max(0, Limit);
            counter.UpdateText();
            if (Limit == 0)
                StartCoroutine(WaitingForLosingGame());
        }

        public void GameRestartOrGiveNEwHealth()
        {
            //TopBarAnimationController.OnTopBarStateChange(TopBarAnimationState.Idle);
            busyOperation = 0;
            busyOperationFalling = 0;
            gameFinished = false;
        }

        private void RaycastDestroy(Vector3 startPosition, Rectangle thisStrip, Vector3 direction, bool destroyNeighbours = false, GameObject obj = null, bool horizontal = false)
        {
            StartBusyOperation();
            BackToPool pool = null;
            obj?.TryGetComponent(out pool);
            if (pool) pool.StartAnimation();
        }


        public IEnumerator PauseFalling(float duration, GameObject obj)
        {
            if (AddComponentIfMissing<In_GameBlocker>(gameObject))
            {
                yield return new WaitForSeconds(duration);
                RemoveComponent<In_GameBlocker>(gameObject);
            }
        }

        // Arrow effect (no visual effect)
        public void ArrowEffect(Rectangle rectangle, bool isRowEffect)
        {
            StopedAI();
            if (rectangle != null)
            {
                DestroySquaresOneByOne(rectangle, isRowEffect);
            }
        }

        private void DestroySquaresOneByOne(Rectangle rectangle, bool isRowEffect)
        {
            StartCoroutine(DestroySquaresOneByOneCoroutine(rectangle, isRowEffect));
        }

        private IEnumerator DestroySquaresOneByOneCoroutine(Rectangle rectangle, bool isRowEffect)
        {
            // yield return PauseFalling(0.5f, Level);

            // Get the relevant squares based on the direction
            List<Rectangle> squares = isRowEffect ? GetRowSquare(rectangle.row) : GetColumnSquare(rectangle.col);

            // Sort squares based on the required order
            if (isRowEffect)
            {
                squares.Sort((a, b) => a.col.CompareTo(b.col)); // Left to right
            }
            else
            {
                squares.Sort((a, b) => b.row.CompareTo(a.row)); // Down to up
            }

            squares = squares.Distinct().ToList();
            squares.ForEach((x) => Debug.LogError("squareDestroy" + x.name));
            yield return new WaitForSeconds(.1f);
            foreach (var hit in squares)
            {
                if (hit != null)
                {
                    if (hit.item != null)
                    {
                        var item = hit.item;
                        if (item != null)
                        {
                            item.DestroyItem(destroyNeighbours: false);
                        }
                    }
                    else
                    {
                        hit.DestroyBlock(destroyNeighbour: false);
                    }

                    // Add a short pause between each destruction
                    yield return new WaitForSeconds(0.04f);
                }
            }
        }

        //popup score, to use - enable "Popup score" in editor
        public void ShowPopupScore(int value, Vector3 pos, int color)
        {
            // UpdateBar();
            if (showPopupScores)
            {
                var parent = GameObject.Find("CanvasScore").transform;
                var poptxt = Instantiate(popupScore, pos, Quaternion.identity);
                poptxt.transform.GetComponentInChildren<Text>().text = "" + value;
                if (color <= scoresColors.Length - 1)
                {
                    poptxt.transform.GetComponentInChildren<Text>().color = scoresColors[color];
                    poptxt.transform.GetComponentInChildren<Outline>().effectColor = scoresColorsOutline[color];
                }

                poptxt.transform.SetParent(parent);
                //   poptxt.transform.position += Vector3.right * 1;
                poptxt.transform.localScale = Vector3.one / 1.5f;
                Destroy(poptxt, 0.3f);
            }
        }

        void RemoveComponent<T>(GameObject obj) where T : Component
        {
            // Check if the object has the component
            T component = obj.GetComponent<T>();
            if (component != null)
            {
                // Remove the component
                Destroy(component);
                Debug.Log(typeof(T).Name + " component removed.");
            }
            else
            {
                Debug.Log(typeof(T).Name + " component not found.");
            }
        }

        bool AddComponentIfMissing<T>(GameObject obj) where T : Component
        {
            // Check if the object already has the component
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                // Add the component if missing
                obj.AddComponent<T>();
                Debug.Log(typeof(T).Name + " component added.");
                return true;
            }
            else
            {
                Debug.Log(typeof(T).Name + " component already exists.");
                return false;
            }
        }


        /// <summary>
        /// check gained stars
        /// </summary>
        public void CheckStars()
        {
            if (Score >= levelData.star1 && stars <= 0)
            {
                stars = 1;
            }

            if (Score >= levelData.star2 && stars <= 1)
            {
                stars = 2;
            }

            if (Score >= levelData.star3 && stars <= 2)
            {
                stars = 3;
            }
        }

        /// <summary>
        /// load level from
        /// </summary>
        /// <param name="currentLevel"></param>
        public void LoadLevel(int currentLevel)
        {
            gameFinished = false;
            Debug.LogError("LoadLevel  " + currentLevel);
            levelLoaded = false;
            levelData = LoadingController.LoadForPlay(currentLevel, levelData);
            Debug.LogError("LoadLevel levelData.limit " + levelData.limit);

            if (gameStatus != GameState.Map)
            {
                foreach (var fieldData in levelData.fields)
                {
                    var _field = Instantiate(FieldBoardPrefab);
                    var fboard = _field.GetComponent<FieldBoard>();
                    fboard.fieldData = fieldData;
                    fboard.squaresArray = new Rectangle[fieldData.maxCols * fieldData.maxRows];
                    fieldBoards.Add(fboard);
                }
            }

            Limit = levelData.limit;
        }

        public void delayedCall(float sec, Action action)
        {
            StartCoroutine(DelayedCallCor(sec, action));
        }

        IEnumerator DelayedCallCor(float sec, Action action)
        {
            yield return new WaitForSeconds(sec);
            action?.Invoke();
        }

        //////////////////////// Haptic FeedBack
        public static void HapticAndShake(int hapticStrength = 0)
        {
            if (!GlobalValue.IsVibrationOn) return;
            // //if (!GlobalValue.iSvibrate) return;
            // MMVibrationManager.Haptic(hapticStrength switch
            // {
            //     0 => HapticTypes.SoftImpact,
            //     1 => HapticTypes.MediumImpact,
            //     2 => HapticTypes.HeavyImpact,
            //     _ => HapticTypes.SoftImpact
            // }, false, true, THIS);
        }

        public static void PlayButtonClickSound()
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);
        }

        private void UpdateCooldownStatus(bool value, float changeAfterTime, BoostType _boosterType)
        {
            // Callback to decrement AmountOwned in BoostIcon
            BoostInventory[] boostIcons = FindObjectsByType<BoostInventory>(FindObjectsSortMode.None);
            GlobalValue.AddItem(_boosterType, -1);
            Debug.Log("update text used booster" + _boosterType + " " + GlobalValue.GetItem(_boosterType).ToString());
            foreach (var boostIcon in boostIcons)
            {
                if (boostIcon.type == _boosterType)
                {
                    boostIcon.UpdateBoosterText();
                }
            }

            StartCoroutine(CooldownCoroutine(value, changeAfterTime));
        }

        private IEnumerator CooldownCoroutine(bool value, float changeAfterTime)
        {
            yield return new WaitForSeconds(changeAfterTime);
            isInCooldown = value;
            Debug.Log("cooldown isInCooldown: " + isInCooldown);
        }
    }


    public interface IDestroyPipelineStripedShow
    {
        public void DestroyByStriped(bool WithoutShrink = true, bool destroyNeighbours = false);
    }
}