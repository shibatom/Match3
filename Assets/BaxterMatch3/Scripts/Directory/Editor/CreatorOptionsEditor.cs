

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Internal.Scriptable.Rewards;
using Internal.Scripts.Blocks;
using Internal.Scripts.Integrations;
using Internal.Scripts.Items;
using Internal.Scripts.Items.Interfaces;
using Internal.Scripts.Level;
using Internal.Scripts.Level.ItemsPerLevel.Editor;
using Internal.Scripts.MapScripts.StaticMap.Editor;
using Internal.Scripts.System;
using Internal.Scripts.TargetScripts.TargetEditor;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Internal.Scripts.Editor
{
    [InitializeOnLoad]
    public class CreatorOptionsEditor : EditorWindow
    {
        private int levelNumber = 1;
        private int subLevelNumber = 1;

        public int SubLevelNumber
        {
            get { return subLevelNumber; }
            set
            {
                subLevelNumber = value;
                levelData.currentSublevelIndex = subLevelNumber - 1;
            }
        }

        public ReferencerUI ReferencerUI
        {
            get
            {
                if (!_referencerUI) _referencerUI = GameObject.FindObjectOfType<ReferencerUI>();
                return _referencerUI;
            }
            set { _referencerUI = value; }
        }

        private int subLevelNumberTotal = 1;

        private LevelTargetTypes _levelTargetType;
        private EditorItems itemBrush;
        private string FileName = "1_1.txt";
        private Vector2 scrollViewVector;
        private bool update;
        private static int selected;
        private string[] toolbarStrings = { "Level Generator", "Panels" };

        private string[] sectionsString = { "Squares", "Objects", "Way Maker", "Warp", "Generators" };

        private static CreatorOptionsEditor window;
        private bool life_settings_show;
        private bool boost_show;
        private bool failed_settings_show;
        private bool gems_shop_show;
        private IEnumerable<EditorItems>[] itemsForEditor;
        private IEnumerable<EditorItems> itemsForEditorFull;
        string levelPath = "Assets/BaxterMatch3/Directory/Resources/Levels/";

        private bool enableGoogleAdsProcessing;
        private bool enableChartboostAdsProcessing;
        private LevelData levelData;
        private Texture[] arrows = new Texture[6];
        private Texture[] teleports = new Texture[2];
        private Texture[] arrows_enter = new Texture[4];
        private ScriptableObjectLevel _scriptableObjectLevel;
        private UnityAdsHolder unityAdsSettings;

        private Texture[] spawner = new Texture[10];
        private Texture[] Rotation = new Texture[10];
        private Spawners spawnerType;
        private SingularSpawn EditableSpawner;
        private SpawnerState CurrentSpawnerState;
        private IEnumerable<SpawnerState> _SpawnerStates;
        private bool noGenInto = false;

        private enum SpawnerState
        {
            None,
            Add,
            Remove,
            Edit,
            Clear
        }

        [MenuItem("Window/Match 3 Kingdom/Creator Options")]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            window = (CreatorOptionsEditor)GetWindow(typeof(CreatorOptionsEditor), false, "CREATOR OPTIONS");
            window.Show();
        }


        [MenuItem("Window/Match 3 Kingdom/Documentation")]
        public static void Documentation()
        {
            string path = Path.Combine(Application.dataPath, "BaxterMatch3/Match-3-Kingdom Document.pdf");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        /*[MenuItem("Help/Baxter Games")]
        public static void HelpSection()
        {
        }*/

        public static void ShowHelp()
        {
            selected = 7;
        }

        public static void ShowWindow()
        {
            GetWindow(typeof(CreatorOptionsEditor));
        }

        private void OnFocus()
        {
            window = (CreatorOptionsEditor)GetWindow(typeof(CreatorOptionsEditor));
            //adsSettings = Resources.Load<AdManagerScriptable>("Scriptable/AdManagerScriptable");
            sceneSwitcher = Resources.Load<MapSwitcher>("Scriptable/MapSwitcher");
            _scriptableObjectLevel = Resources.Load("Levels/LevelScriptable") as ScriptableObjectLevel;
            targetEditorScriptable =
                AssetDatabase.LoadAssetAtPath<TargetEditorScriptable>(
                    "Assets/BaxterMatch3/Directory/Resources/Levels/TargetEditorScriptable.asset");
            LoadLevel(levelNumber);

            if (levelData != null)
            {
                if (levelData.GetField(subLevelNumber - 1).maxRows <= 0)
                    levelData.GetField(subLevelNumber - 1).maxRows = 9;
                if (levelData.GetField(subLevelNumber - 1).maxCols <= 0)
                    levelData.GetField(subLevelNumber - 1).maxCols = 9;
                Initialize();
            }

            customSkin = Resources.Load("SweetSkin") as GUISkin;
            separateBar = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/BaxterMatch3/Directory/Textures/Items/obstacle-bar.png", typeof(Texture));
            arrows[0] = (Texture)AssetDatabase.LoadAssetAtPath(
                "Assets/BaxterMatch3/Directory/Textures/EditorSprites/arrow.png", typeof(Texture));
            arrows[1] = (Texture)AssetDatabase.LoadAssetAtPath(
                "Assets/BaxterMatch3/Directory/Textures/EditorSprites/arrow_left.png", typeof(Texture));
            arrows[2] = (Texture)AssetDatabase.LoadAssetAtPath(
                "Assets/BaxterMatch3/Directory/Textures/EditorSprites/arrow_right.png", typeof(Texture));
            arrows[3] = (Texture)AssetDatabase.LoadAssetAtPath(
                "Assets/BaxterMatch3/Directory/Textures/EditorSprites/arrow_up.png", typeof(Texture));
            arrows[4] = (Texture)AssetDatabase.LoadAssetAtPath(
                "Assets/BaxterMatch3/Directory/Textures/EditorSprites/circle arrow.png", typeof(Texture));
            arrows[5] = (Texture)AssetDatabase.LoadAssetAtPath(
                "Assets/BaxterMatch3/Directory/Textures/EditorSprites/circle arrows.png", typeof(Texture));

            teleports[0] =
                (Texture)AssetDatabase.LoadAssetAtPath(
                    "Assets/BaxterMatch3/Directory/Textures/EditorSprites/teleport_icon1.png", typeof(Texture));
            teleports[1] =
                (Texture)AssetDatabase.LoadAssetAtPath(
                    "Assets/BaxterMatch3/Directory/Textures/EditorSprites/teleport_icon2.png", typeof(Texture));

            arrows_enter[0] =
                (Texture)AssetDatabase.LoadAssetAtPath("Assets/BaxterMatch3/Directory/Textures/EditorSprites/arrow_red.png",
                    typeof(Texture));
            arrows_enter[1] =
                (Texture)AssetDatabase.LoadAssetAtPath(
                    "Assets/BaxterMatch3/Directory/Textures/EditorSprites/arrow_red_left.png", typeof(Texture));
            arrows_enter[2] =
                (Texture)AssetDatabase.LoadAssetAtPath(
                    "Assets/BaxterMatch3/Directory/Textures/EditorSprites/arrow_red_right.png", typeof(Texture));
            arrows_enter[3] =
                (Texture)AssetDatabase.LoadAssetAtPath("Assets/BaxterMatch3/Directory/Textures/EditorSprites/arrow_red_up.png",
                    typeof(Texture));


            //Added_feature
            spawner[0] =
                (Texture)AssetDatabase.LoadAssetAtPath(
                    "Assets/BaxterMatch3/Directory/Textures/EditorSprites/Dispenser_Top.png", typeof(Texture));
            spawner[1] =
                (Texture)AssetDatabase.LoadAssetAtPath(
                    "Assets/BaxterMatch3/Directory/Textures/EditorSprites/Dispenser_Down.png", typeof(Texture));
            spawner[2] =
                (Texture)AssetDatabase.LoadAssetAtPath(
                    "Assets/BaxterMatch3/Directory/Textures/EditorSprites/Dispenser_Left.png", typeof(Texture));
            spawner[3] =
                (Texture)AssetDatabase.LoadAssetAtPath(
                    "Assets/BaxterMatch3/Directory/Textures/EditorSprites/Dispenser_Right.png", typeof(Texture));

            Rotation[0] =
                (Texture)AssetDatabase.LoadAssetAtPath("Assets/BaxterMatch3/Directory/Textures/EditorSprites/LeftRotation.png",
                    typeof(Texture));
            Rotation[1] =
                (Texture)AssetDatabase.LoadAssetAtPath(
                    "Assets/BaxterMatch3/Directory/Textures/EditorSprites/RightRotation.png", typeof(Texture));


            if (EditorSceneManager.GetActiveScene().name == "GameScene")
            {
                ReferencerUI = FindObjectOfType<ReferencerUI>();

                MainManager lm = Camera.main.GetComponent<MainManager>();
                Initiations initscript = Camera.main.GetComponent<Initiations>();
                /*if (oldList == null)
                {
                    oldList = new List<AdEvents>();
                    oldList.Clear();
                    for (int i = 0; i < adsSettings.adsEvents.Count; i++)
                    {
                        oldList.Add(new AdEvents());
                        oldList[i].adType = adsSettings.adsEvents[i].adType;
                        oldList[i].everyLevel = adsSettings.adsEvents[i].everyLevel;
                        oldList[i].gameEvent = adsSettings.adsEvents[i].gameEvent;
                    }
                }*/
            }

            //gotFocus = true;
        }

        private void OnLostFocus()
        {
            dirtyLevel = true;
            SaveLevel(levelNumber);
            //gotFocus = false;
        }

        private void Initialize()
        {
            unityAdsSettings = Resources.Load<UnityAdsHolder>("Scriptable/UnityAdsID");
            var shader = Shader.Find("Hidden/Internal-Colored");
            mat = new Material(shader);
            subLevelNumberTotal = GetSubLevelsCount();
            if (levelNumber < 1)
                levelNumber = 1;
            life_settings_show = true;
            //score_settings = true;
            boost_show = true;
            failed_settings_show = true;
            gems_shop_show = true;
            //target_description_show = true;
            var num = 0;
            var simpleItem = Resources.Load<GameObject>("Items/Item").GetComponent<SimpleItem>();
            var simpleitems = simpleItem.GetComponent<ColorReciever>().GetSprites(levelNumber)
                .Select(i => new EditorItems
                {
                    Item = simpleItem.gameObject,
                    ItemType = simpleItem.currentType,
                    Texture = i.texture,
                    order = 100
                }).ToList().ForEachY(i =>
                {
                    i.Color = num;
                    num++;
                });
            Resources.LoadAll("Items");
            var bonusItems = Resources.FindObjectsOfTypeAll(typeof(GetEditorIcon))
                .Select(i => (GetEditorIcon)i)
                .GroupBy(i => i.GetType())
                .Select(i => i)
                .SelectMany(group => group)
                .Select(i => new EditorItems
                {
                    Item = i.gameObject,
                    Color = color,
                    ItemType = i.itemType,
                    colors = i.GetComponent<ColorReciever>(),
                    Texture = i.GetComponent<Item>().SprRenderer.FirstOrDefault().sprite.texture,
                    order = 100
                });
            packageTexture = bonusItems.First(i => i.ItemType == ItemsTypes.Bomb).Texture;
            var l1 = bonusItems.ToList();
            // for (int i = 0; i < 5; i++)
            // {
            //     var item = bonusitems.First(x => x.ItemType == ItemsTypes.PACKAGE);
            //     l1.Add(item.DeepCopy());
            // }

            var list = l1.GroupBy(i => i.ItemType).Select(group => group.OrderBy(i => i.Item.name).ToArray()).ToArray()
                .ForEachY(group =>
                {
                    num = 0;
                    group.ForEachY(i =>
                    {
                        i.Color = num;
                        num++;
                    });
                }).SelectMany(i => i).ToArray();

            itemsForEditorFull = simpleitems.Union(list);
            itemsForEditor = simpleitems
                .Union(bonusItems.GroupBy(i => i.ItemType).SelectMany(i => i))
                .Where(item => item.ItemType != ItemsTypes.Eggs && item.ItemType != ItemsTypes.Pots)
                .GroupBy(item => item.Texture)
                .Select(g => g.First())
                .Select((item, index) => new { Index = index, Value = item })
                .GroupBy(x => x.Index / 6)
                .Select(g => g.Select(x => x.Value))
                .ToArray();

            if (levelData.target.prefabs.All(i => i.GetComponent<ColorReciever>()))
            {
                for (var index = 0; index < levelData.subTargetsContainers.Count; index++)
                {
                    var levelDataSubTargetsContainer = levelData.subTargetsContainers[index];
                    levelDataSubTargetsContainer.extraObject = levelDataSubTargetsContainer.targetPrefab
                        .GetComponent<ColorReciever>().GetSprite(levelNumber, index);
                }
            }

            winReward = Resources.Load<WinReward>("Scriptable/WinReward");

            _squareTypeItems = EnumUtil.GetValues<LevelTargetTypes>();

            _SpawnerStates = EnumUtil.GetValues<SpawnerState>();

            _squareTypeItems = EnumUtil.GetValues<LevelTargetTypes>();
            squareTypeGroupped = _squareTypeItems.Select((x, i) => new { Index = i, Value = x }).GroupBy(i => i.Index / 6)
                .Select(i => i.Select(x => x.Value))
                .ToArray();
            var squareTypeItems = _squareTypeItems;
            var squareTypeses = squareTypeItems as LevelTargetTypes[] ?? squareTypeItems.ToArray();
            squareTextures = new Texture2DSize[squareTypeses.Count()];
            for (var index = 0; index < squareTypeses.Length; index++)
            {
                LevelTargetTypes levelTargetTypeItem = squareTypeses[index];
                if (levelTargetTypeItem == LevelTargetTypes.NONE) continue;
                var squareTexture = Rectangle.GetSquareTexture(levelTargetTypeItem);
                squareTextures[index] = squareTexture;
            }
        }

        private void InitializeSublevel()
        {
            levelData.GetField(subLevelNumber - 1).levelSquares =
                new RectangleBlocks[levelData.GetField(subLevelNumber - 1).maxCols *
                                    levelData.GetField(subLevelNumber - 1).maxRows];
            for (int i = 0; i < levelData.GetField(subLevelNumber - 1).levelSquares.Length; i++)
            {
                RectangleBlocks sqBlocks = new RectangleBlocks();
                sqBlocks.block = LevelTargetTypes.EmptySquare;
                sqBlocks.obstacle = LevelTargetTypes.NONE;

                levelData.GetField(subLevelNumber - 1).levelSquares[i] = sqBlocks;
            }

            ResetDirection();
        }

        private void OnGUI()
        {
            InitStyles();

            if (!levelData.fields.Any())
            {
                OnFocus();
                return;
            }

            //		if (!gotFocus) return;
            UnityEngine.GUI.changed = false;


            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            int oldSelected = selected;
            selected = GUILayout.Toolbar(selected, toolbarStrings, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            scrollViewVector =
                EditorGUILayout.BeginScrollView(scrollViewVector, GUILayout.Width(window.position.width));

            if (oldSelected != selected)
                scrollViewVector = Vector2.zero;

            if (selected == 0 && levelData.fields.Any())
            {
                if (levelData != null)
                {
                    //                if (EditorSceneManager.GetActiveScene().name == "game")
                    {
                        GUILevelSelector();
                        GUILayout.Space(10);

                        GUIStars();
                        GUILayout.Space(10);

                        //GUISwitchSubLevel();
                        //GUILayout.Space(15);

                        GUILimit();
                        GUILayout.Space(10);

                        GUIColorLimit();
                        levelData.SpawnerExits = false;
                        noGenInto = false;
                        levelData.generateIngredientOnlyFromSpawner = false;
                        levelData.GetField(subLevelNumber - 1).bombTimer = 15;
                        //BombTimerGui();

                        GUILayout.Space(10);

                        GUILayout.BeginHorizontal();
                        {
                            //                        GUIMamalade();
                            //                        GUILayout.Space(10);

                            GUINoRegen();
                            //                        GUILayout.Space(10);
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.Space(10);


                        GUITarget();
                        GUILayout.Space(10);

                        GUILevelSize();
                        GUILayout.Space(15);

                        PlayLevelGUI();
                        GUILayout.Space(10);

                        GUISections();
                        GUILayout.Space(10);

                        if (section == 0)
                        {
                            GUIBlocks();
                            GUILayout.Space(10);
                        }

                        if (section == 1)
                        {
                            GUIItems();
                            GUILayout.Space(10);
                        }

                        if (section == 2)
                        {
                            GUIDirections();
                            GUILayout.Space(10);
                        }

                        if (section == 3)
                        {
                            GUITeleport();
                            GUILayout.Space(10);
                        }

                        //Added_feature
                        if (section == 4)
                        {
                            GUISpawners();
                            GUILayout.Space(10);
                        }

                        if (!EditorApplication.isPlaying)
                            GUIGameField();
                    }
                    //                else
                    //                    GUIShowWarning();
                }
            }
            else
            {
                if (EditorSceneManager.GetActiveScene().name == "GameScene" ||
                    EditorSceneManager.GetActiveScene().name == "gameStatic")
                    GUIDialogs();
                else
                    GUIShowWarning();
            }
            /*else if (selected == 1)
            {
                if (EditorSceneManager.GetActiveScene().name == "game" ||
                    EditorSceneManager.GetActiveScene().name == "gameStatic")
                    GUISettings();
                else
                    GUIShowWarning();

                GUILayout.Space(10);
                // CheckSeparateLevels();
            }
            else if (selected == 2)
            {
                if (EditorSceneManager.GetActiveScene().name == "game" ||
                    EditorSceneManager.GetActiveScene().name == "gameStatic")
                    GUIShops();
                else
                    GUIShowWarning();
            }
            else if (selected == 3)
            {
                if (EditorSceneManager.GetActiveScene().name == "game" ||
                    EditorSceneManager.GetActiveScene().name == "gameStatic")
                    GUIInappSettings();
                else
                    GUIShowWarning();
            }
            else if (selected == 4)
            {
                GUIAds();
            }
            else if (selected == 5)
            {
                if (EditorSceneManager.GetActiveScene().name == "game" ||
                    EditorSceneManager.GetActiveScene().name == "gameStatic")
                    GUIDialogs();
                else
                    GUIShowWarning();
            }
            else if (selected == 6)
            {
                if (EditorSceneManager.GetActiveScene().name == "game" ||
                    EditorSceneManager.GetActiveScene().name == "gameStatic")
                    GUIRate();
                else
                    GUIShowWarning();
            }
            else if (selected == 7)
            {
                //GUIHelp();
            }*/


            EditorGUILayout.EndScrollView();
            if (UnityEngine.GUI.changed && !EditorApplication.isPlaying)
                EditorSceneManager.MarkAllScenesDirty();

            if (enableGoogleAdsProcessing)
                RunOnceGoogle();

            if (enableChartboostAdsProcessing)
                RunOnceChartboost();
        }

        private void BombTimerGui()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Bomb timer", GUILayout.Width(80));
            GUILayout.Space(70);
            levelData.GetField(subLevelNumber - 1).bombTimer = EditorGUILayout.IntField("",
                levelData.GetField(subLevelNumber - 1).bombTimer, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            //spawner exits var, enable if any spawner, its handy job
            GUILayout.BeginHorizontal();
            GUILayout.Label("Spawner Exits", GUILayout.Width(100));
            GUILayout.Space(50);
            levelData.SpawnerExits = EditorGUILayout.Toggle(levelData.SpawnerExits, GUILayout.Width(30));
            GUILayout.Space(5);
            GUILayout.Label("<-- Enable only if you set ingredient as spawner.", GUILayout.Width(400));
            noGenInto = EditorGUILayout.Toggle(noGenInto, GUILayout.Width(30));
            GUILayout.Space(5);
            GUILayout.Label("<-- no Gen INto", GUILayout.Width(400));
            GUILayout.Space(50);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ingredient Generate Only From Spawner.", GUILayout.Width(230));
            GUILayout.Space(50);
            levelData.generateIngredientOnlyFromSpawner = EditorGUILayout.Toggle(levelData.generateIngredientOnlyFromSpawner, GUILayout.Width(50));
            GUILayout.Space(5);
            GUILayout.Label("<-- Enable only if you want to generate ingredient only from spawner.",
                GUILayout.Width(400));
            GUILayout.Space(50);
            GUILayout.EndHorizontal();
        }


        private void CheckSeparateLevels()
        {
            // if (GUILayout.Button("Re-generate separate levels", GUILayout.Width(300)))
            // {
            //     var ls = Resources.Load("Levels/LevelScriptable") as LevelScriptable;
            //     for (int i = 0; i < ls.levels.Count(); i++)
            //     {
            //         ScriptableLevelManager.CreateFileLevel( i+1, ls.levels[i]);
            //     }
            // }
        }


        private void GUIShowWarning()
        {
            GUILayout.Space(100);
            GUILayout.Label("Attention!", EditorStyles.boldLabel, GUILayout.Width(600));
            GUILayout.Label("Please open scene - game ( Assets/BaxterMatch3/Directory/Scenes/GameScene.unity )", EditorStyles.boldLabel,
                GUILayout.Width(600));
        }


        #region GUIRate

        private void GUIRate()
        {
            Initiations initscript = Camera.main.GetComponent<Initiations>();

            GUILayout.Label("Rate settings:", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            initscript.ShowRateEvery = EditorGUILayout.IntField("Show Rate every ", initscript.ShowRateEvery,
                GUILayout.Width(220), GUILayout.MaxWidth(220));
            GUILayout.Label(" level (0 = disable)", EditorStyles.label, GUILayout.Width(150));
            GUILayout.EndHorizontal();
            initscript.RateURL =
                EditorGUILayout.TextField("URL", initscript.RateURL, GUILayout.Width(220), GUILayout.MaxWidth(220));
            initscript.RateURLIOS = EditorGUILayout.TextField("URL iOS", initscript.RateURLIOS, GUILayout.Width(220),
                GUILayout.MaxWidth(220));
        }

        #endregion

        #region GUIDialogs

        private void GUIDialogs()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("Tabs & Panels", _headerStyle);
                GUILayout.Space(5);

                ShowMenuButton("Play Panel", "PlayPanel");
                ShowMenuButton("Complete Panel", "CompletePanel");
                ShowMenuButton("Failed Panel", "FailedPanel");
                ShowMenuButton("PreFailed Panel", "PreFailedPanel");
                ShowMenuButton("Buy Boost", "BuyBoost");
                ShowMenuButton("Life Refill Panel", "LifeRefillPopup");
                ShowMenuButton("Shop Panel", "ShopPanel");
                ShowMenuButton("Settings Panel", "SettingsPanel");
                ShowMenuButton("RateUs Panel", "RateUsPanel");
            }
            GUILayout.EndVertical();
        }

        private void ShowMenuButton(string label, string name)
        {
            GUILayout.BeginVertical(GUILayout.Width(120));
            {
                GUILayout.Label(label, _labelStyle, GUILayout.Width(120));

                GameObject obj = ReferencerUI.transform.Find(name).gameObject;

                string buttonText = obj.activeSelf ? "Disappear" : "Appear";
                if (GUILayout.Button(buttonText, _buttonStyle, GUILayout.Width(100)))
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeGameObject = obj;
                    obj.SetActive(!obj.activeSelf);
                }
            }
            GUILayout.EndVertical();
        }


        public static void SetSearchFilter(string filter, int filterMode)
        {
            SearchableEditorWindow[] windows =
                (SearchableEditorWindow[])Resources.FindObjectsOfTypeAll(typeof(SearchableEditorWindow));
            SearchableEditorWindow hierarchy = null;
            foreach (SearchableEditorWindow window in windows)
            {
                if (window.GetType().ToString() == "UnityEditor.SceneHierarchyWindow")
                {
                    hierarchy = window;
                    break;
                }
            }

            if (hierarchy == null)
                return;

            MethodInfo setSearchType =
                typeof(SearchableEditorWindow).GetMethod("SetSearchFilter",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = { filter, filterMode, false };

            setSearchType.Invoke(hierarchy, parameters);
        }

        #endregion

        #region ads_settings

        private void RunOnceGoogle()
        {
            if (Directory.Exists("Assets/PlayServicesResolver"))
            {
                Debug.Log("assets try reimport");
#if GOOGLE_MOBILE_ADS && UNITY_ANDROID
//            GooglePlayServices.PlayServicesResolver.MenuResolve();//2.1.2
            Debug.Log("assets reimorted");
            enableGoogleAdsProcessing = false;
#endif
            }
        }

        private void RunOnceChartboost()
        {
            enableChartboostAdsProcessing = false;
        }


        /*private void GUIAds()
        {
            MainManager lm = Camera.main.GetComponent<MainManager>();
            Initiations initscript = Camera.main.GetComponent<Initiations>();

            GUILayout.Label("Ads settings:", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.BeginHorizontal();

            GUILayout.Label("Unity ads", EditorStyles.boldLabel, GUILayout.Width(150)); //1.6.1
            // GUILayout.Label("Install: Windows->\n Services->Ads - OFF", GUILayout.Width(130));
            if (GUILayout.Button("Help", GUILayout.Width(80)))
            {
                Application.OpenURL("https://docs.google.com/document/d/1HeN8JtQczTVetkMnd8rpSZp_TZZkEA7_kan7vvvsMw0");
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            //		}
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUI.BeginChangeCheck();
            unityAdsSettings.enable =
                EditorGUILayout.Toggle("Enable Unity ads ", unityAdsSettings.enable, GUILayout.Width(220));
            GUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                // if (unityAdsSettings.enable) DefineSymbolsUtils.AddSymbol("UNITY_ADS");
                // else DefineSymbolsUtils.DeleteSymbol("UNITY_ADS");
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUI.BeginChangeCheck();
            unityAdsSettings.androidID =
                EditorGUILayout.TextField("Android ID ", unityAdsSettings.androidID, GUILayout.Width(220));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            unityAdsSettings.iOSID = EditorGUILayout.TextField("iOS ID", unityAdsSettings.iOSID, GUILayout.Width(220));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            unityAdsSettings.unityRewardedAndroid = EditorGUILayout.TextField("Android rewarded ad unit ID", unityAdsSettings.unityRewardedAndroid, GUILayout.Width(320));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            unityAdsSettings.unityInterstitialAndroid = EditorGUILayout.TextField("Android interstitial ad unit ID", unityAdsSettings.unityInterstitialAndroid, GUILayout.Width(320));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            unityAdsSettings.unityRewardediOS = EditorGUILayout.TextField("iOS rewarded ad unit ID", unityAdsSettings.unityRewardediOS, GUILayout.Width(320));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            unityAdsSettings.unityInterstitialiOS = EditorGUILayout.TextField("iOS interstitial ad unit ID", unityAdsSettings.unityInterstitialiOS, GUILayout.Width(320));

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(unityAdsSettings);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            initscript.rewardedGems = EditorGUILayout.IntField("Rewarded gems count", initscript.rewardedGems,
                GUILayout.Width(200), GUILayout.MaxWidth(200));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

#if GOOGLE_MOBILE_ADS
            GUILayout.BeginHorizontal();

            GUILayout.Label("Google mobile ads", EditorStyles.boldLabel, GUILayout.Width(150)); //1.6.1
            if (GUILayout.Button("Install", GUILayout.Width(100)))
            {
                Application.OpenURL("https://github.com/googleads/googleads-mobile-unity/releases"); //1.6.1
            }

            if (GUILayout.Button("Help", GUILayout.Width(80)))
            {
                Application.OpenURL(
                    "https://docs.google.com/document/d/1I69mo9yLzkg35wtbHpsQd3Ke1knC5pf7G1Wag8MdO-M/edit?usp=sharing");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            adsSettings.admobUIDAndroid = EditorGUILayout.TextField("Admob Interstitial ID Android ",
                adsSettings.admobUIDAndroid, GUILayout.Width(220), GUILayout.MaxWidth(220));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            adsSettings.admobUIDIOS = EditorGUILayout.TextField("Admob Interstitial ID iOS", adsSettings.admobUIDIOS,
                GUILayout.Width(220), GUILayout.MaxWidth(220));
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();//2.2
            GUILayout.Space(20);
            adsSettings.admobRewardedUIDAndroid =
 EditorGUILayout.TextField("Admob Rewarded ID Android ", adsSettings.admobRewardedUIDAndroid, new GUILayoutOption[] {
                GUILayout.Width (220),
                GUILayout.MaxWidth (220)
            });
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            adsSettings.admobRewardedUIDIOS =
 EditorGUILayout.TextField("Admob Rewarded ID iOS", adsSettings.admobRewardedUIDIOS, new GUILayoutOption[] {
                GUILayout.Width (220),
                GUILayout.MaxWidth (220)
            });
            GUILayout.EndHorizontal();
            // adsSettings.admobRewardedPriority = EditorGUILayout.Toggle("show admob rewarded first ", adsSettings.admobRewardedPriority);
            // GUILayout.Space(10);
#endif

            //CHARTBOOST ADS
//        GUILayout.BeginHorizontal();
//        bool oldenableChartboostAds = initscript.enableChartboostAds;
//        //		initscript.enableChartboostAds = EditorGUILayout.Toggle ("Enable Chartboost Ads", initscript.enableChartboostAds, new GUILayoutOption[] {//1.6.1
//        //			GUILayout.Width (50),
//        //			GUILayout.MaxWidth (200)
//        //		});
//        GUILayout.Label("Chartboost ads", EditorStyles.boldLabel, GUILayout.Width(150)); //1.6.1
//        if (GUILayout.Button("Install", GUILayout.Width(100)))
//        {
//            Application.OpenURL("http://www.chartboo.st/sdk/unity"); //1.6.1
//        }
//
//        if (GUILayout.Button("Help", GUILayout.Width(80)))
//        {
//            Application.OpenURL("https://docs.google.com/document/d/1ibnQbuxFgI4izzyUtT45WH5m1ab3R5d1E3ke3Wrb10Y");
//        }
//
//        GUILayout.EndHorizontal();

//        GUILayout.Space(10);
//        //		if (oldenableChartboostAds != initscript.enableChartboostAds) {//1.6.1
//        //			SetScriptingDefineSymbols ();
//        //			if (initscript.enableChartboostAds) {
//        //				enableChartboostAdsProcessing = true;
//        //			}
//        //
//        //		}
//        if (initscript.enableChartboostAds)
//        {
//            GUILayout.BeginHorizontal();
//            GUILayout.Space(20);
//            EditorGUILayout.LabelField("menu Chartboost->Edit settings", GUILayout.Width(50), GUILayout.MaxWidth(200));
//            GUILayout.EndHorizontal();
//            GUILayout.BeginHorizontal();
//            GUILayout.Space(20);
//            EditorGUILayout.LabelField("Put ad ID to appropriate platform to prevent crashing!", EditorStyles.boldLabel,
//                GUILayout.Width(100), GUILayout.MaxWidth(400));
//            GUILayout.EndHorizontal();
//
//            GUILayout.Space(10);
//        }


            GUILayout.Space(10);

            GUILayout.Label("Ads controller:", EditorStyles.boldLabel, GUILayout.Width(150));

            EditorGUILayout.Space();

            GUILayout.Label("Event:               Status:                            Show every:",
                GUILayout.Width(350));

            foreach (AdEvents item in adsSettings.adsEvents)
            {
                EditorGUILayout.BeginHorizontal();
                item.gameEvent = (GameState)EditorGUILayout.EnumPopup(item.gameEvent, GUILayout.Width(100));
                item.adType = (AdType)EditorGUILayout.EnumPopup(item.adType, GUILayout.Width(150));
                item.everyLevel = EditorGUILayout.IntPopup(item.everyLevel, new[]
                {
                    "1",
                    "2",
                    "3",
                    "4",
                    "5",
                    "6",
                    "7",
                    "8",
                    "9",
                    "10"
                }, new[]
                {
                    1,
                    2,
                    3,
                    4,
                    5,
                    6,
                    7,
                    8,
                    9,
                    10
                }, GUILayout.Width(100));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                AdEvents adevent = new AdEvents();
                adevent.everyLevel = 1;
                adsSettings.adsEvents.Add(adevent);
            }

            if (GUILayout.Button("Delete"))
            {
                if (adsSettings.adsEvents.Count > 0)
                    adsSettings.adsEvents.Remove(adsSettings.adsEvents[adsSettings.adsEvents.Count - 1]);
            }


            GUILayout.Space(10);

            if (GUILayout.Button("Save"))
            {
                EditorUtility.SetDirty(adsSettings);
                AssetDatabase.SaveAssets();
            }

            GUILayout.EndHorizontal();
        }*/

        private string PendingItemDrawer(Rect position, string itemValue)
        {
            // Text fields do not like null values!
            if (itemValue == null)
                itemValue = "";

            position.width -= 50;
            itemValue = EditorGUI.TextField(position, itemValue);

            position.x = position.xMax + 5;
            position.width = 45;
            if (UnityEngine.GUI.Button(position, "Info"))
            {
            }

            return itemValue;
        }

        private void DrawEmpty()
        {
            GUILayout.Label("No items in list.", EditorStyles.miniLabel);
        }

        #endregion

        /*
        #region inapps_settings

        private void GUIInappSettings()
        {
            LevelManager lm = Camera.main.GetComponent<LevelManager>();

            GUILayout.Label("In-apps settings:", EditorStyles.boldLabel, GUILayout.Width(150));

            if (GUILayout.Button("Reset to default", GUILayout.Width(150)))
            {
                ResetInAppsSettings();
            }


            GUILayout.Space(10);
            bool oldenableInApps = lm.enableInApps;

            GUILayout.BeginHorizontal();
            //		lm.enableInApps = EditorGUILayout.Toggle ("Enable In-apps", lm.enableInApps, new GUILayoutOption[] {//1.6.1
            //			GUILayout.Width (180)
            //		});
            if (GUILayout.Button("How to setup", GUILayout.Width(120)))
            {
                Application.OpenURL(
                    "https://docs.google.com/document/d/1HeN8JtQczTVetkMnd8rpSZp_TZZkEA7_kan7vvvsMw0#bookmark=id.b1efplsspes5");
            }

            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label("Install: Windows->Services->\n In-app Purchasing - ON->Import", GUILayout.Width(400));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            //		if (oldenableInApps != lm.enableInApps) {1.6.1
            //			SetScriptingDefineSymbols ();
            //		}


            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            GUILayout.BeginVertical();
            for (int i = 0; i < lm.InAppIDs.Length; i++)
            {
                lm.InAppIDs[i] = EditorGUILayout.TextField("Product id " + (i + 1), lm.InAppIDs[i],
                    GUILayout.Width(300),
                    GUILayout.MaxWidth(300));
            }

            GUILayout.Space(10);

            GUILayout.Label("Android:", EditorStyles.boldLabel, GUILayout.Width(150));

            GUILayout.BeginHorizontal();
            GUILayout.Space(30);

            GUILayout.BeginVertical();
            GUILayout.Space(10);
            //GUILayout.Label(" Put Google license key into the field \n from the google play account ", EditorStyles.label, new GUILayoutOption[] { GUILayout.Width(300) });
            //GUILayout.Space(10);

            //lm.GoogleLicenseKey = EditorGUILayout.TextField("Google license key", lm.GoogleLicenseKey, new GUILayoutOption[] {
            //    GUILayout.Width (300),
            //    GUILayout.MaxWidth (300)
            //});

            GUILayout.Space(10);
            if (GUILayout.Button("Android account help", GUILayout.Width(400)))
            {
                Application.OpenURL("http://developer.android.com/google/play/billing/billing_admin.html");
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            GUILayout.BeginVertical();

            GUILayout.Space(10);
            GUILayout.Label("iOS:", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);

            GUILayout.BeginVertical();

            //GUILayout.Label(" StoreKit library must be added \n to the XCode project, generated by Unity ", EditorStyles.label, new GUILayoutOption[] { GUILayout.Width(300) });
            GUILayout.Space(10);
            if (GUILayout.Button("iOS account help", GUILayout.Width(400)))
            {
                Application.OpenURL("https://developer.apple.com/library/ios/qa/qa1329/_index.html");
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void ResetInAppsSettings()
        {
            LevelManager lm = Camera.main.GetComponent<LevelManager>();
            lm.InAppIDs[0] = "gems10";
            lm.InAppIDs[1] = "gems50";
            lm.InAppIDs[2] = "gems100";
            lm.InAppIDs[3] = "gems150";
        }

        #endregion

        #region settings

        private bool share_settings;
        private bool target_settings;

        private void GUISettings()
        {
            LevelManager lm = Camera.main.GetComponent<LevelManager>(); //TODO: move all game settings to scriptabble
            InitScript initscript = Camera.main.GetComponent<InitScript>();
            GUILayout.Label("Game settings:", EditorStyles.boldLabel, GUILayout.Width(150));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to default", GUILayout.Width(150)))
            {
                ResetSettings();
            }

            if (GUILayout.Button("Clear player prefs", GUILayout.Width(150)))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("Player prefs cleared");
            }

            if (GUILayout.Button("Open all levels", new GUILayoutOption[] { GUILayout.Width(150) }))
            {
                for (int i = 1; i < 1000; i++)
                {
                    SaveLevelStarsCount(i, 3);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            bool oldFacebookEnable = lm.FacebookEnable;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Facebook", EditorStyles.boldLabel, GUILayout.Width(150)); //1.6.1
            if (GUILayout.Button("Install", GUILayout.Width(70)))
            {
                Application.OpenURL("https://developers.facebook.com/docs/unity/downloads");
            }

            if (GUILayout.Button("Account", GUILayout.Width(70)))
            {
                Application.OpenURL("https://developers.facebook.com");
            }

            if (GUILayout.Button("How to setup", GUILayout.Width(120)))
            {
                Application.OpenURL(
                    "https://docs.google.com/document/d/1bTNdM3VSg8qu9nWwO7o7WeywMPhVLVl8E_O0gMIVIw0/edit?usp=sharing");
            }

            GUILayout.EndHorizontal();

#if GAMESPARKS
            GUILayout.BeginHorizontal();
            {
                GUILayout.Space(150);
//                if (GUILayout.Button("Create game", GUILayout.Width(100)))
//                {
//                    GamesparksConfiguration window = CreateInstance<GamesparksConfiguration>();
//                    window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 200);
//                    window.ShowPopup();
//                }

            }
            GUILayout.EndHorizontal();
#endif
//#if FACEBOOK
//        share_settings = EditorGUILayout.Foldout(share_settings, "Share settings:");
//        if (share_settings)
//        {
//            GUILayout.BeginHorizontal();
//            GUILayout.Space(30);
//            GUILayout.BeginVertical();
//            {
//                lm.androidSharingPath =
// EditorGUILayout.TextField("Android path", lm.androidSharingPath, GUILayout.MaxWidth(500));
//                lm.iosSharingPath =
// EditorGUILayout.TextField("iOS path", lm.iosSharingPath, GUILayout.MaxWidth(500));
//            }
//            GUILayout.EndVertical();
//            GUILayout.EndHorizontal();
//
//            GUILayout.Space(10);
//        }
//#endif
            GUILayout.BeginHorizontal();
            GUILayout.Label("Leadboard Gamesparks", EditorStyles.boldLabel, GUILayout.Width(150)); //1.6.1
            if (GUILayout.Button("Install", GUILayout.Width(70)))
            {
                Application.OpenURL("https://docs.gamesparks.com/sdk-center/unity.html");
            }

            if (GUILayout.Button("Account", GUILayout.Width(70)))
            {
                Application.OpenURL("https://portal.gamesparks.net");
            }

            if (GUILayout.Button("How to setup", GUILayout.Width(120)))
            {
                Application.OpenURL("https://docs.google.com/document/d/1JcQfiiD2ALz6v_i9UIcG93INWZKC7z6FHXH_u6w9A8E");
            }

            GUILayout.EndHorizontal();


            //		if (oldFacebookEnable != lm.FacebookEnable) {//1.6.1
            //			SetScriptingDefineSymbols ();
            //		}
            if (lm.FacebookEnable)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                GUILayout.Label("menu Facebook-> Edit settings", GUILayout.Width(300));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(30);
            var lastRect = GUILayoutUtility.GetLastRect();
            lm.fallingCurve = EditorGUI.CurveField(new Rect(lastRect.x + 3, lastRect.y + 30, position.width - 50, 25),
                "Falling curve", lm.fallingCurve);
            GUILayout.Space(30);

            lm.waitAfterFall = EditorGUILayout.FloatField("Delay after fall", lm.waitAfterFall, GUILayout.Width(50),
                GUILayout.MaxWidth(200));
            GUILayout.Space(10);

//        score_settings = EditorGUILayout.Foldout(score_settings, "Score settings:");
//        if (score_settings)
//        {
//            GUILayout.Space(10);
//            GUILayout.BeginHorizontal();
//            GUILayout.Space(30);
//            GUILayout.BeginVertical();
//            lm.scoreForItem = EditorGUILayout.IntField("Score for item", lm.scoreForItem, GUILayout.Width(50),
//                GUILayout.MaxWidth(200));
//            lm.scoreForBlock = EditorGUILayout.IntField("Score for block", lm.scoreForBlock, GUILayout.Width(50),
//                GUILayout.MaxWidth(200));
//            lm.scoreForWireBlock = EditorGUILayout.IntField("Score for wire block", lm.scoreForWireBlock,
//                GUILayout.Width(50), GUILayout.MaxWidth(200));
//            lm.scoreForBreakableBox = EditorGUILayout.IntField("Score for Breakable Box", lm.scoreForBreakableBox,
//                GUILayout.Width(50), GUILayout.MaxWidth(200));
//            lm.scoreForThrivingBlock = EditorGUILayout.IntField("Score for thriving block", lm.scoreForThrivingBlock,
//                GUILayout.Width(50), GUILayout.MaxWidth(200));
//            GUILayout.Space(10);
//
//            lm.showPopupScores = EditorGUILayout.Toggle("Show popup scores", lm.showPopupScores, GUILayout.Width(50),
//                GUILayout.MaxWidth(200));
//            GUILayout.Space(10);
//
//            lm.scoresColors[0] = EditorGUILayout.ColorField("Score color item 1", lm.scoresColors[0],
//                GUILayout.Width(200), GUILayout.MaxWidth(200));
//            lm.scoresColors[1] = EditorGUILayout.ColorField("Score color item 2", lm.scoresColors[1],
//                GUILayout.Width(200), GUILayout.MaxWidth(200));
//            lm.scoresColors[2] = EditorGUILayout.ColorField("Score color item 3", lm.scoresColors[2],
//                GUILayout.Width(200), GUILayout.MaxWidth(200));
//            lm.scoresColors[3] = EditorGUILayout.ColorField("Score color item 4", lm.scoresColors[3],
//                GUILayout.Width(200), GUILayout.MaxWidth(200));
//            lm.scoresColors[4] = EditorGUILayout.ColorField("Score color item 5", lm.scoresColors[4],
//                GUILayout.Width(200), GUILayout.MaxWidth(200));
//            lm.scoresColors[5] = EditorGUILayout.ColorField("Score color item 6", lm.scoresColors[5],
//                GUILayout.Width(200), GUILayout.MaxWidth(200));
//            GUILayout.Space(10);
//
//            lm.scoresColorsOutline[0] = EditorGUILayout.ColorField("Score color outline item 1",
//                lm.scoresColorsOutline[0], GUILayout.Width(200), GUILayout.MaxWidth(200));
//            lm.scoresColorsOutline[1] = EditorGUILayout.ColorField("Score color outline item 2",
//                lm.scoresColorsOutline[1], GUILayout.Width(200), GUILayout.MaxWidth(200));
//            lm.scoresColorsOutline[2] = EditorGUILayout.ColorField("Score color outline item 3",
//                lm.scoresColorsOutline[2], GUILayout.Width(200), GUILayout.MaxWidth(200));
//            lm.scoresColorsOutline[3] = EditorGUILayout.ColorField("Score color outline item 4",
//                lm.scoresColorsOutline[3], GUILayout.Width(200), GUILayout.MaxWidth(200));
//            lm.scoresColorsOutline[4] = EditorGUILayout.ColorField("Score color outline item 5",
//                lm.scoresColorsOutline[4], GUILayout.Width(200), GUILayout.MaxWidth(200));
//            lm.scoresColorsOutline[5] = EditorGUILayout.ColorField("Score color outline item 6",
//                lm.scoresColorsOutline[5], GUILayout.Width(200), GUILayout.MaxWidth(200));
//            GUILayout.EndVertical();
//            GUILayout.EndHorizontal();
//        }
//
//        GUILayout.Space(20);

            life_settings_show = EditorGUILayout.Foldout(life_settings_show, "Lifes settings:");
            if (life_settings_show)
            {
                GUILayout.Space(10);
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                GUILayout.BeginVertical();


                initscript.CapOfLife = EditorGUILayout.IntField("Max of lifes", initscript.CapOfLife,
                    GUILayout.Width(200),
                    GUILayout.MaxWidth(200));
                GUILayout.Space(10);

                GUILayout.Label("Total time for refill lifes:", EditorStyles.label);
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                GUILayout.Label("Hour", EditorStyles.label, GUILayout.Width(50));
                GUILayout.Label("Min", EditorStyles.label, GUILayout.Width(50));
                GUILayout.Label("Sec", EditorStyles.label, GUILayout.Width(50));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                initscript.TotalTimeForRestLifeHours =
                    EditorGUILayout.FloatField("", initscript.TotalTimeForRestLifeHours, GUILayout.Width(50));
                initscript.TotalTimeForRestLifeMin =
                    EditorGUILayout.FloatField("", initscript.TotalTimeForRestLifeMin, GUILayout.Width(50));
                initscript.TotalTimeForRestLifeSec =
                    EditorGUILayout.FloatField("", initscript.TotalTimeForRestLifeSec, GUILayout.Width(50));
                GUILayout.EndHorizontal();
                GUILayout.Space(10);


                lm.lifeShop.CostIfRefill = EditorGUILayout.IntField("Cost of refilling lifes", lm.lifeShop.CostIfRefill,
                    GUILayout.Width(200), GUILayout.MaxWidth(200));
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(20);

            initscript.FirstGems = EditorGUILayout.IntField("Start gems", initscript.FirstGems, GUILayout.Width(200),
                GUILayout.MaxWidth(200));
            GUILayout.Space(20);

            EditorGUI.BeginChangeCheck();
            winReward.winRewardAmount = EditorGUILayout.IntField("Win reward gems", winReward.winRewardAmount,
                GUILayout.Width(200),
                GUILayout.MaxWidth(200));
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(winReward);
                AssetDatabase.SaveAssets();
            }

            GUILayout.Space(20);

            initscript.losingLifeEveryGame = EditorGUILayout.Toggle("Losing a life every game",
                initscript.losingLifeEveryGame, GUILayout.Width(200), GUILayout.MaxWidth(200));
            GUILayout.Space(20);


            failed_settings_show = EditorGUILayout.Foldout(failed_settings_show, "Failed settings:");
            if (failed_settings_show)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                GUILayout.BeginVertical();

                lm.FailedCost =
                    EditorGUILayout.IntField(new GUIContent("Cost of continue", "Cost of continue after failed"),
                        lm.FailedCost, GUILayout.Width(200), GUILayout.MaxWidth(200));
                lm.ExtraFailedMoves = EditorGUILayout.IntField(
                    new GUIContent("Extra moves", "Extra moves after continue"),
                    lm.ExtraFailedMoves, GUILayout.Width(200), GUILayout.MaxWidth(200));
                lm.ExtraFailedSecs =
                    EditorGUILayout.IntField(new GUIContent("Extra seconds", "Extra seconds after continue"),
                        lm.ExtraFailedSecs, GUILayout.Width(200), GUILayout.MaxWidth(200));
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(20);

            //  EditorUtility.SetDirty(lm);
        }

        private void ResetSettings()
        {
            LevelManager lm = Camera.main.GetComponent<LevelManager>();
            lm.showPopupScores = false;
            lm.scoresColors[0] = new Color(251f / 255f, 80 / 255f, 1 / 255f);
            lm.scoresColors[1] = new Color(255 / 255f, 193 / 255f, 22 / 255f);
            lm.scoresColors[2] = new Color(237 / 255f, 13 / 255f, 233 / 255f);
            lm.scoresColors[3] = new Color(41 / 255f, 157 / 255f, 255 / 255f);
            lm.scoresColors[4] = new Color(255 / 255f, 255 / 255f, 38 / 255f);
            lm.scoresColors[5] = new Color(37 / 255f, 219 / 255f, 0 / 255f);

            lm.scoresColorsOutline[0] = new Color(138 / 255f, 1 / 255f, 0 / 255f);
            lm.scoresColorsOutline[1] = new Color(184 / 255f, 77 / 255f, 1 / 255f);
            lm.scoresColorsOutline[2] = new Color(128 / 255f, 0 / 255f, 128 / 255f);
            lm.scoresColorsOutline[3] = new Color(0 / 255f, 64 / 255f, 182 / 255f);
            lm.scoresColorsOutline[4] = new Color(174 / 255f, 104 / 255f, 0 / 255f);
            lm.scoresColorsOutline[5] = new Color(19 / 255f, 111 / 255f, 0 / 255f);

            InitScript initscript = Camera.main.GetComponent<InitScript>();
            initscript.CapOfLife = 5;
            initscript.TotalTimeForRestLifeHours = 0;
            initscript.TotalTimeForRestLifeMin = 15;
            initscript.TotalTimeForRestLifeSec = 0;
            lm.lifeShop.CostIfRefill = 12;
            lm.FailedCost = 12;
            lm.ExtraFailedMoves = 5;
            lm.ExtraFailedSecs = 30;
            EditorUtility.SetDirty(lm);
        }

        #endregion

        public void SaveLevelStarsCount(int level, int starsCount)
        {
            Debug.Log(string.Format("Stars count {0} of level {1} saved.", starsCount, level));
            PlayerPrefs.SetInt(GetLevelKey(level), starsCount);
        }

        private string GetLevelKey(int number)
        {
            return string.Format("Level.{0:000}.StarsCount", number);
        }

        #region shop

        private void GUIShops()
        {
            LevelManager lm = Camera.main.GetComponent<LevelManager>();

            GUILayout.Label("Shop settings:", EditorStyles.boldLabel, GUILayout.Width(150));

            if (GUILayout.Button("Reset to default", GUILayout.Width(150)))
            {
                ResetShops();
            }

            GUILayout.Space(10);
            gems_shop_show = EditorGUILayout.Foldout(gems_shop_show, "Gems shop settings:");
            if (gems_shop_show)
            {
                int i = 1;
                foreach (GemProduct item in lm.gemsProducts)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.BeginVertical();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    GUILayout.Label("Gems count", GUILayout.Width(100));
                    GUILayout.Label("Price $", GUILayout.Width(100));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    item.count =
                        EditorGUILayout.IntField("", item.count, GUILayout.Width(100), GUILayout.MaxWidth(100));
                    item.price =
                        EditorGUILayout.FloatField("", item.price, GUILayout.Width(100), GUILayout.MaxWidth(100));
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                    i++;
                }
            }

            /*GUILayout.Space(10);
            boost_show = EditorGUILayout.Foldout(boost_show, "Boosts shop settings:");
            if (boost_show)
            {
                BoostShop bs = MenuReference.BoostShop.GetComponent<BoostShop>();
                List<BoostProduct> bp = bs.boostProducts;
                foreach (BoostProduct item in bp)
                {
                    GUILayout.BeginVertical();
                    {
                        item.boostType = (BoostType)EditorGUILayout.EnumPopup(item.boostType, GUILayout.Width(93));

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Description");
                            item.description =
                                EditorGUILayout.TextField("", item.description, GUILayout.Width(400),
                                    GUILayout.MaxWidth(400));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Localization ref", GUILayout.Width(110));
                            item.descriptionLocalizationRefrence =
                                EditorGUILayout.IntField("", item.descriptionLocalizationRefrence, GUILayout.Width(30),
                                    GUILayout.MaxWidth(30));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(item.icon.texture, GUILayout.Width(50), GUILayout.Height(50));
                            GUILayout.BeginVertical();
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    GUILayout.Label("Count", EditorStyles.label, GUILayout.Width(80));
                                    GUILayout.Label("Price(gem)", EditorStyles.label, GUILayout.Width(80));
                                }
                                GUILayout.EndHorizontal();

                                GUILayout.BeginHorizontal();
                                {
                                    item.count = EditorGUILayout.IntField("", item.count, GUILayout.Width(80),
                                        GUILayout.MaxWidth(80));
                                    item.GemPrices =
                                        EditorGUILayout.IntField("", item.GemPrices, GUILayout.Width(80),
                                            GUILayout.MaxWidth(80));
                                }
                                GUILayout.EndHorizontal();
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                }
            }#1#
        }

        private void ResetShops()
        {
            LevelManager lm = Camera.main.GetComponent<LevelManager>();

            lm.gemsProducts[0].count = 10;
            lm.gemsProducts[0].price = 0.99f;
            lm.gemsProducts[1].count = 50;
            lm.gemsProducts[1].price = 4.99f;
            lm.gemsProducts[2].count = 100;
            lm.gemsProducts[2].price = 9.99f;
            lm.gemsProducts[3].count = 150;
            lm.gemsProducts[3].price = 14.99f;

            BoostShop bs = MenuReference.BoostShop.GetComponent<BoostShop>();
            bs.boostProducts[0].description = "Gives you the 5 extra moves";
            bs.boostProducts[1].description = "Place this special item in game";
            bs.boostProducts[2].description = "Place this special item in game";
            bs.boostProducts[3].description = "Gives you the 30 extra seconds";
            bs.boostProducts[4].description = "Destroy the item";
            bs.boostProducts[5].description = "Place this special item in game";
            bs.boostProducts[6].description = "Switch to item that don't match";
            bs.boostProducts[7].description = "Replace the near items color";

            for (int i = 0; i < 8; i++)
            {
                bs.boostProducts[i].count = 3;

                bs.boostProducts[i].GemPrices = 5;
            }

            EditorUtility.SetDirty(lm);
            EditorUtility.SetDirty(bs);
        }

        #endregion
        */

        #region leveleditor

        private void TestLevel(bool playNow = true, bool testByPlay = true)
        {
            dirtyLevel = true;
            SaveLevel(levelNumber);
            if (EditorSceneManager.GetActiveScene().name != sceneSwitcher.GetSceneName())
                EditorSceneManager.OpenScene("Assets/BaxterMatch3/Directory/Scenes/" + sceneSwitcher.GetSceneName() + ".unity");
            MainManager lm = Camera.main.GetComponent<MainManager>();
            PlayerPrefs.SetInt("OpenLevelTest", levelNumber);
            HelperScripts.GlobalValue.CurrentLevel = levelNumber;
            PlayerPrefs.Save();

            if (playNow)
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.isPlaying = false;
                else
                    EditorApplication.isPlaying = true;
            }
        }


        private GUIStyle _headerStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _labelStyle;

        private void InitStyles()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), _referencerUI.GUIColor);

            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal = { textColor = _referencerUI.GUIHeaderColor }
                };

                _labelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    normal = { textColor = Color.white }
                };

                _buttonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    fontSize = 12,
                    fixedHeight = 25,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(4, 4, 2, 2),
                    padding = new RectOffset(6, 6, 4, 4)
                };
                //_buttonStyle.normal.textColor = Color.green;
                //_buttonStyle.normal.textColor = new Color(.55f,.9f,0);
                _buttonStyle.normal.textColor = _referencerUI.GUIButtonColor;
            }
        }

        private void GUILevelSelector()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("CREATOR OPTIONS", _headerStyle);
                GUILayout.Space(10);

                // Level Section
                GUILayout.Label("Level Control", _headerStyle);
                GUILayout.BeginHorizontal("box");
                {
                    if (GUILayout.Button("Previous", _buttonStyle, GUILayout.Width(80)))
                    {
                        PreviousLevel();
                        OpenTarget();
                    }

                    string changeLvl = GUILayout.TextField(levelNumber.ToString(), GUILayout.Width(50));
                    try
                    {
                        int newLevel = int.Parse(changeLvl);
                        if (newLevel != levelNumber)
                        {
                            subLevelNumber = 1;
                            if (LoadLevel(newLevel))
                            {
                                levelNumber = newLevel;
                                OpenTarget();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // You could show a warning message here
                    }

                    if (GUILayout.Button("Next", _buttonStyle, GUILayout.Width(80)))
                    {
                        NextLevel();
                        OpenTarget();
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(new GUIContent("+", "Add Level"), _buttonStyle, GUILayout.Width(30)))
                        AddLevel();

                    if (GUILayout.Button(new GUIContent("-", "Remove Current Level"), _buttonStyle, GUILayout.Width(30)))
                        RemoveLevel();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(15);

                // SubLevel Section
                /*GUILayout.Label("Sub-Level Control", _headerStyle);
                GUILayout.BeginHorizontal("box");
                {
                    if (GUILayout.Button("Previous", _buttonStyle, GUILayout.Width(80)))
                        PreviousSubLevel();

                    GUILayout.Label($"{SubLevelNumber} / {subLevelNumberTotal}", _labelStyle, GUILayout.Width(80));

                    if (GUILayout.Button("Next", _buttonStyle, GUILayout.Width(80)))
                        NextSubLevel();

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(new GUIContent("+", "Add SubLevel"), _buttonStyle, GUILayout.Width(30)))
                        AddSubLevel();

                    if (GUILayout.Button(new GUIContent("-", "Remove SubLevel"), _buttonStyle, GUILayout.Width(30)))
                        RemoveSubLevel();
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(15);*/
            }
            GUILayout.EndVertical();
        }


        private void GUILevelSize()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("Level Size", _headerStyle);
                GUILayout.Space(5);

                int oldValue = levelData.GetField(subLevelNumber - 1).maxRows +
                               levelData.GetField(subLevelNumber - 1).maxCols;

                var field = levelData.GetField(subLevelNumber - 1);

                // Columns
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Columns", _labelStyle, GUILayout.Width(100));
                    field.maxCols = (int)GUILayout.HorizontalSlider(field.maxCols, 3, 10, GUILayout.Width(100));
                    field.maxCols = EditorGUILayout.IntField(field.maxCols, GUILayout.Width(60));
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                // Rows
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Rows", _labelStyle, GUILayout.Width(100));
                    field.maxRows = (int)GUILayout.HorizontalSlider(field.maxRows, 3, 10, GUILayout.Width(100));
                    field.maxRows = EditorGUILayout.IntField(field.maxRows, GUILayout.Width(60));
                }
                GUILayout.EndHorizontal();

                // Validation
                if (field.maxRows < 3) field.maxRows = 3;
                if (field.maxCols < 3) field.maxCols = 3;
                if (field.maxCols > 11) field.maxCols = 11;
                // You can re-enable this line if needed:
                // if (field.maxRows > 11) field.maxRows = 11;

                if (oldValue != field.maxRows + field.maxCols)
                {
                    Initialize();
                    InitializeSublevel();
                    ClearLevel();
                    dirtyLevel = true;
                }

                GUILayout.Space(10);
                if (GUILayout.Button("Save", _buttonStyle, GUILayout.Width(80)))
                {
                    dirtyLevel = true;
                    SaveLevel(levelNumber);
                }
            }
            GUILayout.EndVertical();
        }

        private void GUISwitchSubLevel()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("Sub Level Behavior", _headerStyle);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent("Switch if No Match Available", "Switch Sub Level If No Matches Are Possible"), _labelStyle, GUILayout.Width(200));

                    bool toggle = EditorGUILayout.Toggle(levelData.GetField().switchSublevelNoMatch, GUILayout.Width(30));

                    if (toggle != levelData.GetField().switchSublevelNoMatch)
                    {
                        levelData.GetField().switchSublevelNoMatch = toggle;
                        dirtyLevel = true;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }


        private void GUILimit()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("Level Limit Settings", _headerStyle);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Limit Type", _labelStyle, GUILayout.Width(80));

                    LIMIT previousLimitType = levelData.limitType;
                    int previousLimitValue = levelData.limit;

                    levelData.limitType = (LIMIT)EditorGUILayout.EnumPopup(levelData.limitType, GUILayout.Width(100));

                    GUILayout.Space(10);

                    if (levelData.limitType == LIMIT.MOVES)
                    {
                        GUILayout.Label("Moves", _labelStyle, GUILayout.Width(50));
                        levelData.limit = EditorGUILayout.IntField(levelData.limit, GUILayout.Width(50));
                    }
                    else
                    {
                        GUILayout.Label("Time (mm:ss)", _labelStyle, GUILayout.Width(90));

                        GUILayout.BeginHorizontal();
                        {
                            int minutes = levelData.limit / 60;
                            int seconds = levelData.limit % 60;

                            minutes = EditorGUILayout.IntField(minutes, GUILayout.Width(30));
                            GUILayout.Label(":", _labelStyle, GUILayout.Width(10));
                            seconds = EditorGUILayout.IntField(seconds, GUILayout.Width(30));

                            levelData.limit = Mathf.Max(1, minutes * 60 + seconds); // Ensure limit is at least 1
                        }
                        GUILayout.EndHorizontal();
                    }

                    if (previousLimitType != levelData.limitType || previousLimitValue != levelData.limit)
                        dirtyLevel = true;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void GUIColorLimit()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("Items Color Count", _headerStyle);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    int previous = levelData.colorLimit;

                    GUILayout.Label("Color Count", _labelStyle, GUILayout.Width(100));
                    levelData.colorLimit = (int)GUILayout.HorizontalSlider(levelData.colorLimit, 2, 6, GUILayout.Width(100));
                    levelData.colorLimit = EditorGUILayout.IntField(levelData.colorLimit, GUILayout.Width(50));

                    levelData.colorLimit = Mathf.Clamp(levelData.colorLimit, 2, 6);

                    if (levelData.colorLimit != previous)
                        dirtyLevel = true;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }


        private void GUIStars()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("Stars Threshold", _headerStyle);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Points for:", _labelStyle, GUILayout.Width(100));

                    int s;

                    // Star 1
                    GUILayout.Label("★1 ", _labelStyle, GUILayout.Width(25));
                    s = EditorGUILayout.IntField(levelData.star1, GUILayout.Width(50));
                    if (s != levelData.star1)
                    {
                        levelData.star1 = Mathf.Max(1, s);
                        dirtyLevel = true;
                    }

                    // Star 2
                    GUILayout.Label("★2 ", _labelStyle, GUILayout.Width(25));
                    s = EditorGUILayout.IntField(levelData.star2, GUILayout.Width(50));
                    if (s != levelData.star2)
                    {
                        levelData.star2 = Mathf.Max(levelData.star1 + 10, s);
                        dirtyLevel = true;
                    }

                    // Star 3
                    GUILayout.Label("★3 ", _labelStyle, GUILayout.Width(25));
                    s = EditorGUILayout.IntField(levelData.star3, GUILayout.Width(50));
                    if (s != levelData.star3)
                    {
                        levelData.star3 = Mathf.Max(levelData.star2 + 10, s);
                        dirtyLevel = true;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }


        private void GUINoRegen()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("Gameplay Flags", _headerStyle);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    // Don't Regen
                    GUILayout.Label(new GUIContent("Don't Regen", "Don't regenerate if no matches possible"), _labelStyle, GUILayout.Width(100));
                    bool noRegen = EditorGUILayout.Toggle(levelData.GetField().noRegenLevel, GUILayout.Width(30));
                    if (noRegen != levelData.GetField().noRegenLevel)
                    {
                        levelData.GetField().noRegenLevel = noRegen;
                        dirtyLevel = true;
                    }

                    GUILayout.Space(40);

                    // Scroll
                    GUILayout.Label(new GUIContent("Scroll", "Scroll level to hide squares with no target items"), _labelStyle, GUILayout.Width(100));
                    bool scroll = EditorGUILayout.Toggle(levelData.GetField().scrollLevel, GUILayout.Width(30));
                    if (scroll != levelData.GetField().scrollLevel)
                    {
                        levelData.GetField().scrollLevel = scroll;
                        dirtyLevel = true;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }


        void UpdateTarget(int newselectedTarget, TargetLevel targetLevel, TargetContainer target)
        {
            // levelData.SetTarget(newselectedTarget);
            targetLevel.ChangeTarget(target, newselectedTarget, levelData, targetEditorScriptable);
            dirtyLevel = true;
        }

        private void GUITarget()
        {
            GUILayout.Space(5);

            // === Target Section ===
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("Target Settings", _headerStyle);
                GUILayout.Space(5);

                var targetLevel = GetTargetLevel();
                var subTargets = targetLevel.targets;
                var targetObjects = subTargets.GroupBy(i => i.targetType.type).FirstOrDefault();
                int selectedTarget = targetObjects?.FirstOrDefault().targetType.type ?? 0;
                int newSelectedTarget = selectedTarget;

                // Target dropdown and editor button
                GUILayout.BeginHorizontal();
                {
                    newSelectedTarget = EditorGUILayout.Popup(selectedTarget, levelData.GetTargetsNames(), GUILayout.Width(120));
                    GUILayout.Space(10);
                    if (GUILayout.Button("EDIT TARGETS", _buttonStyle, GUILayout.Width(160)))
                    {
                        OpenTarget();
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                // Target Count Editors
                if (targetObjects != null)
                {
                    TargetContainer target = targetObjects.FirstOrDefault().targetType.GetTarget();
                    if (target.setCount == SetCount.Manually)
                    {
                        GUILayout.BeginVertical("box");
                        {
                            GUILayout.Label("Target Counts", _labelStyle);
                            foreach (var targetObject in targetObjects)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    var spriteName = targetObject.sprites.FirstOrDefault()?.icon?.name ?? "Item";
                                    GUILayout.Label(spriteName, _labelStyle, GUILayout.Width(80));

                                    EditorGUI.BeginChangeCheck();
                                    targetObject.CountDrawer.count = EditorGUILayout.IntField(targetObject.CountDrawer.count, GUILayout.Width(60));
                                    if (EditorGUI.EndChangeCheck())
                                        targetLevel.saveData();
                                }
                                GUILayout.EndHorizontal();
                            }
                        }
                        GUILayout.EndVertical();
                    }
                }

                // Handle target change
                if (newSelectedTarget != selectedTarget || update)
                {
                    update = false;
                    UpdateTarget(newSelectedTarget, targetLevel, targetObjects?.FirstOrDefault()?.targetType?.GetTarget());
                }
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);
        }

        private void PlayLevelGUI()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("PLAY LEVEL", _buttonStyle, GUILayout.Width(500)))
                    TestLevel();
            }
            GUILayout.EndHorizontal();
        }


        string[] GetTutorialNames() => new[]
        {
            "Disabled", "SIMPLE", ItemsTypes.RocketHorizontal.ToString(), ItemsTypes.Bomb.ToString(),
            ItemsTypes.TimeBomb.ToString()
        };

        private void OpenTarget()
        {
            var asset = PrepareTargetLevel();

            Selection.activeObject = asset;
        }

        private TargetLevel PrepareTargetLevel()
        {
            var asset = GetTargetLevel();
            if (asset == null)
            {
                asset = CreateInstance<TargetLevel>();
                asset.name = "Level" + levelNumber;
                string assetPathAndName =
                    AssetDatabase.GenerateUniqueAssetPath("Assets/BaxterMatch3/Directory/Resources/Levels/Targets/TargetLevel" +
                                                          levelNumber + ".asset");
                AssetDatabase.CreateAsset(asset, assetPathAndName);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return asset;
        }

        private TargetLevel GetTargetLevel()
        {
            var asset = Resources.Load<TargetLevel>("Levels/Targets/TargetLevel" + levelNumber);
            return asset;
        }


        private bool HideAnotherTargetBlock(string blockName)
        {
            var targets = levelData.GetTargetsNames();
            var currentTarget = levelData.target.name;
            if (IsBlockTarget(blockName, targets) && !blockName.Contains(currentTarget) && blockName.Contains("grass"))
                return true;
            return false;
        }

        private bool IsBlockTarget(string blockName, string[] targets)
        {
            var list = targets.Where(i => blockName.Contains(i));
            return list.Count() > 0;
        }

        private void GUISections()
        {
            GUILayout.BeginVertical("box");
            {
                GUILayout.Label("Editor Sections", _headerStyle);
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();
                {
                    var prevSection = section;
                    section = GUILayout.Toolbar(section, sectionsString, GUILayout.Width(500));

                    if (section != prevSection && (section == 3 || section == 4))
                    {
                        _rectangleBlockSelected = null;
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }


        private void GUITeleport()
        {
            GUILayout.BeginHorizontal();
            {
                //GUILayout.Space(30);
                GUILayout.BeginVertical();
                {
                    if (GUILayout.Button(new GUIContent("Clear", "clear all teleports"), GUILayout.Width(50),
                            GUILayout.Height(50)))
                    {
                        _rectangleBlockSelected = null;
                        for (int i = 0; i < levelData.GetField(subLevelNumber - 1).levelSquares.Length; i++)
                        {
                            levelData.GetField(subLevelNumber - 1).levelSquares[i].isEnterTeleport = false;
                            levelData.GetField(subLevelNumber - 1).levelSquares[i].teleportCoordinatesLinkedBack =
                                Vector2Int.one * -1;
                            levelData.GetField(subLevelNumber - 1).levelSquares[i].teleportCoordinatesLinked =
                                Vector2Int.one * -1;
                        }

                        dirtyLevel = true;
                        // SaveLevel();
                    }

                    GUILayout.Space(10);

                    GUILayout.Label("Click to the field, once for enter then for exit.", EditorStyles.boldLabel);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        //Added_feature
        private int Column, Row = -1;

        private void GUISpawners()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Spawners:", EditorStyles.boldLabel);
                    GUILayout.BeginHorizontal();
                    {
                        UnityEngine.GUI.color = new Color(1, 1, 1, 1f);
                        foreach (SpawnerState States in _SpawnerStates)
                        {
                            if (CurrentSpawnerState == States && (CurrentSpawnerState != SpawnerState.None &&
                                                                  CurrentSpawnerState != SpawnerState.Clear))
                                UnityEngine.GUI.backgroundColor = Color.gray;
                            if (GUILayout.Button(
                                    new GUIContent(States.ToString() + "", "changes to " + States.ToString() + " State"),
                                    GUILayout.Width(80), GUILayout.Height(50)))
                            {
                                CurrentSpawnerState = States;
                                //Debug.Log("" + CurrentSpawnerState);

                                switch (CurrentSpawnerState)
                                {
                                    case SpawnerState.Add:
                                    {
                                    }
                                        break;
                                    case SpawnerState.Clear:
                                    {
                                        for (int i = 0;
                                             i < levelData.GetField(subLevelNumber - 1).levelSquares.Length;
                                             i++)
                                        {
                                            levelData.GetField(subLevelNumber - 1).levelSquares[i].spawners =
                                                new List<SingularSpawn>();
                                        }

                                        dirtyLevel = true;
                                    }
                                        break;
                                    case SpawnerState.Edit:
                                    {
                                    }
                                        break;
                                    case SpawnerState.None:
                                    {
                                    }
                                        break;
                                    case SpawnerState.Remove:
                                    {
                                    }
                                        break;
                                    default:
                                        break;
                                }

                                Column = Row = -1;
                            }

                            UnityEngine.GUI.backgroundColor = Color.white;
                        }

                        UnityEngine.GUI.color = new Color(1, 1, 1, 1f);
                    }
                    GUILayout.EndHorizontal();

                    if (CurrentSpawnerState == SpawnerState.Edit)
                        if (Column >= 0 && Row >= 0)
                        {
                            RectangleBlocks rectangleBlock = levelData.GetBlock(Row, Column);
                            if (rectangleBlock != null)
                            {
                                if (rectangleBlock.spawners.Count > 0)
                                {
                                    GUILayout.BeginHorizontal(GUILayout.Width(100));
                                    {
                                        //Dispenser Rotating Option
                                        GUILayout.BeginHorizontal(GUILayout.Width(100));
                                        {
                                            GUILayout.Label(" " + rectangleBlock.spawners[0].SpawnId,
                                                GUILayout.Width(100));
                                            if (GUILayout.Button(new GUIContent(Rotation[0], " Rotate Left"),
                                                    GUILayout.Width(50), GUILayout.Height(50)))
                                            {
                                                rectangleBlock.spawners[0].rotationType =
                                                    CheckForRotation(rectangleBlock.spawners[0].rotationType, true);
                                            }

                                            if (GUILayout.Button(new GUIContent(Rotation[1], " Rotate right"),
                                                    GUILayout.Width(50), GUILayout.Height(50)))
                                            {
                                                rectangleBlock.spawners[0].rotationType =
                                                    CheckForRotation(rectangleBlock.spawners[0].rotationType, false);
                                            }
                                        }
                                        GUILayout.EndHorizontal();

                                        //Selecting Spawner Type and selecting persentage of spawning

                                        GUILayout.BeginVertical(GUILayout.Width(100));
                                        {
                                            //make sure only one ingredient Dispenser
                                            if (rectangleBlock.spawners[0].SpawnersType == Spawners.Ingredient)
                                            {
                                                if (rectangleBlock.spawners[0].SpawnersType_2 == Spawners.Ingredient)
                                                    rectangleBlock.spawners[0].SpawnersType_2 = Spawners.None;
                                                else if (rectangleBlock.spawners[0].IngredentSpawner_01.Ingredient_01 &&
                                                         rectangleBlock.spawners[0].IngredentSpawner_01.Ingredient_02)
                                                {
                                                    rectangleBlock.spawners[0].SpawnersType_2 = Spawners.None;
                                                }
                                            }

                                            //First spawner
                                            rectangleBlock.spawners[0].SpawnersType =
                                                (Spawners)EditorGUILayout.EnumPopup(
                                                    rectangleBlock.spawners[0].SpawnersType, GUILayout.Width(250));
                                            if (rectangleBlock.spawners[0].SpawnersType != Spawners.Ingredient)
                                            {
                                                GUILayout.BeginVertical(GUILayout.Width(100));
                                                {
                                                    rectangleBlock.spawners[0].SpawnPersentage =
                                                        EditorGUILayout.FloatField(" Persentage ",
                                                            rectangleBlock.spawners[0].SpawnPersentage,
                                                            GUILayout.Width(300));
                                                }
                                                GUILayout.EndVertical();
                                            }
                                            else
                                            {
                                                //for ingredient
                                                GUILayout.BeginHorizontal(GUILayout.Width(100));
                                                {
                                                    GUILayout.Label(" Ingredient_01 ", GUILayout.Width(100));
                                                    rectangleBlock.spawners[0].IngredentSpawner_01.Ingredient_01 =
                                                        EditorGUILayout.Toggle(
                                                            rectangleBlock.spawners[0].IngredentSpawner_01.Ingredient_01,
                                                            GUILayout.Width(100));
                                                    GUILayout.Label(" Ingredient_02 ", GUILayout.Width(100));
                                                    rectangleBlock.spawners[0].IngredentSpawner_01.Ingredient_02 =
                                                        EditorGUILayout.Toggle(
                                                            rectangleBlock.spawners[0].IngredentSpawner_01.Ingredient_02,
                                                            GUILayout.Width(100));
                                                }
                                                GUILayout.EndHorizontal();

                                                GUILayout.Label(
                                                    " (!) This dispenser spawn ingredient one after another  ",
                                                    GUILayout.Width(700));


                                                GUILayout.Label(
                                                    " (!) Do not place the ingredients in field manually. (!)",
                                                    GUILayout.Width(700));
                                            }

                                            //second spawner
                                            rectangleBlock.spawners[0].SpawnersType_2 =
                                                (Spawners)EditorGUILayout.EnumPopup(
                                                    rectangleBlock.spawners[0].SpawnersType_2, GUILayout.Width(250));
                                            if (rectangleBlock.spawners[0].SpawnersType_2 != Spawners.Ingredient)
                                            {
                                                GUILayout.BeginVertical(GUILayout.Width(100));
                                                {
                                                    rectangleBlock.spawners[0].SpawnPersentage_2 =
                                                        EditorGUILayout.FloatField(" Persentage ",
                                                            rectangleBlock.spawners[0].SpawnPersentage_2,
                                                            GUILayout.Width(300));
                                                }
                                                GUILayout.EndVertical();
                                            }
                                            else
                                            {
                                                //for ingredient
                                                GUILayout.BeginHorizontal(GUILayout.Width(100));
                                                {
                                                    GUILayout.Label(" Ingredient_01 ", GUILayout.Width(100));
                                                    rectangleBlock.spawners[0].IngredentSpawner_02.Ingredient_01 =
                                                        EditorGUILayout.Toggle(
                                                            rectangleBlock.spawners[0].IngredentSpawner_02.Ingredient_01,
                                                            GUILayout.Width(50));
                                                    GUILayout.Label(" Ingredient_02 ", GUILayout.Width(100));
                                                    rectangleBlock.spawners[0].IngredentSpawner_02.Ingredient_02 =
                                                        EditorGUILayout.Toggle(
                                                            rectangleBlock.spawners[0].IngredentSpawner_02.Ingredient_02,
                                                            GUILayout.Width(50));
                                                }
                                                GUILayout.EndHorizontal();

                                                GUILayout.Label(
                                                    " (!) This dispenser spawn ingredient one after another  ",
                                                    GUILayout.Width(700));
                                            }
                                        }
                                        GUILayout.EndVertical();

                                        //GUILayout.BeginVertical(GUILayout.Width(100));
                                        //{

                                        //}
                                        //GUILayout.EndVertical();
                                    }
                                    GUILayout.EndHorizontal();
                                }
                            }
                        }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        ////Added_feature
        //private void SetSpawnerData(SingleSpawn Spawner)
        //{
        //    levelData.GetField(SubLevelNumber - 1).Spawners[Spawner.position.x].Spawns[Spawner.position.y] = Spawner;
        //}


        private int arrow_index;
        private int selectDirectionType;

        private void GUIDirections()
        {
            GUILayout.BeginHorizontal();
            {
                //GUILayout.Space(30);
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Tools:", EditorStyles.boldLabel);
                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button(new GUIContent(arrows_enter[0], "enter point on/off"), GUILayout.Width(50),
                                GUILayout.Height(50)))
                        {
                            selectDirectionType = 1;
                        }

                        if (GUILayout.Button(new GUIContent(arrows[4], "rotate one arrow"), GUILayout.Width(50),
                                GUILayout.Height(50)))
                        {
                            selectDirectionType = 0;
                        }

                        if (GUILayout.Button(new GUIContent(arrows[5], "rotate all"), GUILayout.Width(50),
                                GUILayout.Height(50)))
                        {
                            arrow_index = (int)Mathf.Repeat(arrow_index + 1, 4);
                            var squares = levelData.GetField(subLevelNumber - 1).levelSquares;
                            foreach (var item in squares)
                            {
                                item.direction = GetDirectionByIndex(arrow_index);
                            }

                            SetEnterPoint(GetDirectionByIndex(arrow_index));
                        }

                        if (GUILayout.Button(new GUIContent("Default", "default all direction"), GUILayout.Width(50),
                                GUILayout.Height(50)))
                        {
                            arrow_index = 0;
                            ResetDirection();
                            // SaveLevel();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void ResetDirection()
        {
            var squares = levelData.GetField(subLevelNumber - 1).levelSquares;
            foreach (var item in squares)
            {
                item.direction = GetDirectionByIndex(arrow_index);
            }

            SetEnterPoint(GetDirectionByIndex(arrow_index));
            dirtyLevel = true;
        }

        private void SetEnterPoint(Vector2 direction)
        {
            levelData.GetField(subLevelNumber - 1).levelSquares.ToList().ForEach(i => i.enterSquare = false);
            if (direction == Vector2.down)
            {
                for (int col = 0; col < levelData.GetField(subLevelNumber - 1).maxCols; col++)
                {
                    var squareBlocks = levelData.GetBlock(0, col);
                    if (squareBlocks.teleportCoordinatesLinkedBack == Vector2Int.one * -1)
                        squareBlocks.enterSquare = true;
                }
            }

            if (direction == Vector2.up)
            {
                for (int col = 0; col < levelData.GetField(subLevelNumber - 1).maxCols; col++)
                {
                    var squareBlocks = levelData.GetBlock(levelData.GetField(subLevelNumber - 1).maxRows - 1, col);
                    if (squareBlocks.teleportCoordinatesLinkedBack == Vector2Int.one * -1)
                        squareBlocks.enterSquare = true;
                }
            }

            if (direction == Vector2.left)
            {
                for (int row = 0; row < levelData.GetField(subLevelNumber - 1).maxRows; row++)
                {
                    var squareBlocks = levelData.GetBlock(row, levelData.GetField(subLevelNumber - 1).maxCols - 1);
                    if (squareBlocks.teleportCoordinatesLinkedBack == Vector2Int.one * -1)
                        squareBlocks.enterSquare = true;
                }
            }

            if (direction == Vector2.right)
            {
                for (int row = 0; row < levelData.GetField(subLevelNumber - 1).maxRows; row++)
                {
                    var squareBlocks = levelData.GetBlock(row, 0);
                    if (squareBlocks.teleportCoordinatesLinkedBack == Vector2Int.one * -1)
                        squareBlocks.enterSquare = true;
                }
            }
        }

        private int GetIndexByDirection(Vector2 direction)
        {
            if (direction == Vector2.right)
                return 3;
            if (direction == Vector2.left)
                return 1;
            if (direction == Vector2.up)
                return 2;
            return 0;
        }

        private Vector2 GetDirectionByIndex(int index)
        {
            if (index == 0) return Vector2.down;
            if (index == 1) return Vector2.left;
            if (index == 2) return Vector2.up;
            if (index == 3) return Vector2.right;
            return Vector2.down;
        }

        private Texture GetArrowByAngle(float angle)
        {
            return arrows[(int)(angle % 90)];
        }

        private Texture GetArrowByVector(Vector2 direction, bool enterPoint)
        {
            var arr = arrows;
            if (enterPoint) arr = arrows_enter;
            if (direction == Vector2.up)
                return arr[3];
            if (direction == Vector2.left)
                return arr[1];
            if (direction == Vector2.right)
                return arr[2];
            return arr[0];
        }

        private void GUIItems()
        {
            GUILayout.BeginHorizontal();
            {
                //GUILayout.Space(30);
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Tip: right click to change sprite for level", EditorStyles.boldLabel);
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.BeginVertical();
                        {
                            if (GUILayout.Button(new GUIContent("Clear\nAll", "clear all field"), GUILayout.Width(50),
                                    GUILayout.Height(50)))
                            {
                                ClearItems();
                                // SaveLevel();
                            }

                            if (GUILayout.Button(new GUIContent("Clear\nBlock", "Clear block"), GUILayout.Width(50),
                                    GUILayout.Height(50)))
                            {
                                itemBrush = null;
                            }
                        }
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical();
                        {
                            foreach (var val in itemsForEditor)
                            {
                                GUILayout.BeginHorizontal();
                                {
                                    foreach (var item in val)
                                    {
                                        GameObject item1 = item.Item;
                                        if (!item1) continue;
                                        if (item.ItemType != ItemsTypes.NONE)
                                            item.SetColor(color, levelNumber);
                                        /*if (item.ItemType is ItemsTypes.Eggs or ItemsTypes.Pots)
                                            continue;*/
                                        var texture = item.Texture;
                                        if (item.ItemType == ItemsTypes.Bomb)
                                        {
                                            try
                                            {
                                                texture = packageTexture.AlphaBlend(item.Texture);
                                            }
                                            catch (Exception e)
                                            {
                                                Debug.Log(e);
                                            }
                                        }

                                        if (itemBrush != null && (itemBrush.ItemType == item.ItemType &&
                                                                  item.ItemType == ItemsTypes.NONE &&
                                                                  color == item.Color && itemBrush
                                                                      .Texture == item.Texture))
                                            UnityEngine.GUI.backgroundColor = Color.gray;
                                        else if (itemBrush != null && (itemBrush.ItemType == item.ItemType &&
                                                                       item.ItemType != ItemsTypes.NONE && itemBrush
                                                                           .Texture == item.Texture))
                                            UnityEngine.GUI.backgroundColor = Color.gray;
                                        if (GUILayout.Button(new GUIContent(texture, item1.name),
                                                GUILayout.Width(50), GUILayout.Height(50)))
                                        {
                                            Event current = Event.current;
                                            if (current.button == 1)
                                            {
                                                dirtyLevel = true;
                                                SaveLevel(levelNumber);
                                                ItemsInsideEditor.ShowWindow(item1, levelNumber);
                                            }
                                            else
                                            {
                                                itemBrush = item.DeepCopy();
                                                if (item.ItemType == ItemsTypes.NONE) color = itemBrush.Color;
                                            }
                                        }

                                        UnityEngine.GUI.backgroundColor = Color.white;
                                    }
                                }
                                GUILayout.EndHorizontal();
                            }
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void ClearItems()
        {
            for (int i = 0; i < levelData.GetField(subLevelNumber - 1).levelSquares.Length; i++)
            {
                levelData.GetField(subLevelNumber - 1).levelSquares[i].item = null;
            }

            dirtyLevel = true;
        }

        private void GUIBlocks()
        {
            GUILayout.BeginHorizontal();
            {
                //GUILayout.Space(30);
                GUILayout.BeginVertical();
                {
                    GUILayout.Label("Tools:", EditorStyles.boldLabel);
                    GUILayout.BeginHorizontal();
                    {
                        UnityEngine.GUI.color = new Color(1, 1, 1, 1f);
                        foreach (LevelTargetTypes squareTypeItem in _squareTypeItems)
                        {
//                                if (HideAnotherTargetBlock(squareTypeItem.ToString())) continue;
                            if (squareTypeItem == LevelTargetTypes.NONE) continue;
                            var squareTexture = Rectangle.GetSquareTexture(squareTypeItem);
                            if (_levelTargetType == squareTypeItem)
                                UnityEngine.GUI.backgroundColor = Color.gray;
                            if (squareTexture.Texture2D != null && GUILayout.Button(
                                    new GUIContent(squareTexture.Texture2D, squareTypeItem.ToString()),
                                    GUILayout.Width(50), GUILayout.Height(50)))
                            {
                                _levelTargetType = squareTypeItem;
                                deleteBlock = false;
                                separateBarBrush = false;
                            }

                            UnityEngine.GUI.backgroundColor = Color.white;
                        }

                        UnityEngine.GUI.color = new Color(1, 1, 1, 1f);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        if (separateBarBrush)
                            UnityEngine.GUI.backgroundColor = Color.gray;
                        if (GUILayout.Button(
                                new GUIContent(separateBar, "Separate bar prevent items from falling or moving through"),
                                GUILayout.Width(50),
                                GUILayout.Height(50)))
                        {
                            _levelTargetType = LevelTargetTypes.NONE;
                            separateBarBrush = !separateBarBrush;
                        }

                        UnityEngine.GUI.backgroundColor = Color.white;

                        if (GUILayout.Button(new GUIContent("Clear", "clear all field"), GUILayout.Width(50),
                                GUILayout.Height(50)))
                        {
                            ClearLevel();
                            // SaveLevel();
                        }

                        if (GUILayout.Button(new GUIContent("X", "Clear block"), GUILayout.Width(50),
                                GUILayout.Height(50)))
                        {
                            _levelTargetType = LevelTargetTypes.NONE;
                            separateBarBrush = false;
                        }

                        if (GUILayout.Button(
                                new GUIContent("Fill+", "Fill with selected block, second click change or clear filling"),
                                GUILayout.Width(50),
                                GUILayout.Height(50)))
                        {
                            FillLevel();
                        }

                        if (deleteBlock)
                            UnityEngine.GUI.backgroundColor = Color.gray;
                        if (GUILayout.Button(new GUIContent("-1", "to Delete a block from layer"), GUILayout.Width(50),
                                GUILayout.Height(50)))
                        {
                            deleteBlock = !deleteBlock;
                            separateBarBrush = false;
                        }

                        UnityEngine.GUI.backgroundColor = Color.white;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void ClearLevel()
        {
            layers = 1;
            layer = 0;
            foreach (var squareBlocks in levelData.GetField(subLevelNumber - 1).levelSquares)
            {
                squareBlocks.blocks.Clear();
                squareBlocks.block = LevelTargetTypes.EmptySquare;
                squareBlocks.blockLayer = 1;
                squareBlocks.obstacle = LevelTargetTypes.NONE;
                squareBlocks.obstacleLayer = 1;
                squareBlocks.blocks.Add(new SquareTypeLayer { levelTargetType = squareBlocks.block });
                squareBlocks.separatorIndexes = new bool[4];
            }

            dirtyLevel = true;
        }

        private void FillLevel()
        {
            for (int i = 0; i < levelData.GetField(subLevelNumber - 1).levelSquares.Length; i++)
            {
                var squareBlocks = levelData.GetField(subLevelNumber - 1).levelSquares[i];
                var tempType = _levelTargetType;
                if ((squareBlocks.block == _levelTargetType && Rectangle.GetLayersCount(_levelTargetType) == squareBlocks.blockLayer)
                    || (squareBlocks.obstacle == _levelTargetType &&
                        Rectangle.GetLayersCount(_levelTargetType) == squareBlocks.obstacleLayer))
                {
                    var sqPos = squareBlocks.position;
                    _levelTargetType = LevelTargetTypes.EmptySquare;
                    SetSquareType(sqPos.x, sqPos.y);
                    _levelTargetType = tempType;
                }
                else if (squareBlocks.block == LevelTargetTypes.EmptySquare ||
                         (squareBlocks.block == _levelTargetType || squareBlocks.obstacle == _levelTargetType))
                {
                    var sqPos = squareBlocks.position;
                    SetSquareType(sqPos.x, sqPos.y);
                }
            }

            dirtyLevel = true;
        }

        private RectangleBlocks _rectangleBlockSelected; //for teleport linking

        class RectangleTexture
        {
            public Texture2DSize square;
            public Rect rect;
        }

        private void GUIGameField()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Layers " + (layer + 1) + " / " + layers, EditorStyles.label, GUILayout.Width(100));
                GUILayout.Space(0);
                layer = (int)GUILayout.HorizontalSlider(layer, layers - 1, 0, GUILayout.Width(100));
                //GUILayout.Space(50);
                //rotateBig = GUILayout.Toggle(rotateBig, new GUIContent("rotate", "rotate big object"), GUILayout.Width(100));
            }
            GUILayout.EndHorizontal();
            var rects = new List<RectangleTexture>();
            GUILayout.BeginVertical();
            for (int row = 0; row < GetField().maxRows; row++)
            {
                GUILayout.BeginHorizontal();

                for (int col = 0; col < GetField().maxCols; col++)
                {
                    var squareColor = new Color(0.8f, 0.8f, 0.8f);
                    var imageButton = new object();
                    var squareBlock = levelData.GetBlock(row, col);

                    squareBlock.position = new Vector2Int(col, row);

                    //GUILayout.Label("c-"+ col + " & r-"+row);

                    var blockTextures = Rectangle.GetSquareTextures(squareBlock, layer);

                    var blockTexturesArray = new List<Texture2DSize>();
                    if (squareBlock.block == LevelTargetTypes.NONE)
                    {
                        imageButton = "X";
                        squareColor = new Color(0.8f, 0.8f, 0.8f);
                    }
                    else
                    {
                        imageButton = blockTextures?[0];
                        squareColor = Color.white;
                    }

                    blockTexturesArray = blockTextures.ToList();
                    if (section == 0 || section == 1)
                    {
                        if (squareBlock.block != LevelTargetTypes.NONE)
                        {
                            //                        imageButton = squareBlock.item?.Texture;
                            if (squareBlock.item != null && squareBlock.item.Item != null &&
                                squareBlock.item?.ItemType != ItemsTypes.Gredient)
                            {
                                blockTexturesArray.Add(new Texture2DSize(squareBlock.item?.Item
                                    .GetComponent<ColorReciever>().GetSprite(levelNumber, squareBlock.item
                                        .Color).texture, Vector2Int.one, squareBlock.item.order, rotateBig));
                            }
                            else
                            {
                                var t = Rectangle.GetSquareTexture(LevelTargetTypes.EmptySquare);
                                if (!blockTexturesArray.Any(i => i.Texture2D.name == t.Texture2D.name))
                                    blockTexturesArray.Insert(0,
                                        new Texture2DSize(t.Texture2D, Vector2Int.one, t.order, rotateBig));
                                if (squareBlock.item?.Texture != null)
                                    blockTexturesArray.Add(new Texture2DSize(squareBlock.item?.Texture, Vector2Int.one,
                                        squareBlock.item.order, rotateBig));
                            }

                            if (squareBlock.separatorIndexes.Any(i => i))
                            {
                                blockTexturesArray.AddRange(squareBlock.GetSeparatorTexture(separateBar));
                            }
                        }

                        if (squareBlock.item?.ItemType == ItemsTypes.Bomb)
                        {
                            // if (squareBlock.item?.Texture == null)
                            var texture1 = itemsForEditorFull.First(i => i.ItemType == ItemsTypes.Bomb).Texture;
                            var texture2 = squareBlock.item?.Item.GetComponent<ColorReciever>()
                                .GetSprite(levelNumber, squareBlock.item.Color).texture;
                            if (texture1.width == texture2.width && texture1.height == texture2.height)
                                blockTexturesArray.Add(new Texture2DSize(texture1?.AlphaBlend(texture2), Vector2Int.one,
                                    1000, rotateBig));
                        }
                    }

                    if (section == 2)
                    {
                        imageButton = GetArrowByVector(squareBlock.direction, squareBlock.enterSquare);
                    }

                    if (section == 3)
                    {
                        if (squareBlock.teleportCoordinatesLinkedBack != Vector2Int.one * -1)
                        {
                            imageButton = teleports[1];
                        }

                        if (squareBlock.isEnterTeleport)
                            imageButton = teleports[0];
                    }

                    if (section == 4)
                    {
                        if (squareBlock.spawners.Count > 0)
                        {
                            imageButton = GetRotatedSpawnerTexture(squareBlock.spawners[0].rotationType);
                        }
                    }

                    UnityEngine.GUI.color = squareColor;
                    if (GUILayout.Button(imageButton as Texture, GUILayout.Width(50), GUILayout.Height(50)))
                    {
                        if (section == 0)
                        {
                            if (_levelTargetType == LevelTargetTypes.PlateCabinet || _levelTargetType == LevelTargetTypes.PotionCabinet)
                            {
                                var squarePrototype = Rectangle.GetLayersCount(_levelTargetType);
                                for (var i = 0; i < squarePrototype; i++)
                                    SetSquareType(col, row);
                            }
                            else
                            {
                                SetSquareType(col, row);
                            }
                        }

                        if (section == 1)
                            SetItem(col, row);
                        else if (section == 2)
                        {
                            if (selectDirectionType == 0)
                                SetArrow(col, row);
                            else if (selectDirectionType == 1)
                                levelData.GetBlock(row, col).enterSquare = !levelData.GetBlock(row, col).enterSquare;
                        }
                        else if (section == 3)
                        {
                            if (squareBlock.isEnterTeleport ||
                                squareBlock.teleportCoordinatesLinkedBack != Vector2Int.one * -1)
                            {
                                _rectangleBlockSelected = null;
                                squareBlock.isEnterTeleport = false;
                                squareBlock.teleportCoordinatesLinkedBack = Vector2Int.one * -1;
                                squareBlock.teleportCoordinatesLinked = Vector2Int.one * -1;
                            }
                            else if (_rectangleBlockSelected == null)
                            {
                                _rectangleBlockSelected = squareBlock;
                                _rectangleBlockSelected.isEnterTeleport = true;
                            }
                            else
                            {
                                levelData.GetBlock(row, col).enterSquare = false;
                                squareBlock.teleportCoordinatesLinkedBack = _rectangleBlockSelected.position;
                                _rectangleBlockSelected.teleportCoordinatesLinked = squareBlock.position;
                                _rectangleBlockSelected = null;
                            }
                        }

                        //Added_feature
                        else if (section == 4)
                        {
                            switch (CurrentSpawnerState)
                            {
                                case SpawnerState.Add:
                                {
                                    SetSpawnerType(col, row);
                                    levelData.GetBlock(row, col).enterSquare = true;
                                    Column = -1;
                                    Row = -1;
                                }
                                    break;
                                case SpawnerState.Edit:
                                {
                                    Column = col;
                                    Row = row;
                                    //Debug.Log("Edit Selected");
                                }
                                    break;
                                case SpawnerState.Remove:
                                {
                                    RemoveSpawnerTyps(col, row);
                                    levelData.GetBlock(row, col).enterSquare = false;
                                    Column = -1;
                                    Row = -1;
                                }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }


                    var lastRect = GUILayoutUtility.GetLastRect();
                    var texture2DSizes = blockTexturesArray.WhereNotNull().OrderByDescending(i => i.order).ToArray();
                    foreach (var texture2DSiz in texture2DSizes)
                    {
                        if (texture2DSiz.Texture2D)
                        {
                            var rectTexture = new RectangleTexture() { square = texture2DSiz, rect = lastRect };
                            rects.Add(rectTexture);
                        }
                    }

                    if (lastRect != Rect.zero && lastRect.position != Vector2.zero)
                        squareBlock.guiRect = lastRect;
                    // if (section != 2 && section != 3)
                    //     DrawLayeredTextures(rectTexture);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
            if (section != 2 && section != 3 && section != 4)
            {
                var orderedEnumerable = rects.OrderBy(i => i.square.order);
                foreach (var rectTexture in orderedEnumerable)
                {
                    DrawTexture(rectTexture.square, rectTexture.rect);
                }
            }

            SetupArrows();
            DrawLines();
        }

        //Added_feature
        private Texture GetRotatedSpawnerTexture(RotationType rotationType)
        {
            Texture res = null;

            switch (rotationType)
            {
                case RotationType.Top:
                    return spawner[0];
                case RotationType.Bottom:
                    return spawner[1];
                case RotationType.Left:
                    return spawner[2];
                case RotationType.right:
                    return spawner[3];
                default:
                    break;
            }

            return res;
        }

        RotationType CheckForRotation(RotationType type, bool LeftRotation)
        {
            RotationType res = type;

            switch (res)
            {
                case RotationType.Top:
                    if (LeftRotation)
                        return RotationType.Left;
                    return RotationType.right;
                case RotationType.Left:
                    if (LeftRotation)
                        return RotationType.Bottom;
                    return RotationType.Top;
                case RotationType.Bottom:
                    if (LeftRotation)
                        return RotationType.right;
                    return RotationType.Left;
                case RotationType.right:
                    if (LeftRotation)
                        return RotationType.Top;
                    return RotationType.Bottom;
                default:
                    break;
            }

            return res;
        }

        private FieldData GetField() => levelData.GetField(subLevelNumber - 1);

        private void DrawTexture(Texture2DSize texture2DSize, Rect lastRect)
        {
            var texture2D = texture2DSize.Texture2D;
            if (texture2D != null)
            {
                var rect = new Rect(lastRect.position.x + 4 + texture2DSize.offset.x,
                    lastRect.position.y + 4 + texture2DSize.offset.y, texture2DSize.size.x * 40 * texture2DSize
                        .sizeMod.x, texture2DSize.size.y * 40 * texture2DSize.sizeMod.y);
                if (texture2DSize.size.magnitude >= 2 && texture2DSize.rotate && texture2D.width < 2048)
                {
                    texture2D = texture2D.rotateTexture();
                    rect = new Rect(lastRect.position.x + 4 + texture2DSize.offset.x,
                        lastRect.position.y + 4 + texture2DSize.offset.y, texture2DSize.size.y * 40,
                        texture2DSize.size.x * 40);
                }
                else if (texture2DSize.rotate)
                    texture2D = texture2D.rotateTexture();

                UnityEngine.GUI.DrawTexture(rect, texture2D);
            }
        }

        private void DrawLines()
        {
            if (section != 1) return;
            var choppers = levelData.GetField().levelSquares
                .Where(i => i.item != null && i.item.EnableChopperTargets);
            Handles.color = Color.red;
            foreach (var chopper in choppers)
            {
                //            Handles.BeginGUI();
                foreach (var targetPosition in chopper.item.TargetChopperPositions)
                {
                    Handles.DrawLine(chopper.guiRect.center, levelData.GetBlock(targetPosition).guiRect.center);
                    RotateGizmo(chopper.guiRect.center, levelData.GetBlock(targetPosition).guiRect.center, 20f);
                    RotateGizmo(chopper.guiRect.center, levelData.GetBlock(targetPosition).guiRect.center, -20f);
                }
                //            Handles.EndGUI();
            }
        }

        private void RotateGizmo(Vector3 square, Vector3 pivot, float angle)
        {
            var rightPoint = Vector3.zero;
            var dir = (square - pivot) * 0.1f;
            dir = Quaternion.AngleAxis(angle, Vector3.back) * dir;
            rightPoint = dir + pivot;
            Handles.DrawLine(pivot, rightPoint);
        }

        private void SetItem(int col, int row)
        {
            RectangleBlocks rectangleBlock = levelData.GetBlock(row, col);
            if (rectangleBlock.blocks.Any(i =>
                    i.levelTargetType == LevelTargetTypes.BreakableBox || i.levelTargetType == LevelTargetTypes.ExtraTargetType3 ||
                    i.levelTargetType == LevelTargetTypes.NONE)) return;
            EditorItems itemTemp = rectangleBlock.item;
            if (itemTemp == null)
                itemTemp = itemBrush.DeepCopy();
            else if (itemBrush?.ItemType != ItemsTypes.NONE ||
                     (itemTemp.ItemType == ItemsTypes.NONE && itemBrush?.ItemType == ItemsTypes.NONE))
                itemTemp = itemBrush?.DeepCopy();
            else if (itemBrush.ItemType == ItemsTypes.NONE && itemTemp.ItemType != ItemsTypes.NONE)
                itemTemp.Color = itemBrush.Color;
            //        if (itemTemp.ItemType != ItemsTypes.NONE)
            if (itemTemp != null && itemBrush != null && itemsForEditorFull.First(i => i.ItemType == itemTemp.ItemType)
                    .Item.GetComponent<ColorReciever>()?.GetSprites(levelNumber).Count() > 1)
                itemTemp.Texture =
                    itemsForEditorFull.FirstOrDefault(i => i.ItemType == itemTemp.ItemType).Item
                        .GetComponent<ColorReciever>().GetSprites(levelNumber)[color].texture;

            rectangleBlock.item = itemTemp;
        }

        private void SetupArrows()
        {
            for (int row = 0; row < levelData.GetField(subLevelNumber - 1).maxRows; row++)
            {
                for (int col = 0; col < levelData.GetField(subLevelNumber - 1).maxCols; col++)
                {
                    RectangleBlocks rectangleBlock = levelData.GetBlock(row, col);
                    if (section == 2 || section == 3)
                    {
                        if (rectangleBlock.teleportCoordinatesLinkedBack != Vector2Int.one * -1)
                        {
                            Handles.BeginGUI();
                            Handles.color = Color.red;
                            Vector2 position1 = rectangleBlock.guiRect.center;
                            Vector2 position2 = levelData.GetBlock(rectangleBlock.teleportCoordinatesLinkedBack).guiRect
                                .center;
                            Handles.DrawLine(position1, position2);
                            float angle = AngleInDeg(position1, position2);
                            Handles.ArrowHandleCap(0, position2,
                                Quaternion.AngleAxis(angle, Vector3.forward) * Quaternion.Euler(new Vector3(0, -95, 0)),
                                50,
                                EventType.Repaint);
                            Handles.EndGUI();
                        }
                    }
                }
            }
        }

        //This returns the angle in radians
        public static float AngleInRad(Vector3 vec1, Vector3 vec2)
        {
            return Mathf.Atan2(vec2.y - vec1.y, vec2.x - vec1.x);
        }

        //This returns the angle in degress
        public static float AngleInDeg(Vector3 vec1, Vector3 vec2)
        {
            return AngleInRad(vec1, vec2) * 180 / Mathf.PI;
        }


        private void SetArrow(int col, int row)
        {
            RectangleBlocks rectangleBlock = levelData.GetBlock(row, col);
            int index = GetIndexByDirection(rectangleBlock.direction);
            index = (int)Mathf.Repeat(index + 1, 4);
            // if (squareBlock.block != SquareTypes.NONE)
            // {
            rectangleBlock.direction = GetDirectionByIndex(index);
            // }
        }


        private void SaveLevel(int _levelNumber)
        {
            _rectangleBlockSelected = null;
            if (dirtyLevel)
            {
                if (!FileName.Contains(".txt"))
                    FileName += ".txt";
                SaveToScriptable(_levelNumber);
                // SaveCommonValues();
                // SaveField();
                dirtyLevel = false;
            }
        }

        private void AddLevel()
        {
            _rectangleBlockSelected = null;
            SaveLevel(levelNumber);
            levelNumber = GetLastLevel() + 1;
            SubLevelNumber = 1;
            LevelManagerScriptableObject.CreateFileLevel(levelNumber, levelData);
            levelData = LevelManagerScriptableObject.LoadLevel(levelNumber);
            Initialize();
            ClearLevel();
            ClearItems();
            SaveLevel(levelNumber);
            PrepareTargetLevel();
            subLevelNumberTotal = GetSubLevelsCount();
        }

        private int GetLastLevel()
        {
            int lastLevel = LoadingController.GetLastLevelNum();
            if (lastLevel == 0) lastLevel = _scriptableObjectLevel.levels.Count;
            return lastLevel;
        }

        private void AddSubLevel()
        {
            _rectangleBlockSelected = null;
            SaveLevel(levelNumber);
            levelData.AddNewField();
            SubLevelNumber = GetSubLevelsCount();
            Initialize();
            InitializeSublevel();
            SaveLevel(levelNumber);
            subLevelNumberTotal = GetSubLevelsCount();
        }

        private void RemoveSubLevel()
        {
            if (GetSubLevelsCount() > 1)
            {
                SaveLevel(levelNumber);
                levelData.RemoveField();
                SubLevelNumber = GetSubLevelsCount();
                Initialize();
                // InitializeSublevel();
                SaveLevel(levelNumber);
                subLevelNumberTotal = GetSubLevelsCount();
            }
        }

        private int GetSubLevelsCount()
        {
            return levelData.fields.Count; //GetSublevelsScriptable();
        }

        private int GetSublevelsScriptable()
        {
            return _scriptableObjectLevel.levels[levelNumber - 1].fields.Count;
        }

        private void RemoveLevel()
        {
            PreviousLevel();
            File.Delete(levelPath + "Level_" + (levelNumber + 1) + ".asset");
            File.Delete(levelPath + "Targets/" + "TargetLevel" + (levelNumber + 1) + ".asset");
            AssetDatabase.Refresh();
        }

        private void NextLevel()
        {
            SaveLevel(levelNumber);
            if (levelNumber + 1 <= GetLastLevel())
            {
                levelNumber++;
                SubLevelNumber = 1;
                LoadLevel(levelNumber);
            }
        }

        private void PreviousLevel()
        {
            SaveLevel(levelNumber);
            levelNumber--;
            SubLevelNumber = 1;
            if (levelNumber < 1)
                levelNumber = 1;
            if (!LoadLevel(levelNumber))
            {
                levelNumber++;
                LoadLevel(levelNumber);
            }
        }

        private void NextSubLevel()
        {
            if (SubLevelNumber + 1 <= GetSubLevelsCount())
            {
                SaveLevel(levelNumber);
                SubLevelNumber++;
            }

            // if (!LoadLevel(levelNumber, SubLevelNumber))
            // 	SubLevelNumber--;
        }

        private void PreviousSubLevel()
        {
            SaveLevel(levelNumber);
            SubLevelNumber--;
            if (SubLevelNumber < 1)
                SubLevelNumber = 1;
            // if (!LoadLevel(levelNumber, SubLevelNumber))
            // SubLevelNumber++;
        }

        private bool dirtyLevel;

        private int section;

        //private bool gotFocus;
        private static Material mat;
        private int color;
        private Texture2D packageTexture;
        private GUISkin customSkin;

        private ReferencerUI _referencerUI;

        //private AdManagerScriptable adsSettings;
        private int selectedTutorial;
        private TargetEditorScriptable targetEditorScriptable;
        private Texture2DSize[] squareTextures;
        private MapSwitcher sceneSwitcher;
        private WinReward winReward;

        private int layers
        {
            get => GetField().layers;
            set => GetField().layers = value;
        }

        private int layer;
        private IEnumerable<LevelTargetTypes> _squareTypeItems;
        private IEnumerable<LevelTargetTypes>[] squareTypeGroupped;
        private bool deleteBlock;
        private bool rotateBig;
        private Texture2D separateBar;
        private bool separateBarBrush;
        private int m_separateIndex;
        private RectangleBlocks _mRectangleBlock;

        /// <summary>
        /// Sets the square type for a given grid position
        /// </summary>
        /// <param name="col">Column position</param>
        /// <param name="row">Row position</param>
        private void SetSquareType(int col, int row)
        {
            dirtyLevel = true;
            RectangleBlocks rectangleBlock = levelData.GetBlock(row, col);
            _mRectangleBlock = rectangleBlock;

            // Handle separate bar brush selection via context menu
            if (separateBarBrush)
            {
                GenericMenu menu = new GenericMenu();
                AddMenuItem(menu, "LEFT", 0, rectangleBlock);
                AddMenuItem(menu, "RIGHT", 1, rectangleBlock);
                AddMenuItem(menu, "UP", 2, rectangleBlock);
                AddMenuItem(menu, "DOWN", 3, rectangleBlock);
                AddMenuItem(menu, "NONE", 4, rectangleBlock);
                menu.ShowAsContext();
                return;
            }

            // Clear block if empty or none type selected
            if (_levelTargetType == LevelTargetTypes.EmptySquare || _levelTargetType == LevelTargetTypes.NONE)
            {
                rectangleBlock.blocks.Clear();
                rectangleBlock.blocks.Add(new SquareTypeLayer { levelTargetType = _levelTargetType });
                rectangleBlock.block = _levelTargetType;
                rectangleBlock.blockLayer = 1;
                rectangleBlock.obstacle = LevelTargetTypes.NONE;
                rectangleBlock.obstacleLayer = 1;
                if (noGenInto)
                    rectangleBlock.cantGenInto = true;
            }
            else
            {
                // Get the prototype block for the selected square type
                Rectangle rectanglePrototype = Rectangle.GetBlockPrefab(_levelTargetType).Last().GetComponent<Rectangle>();

                // Add block if not in delete mode
                if (!deleteBlock)
                {
                    // Handle regular blocks
                    if (!rectanglePrototype.IsObstacle() && _levelTargetType != LevelTargetTypes.EmptySquare &&
                        _levelTargetType != LevelTargetTypes.NONE)
                    {
                        // Increment layer if same block type exists
                        if (rectangleBlock.block == _levelTargetType)
                        {
                            rectangleBlock.blockLayer =
                                (int)Mathf.Repeat(rectangleBlock.blockLayer, Rectangle.GetLayersCount(_levelTargetType)) + 1;
                        }
                        else
                        {
                            rectangleBlock.blockLayer = 1;
                        }

                        rectangleBlock.block = _levelTargetType;
                    }
                    // Handle obstacle blocks
                    else if (rectanglePrototype.IsObstacle())
                    {
                        // Increment layer if same obstacle type exists
                        if (rectangleBlock.obstacle == _levelTargetType)
                        {
                            rectangleBlock.obstacleLayer = (int)Mathf.Repeat(rectangleBlock.obstacleLayer,
                                Rectangle.GetLayersCount(_levelTargetType)) + 1;
                        }
                        else
                        {
                            rectangleBlock.obstacleLayer = 1;
                        }

                        rectangleBlock.obstacle = _levelTargetType;
                    }

                    AddBlock(rectangleBlock, rectanglePrototype);
                }
                // Delete block if in delete mode and not on bottom layer
                else if (layer > 0)
                {
                    DeleteBlock(rectangleBlock, rectanglePrototype);
                }
            }

            // update = true;
            // SaveLevel();
            // GetSquare(col, row).type = (int) squareType;
        }

        //Add_feature
        private void SetSpawnerType(int col, int row)
        {
            dirtyLevel = true;
            RectangleBlocks rectangleBlock = levelData.GetBlock(row, col);
            if (!(rectangleBlock.spawners.Count > 0))
            {
                SingularSpawn newSingularSpawn = new SingularSpawn()
                {
                    SpawnId = " Spawner" + "_" + UnityEngine.Random.Range(1000, 9999) + " ",
                    SpawnersType = Spawners.None,
                    SpawnersType_2 = Spawners.None,
                    SpawnPersentage = 0,
                    SpawnPersentage_2 = 0,
                    rotationType = RotationType.Top
                };
                rectangleBlock.spawners.Add(newSingularSpawn);
            }
        }


        void AddMenuItem(GenericMenu menu, string menuPath, int index, RectangleBlocks rectangleBlocks)
        {
            bool squareBlocksSeparatorIndex = false;
            if (index < rectangleBlocks.separatorIndexes.Length)
                squareBlocksSeparatorIndex = rectangleBlocks.separatorIndexes[index];
            menu.AddItem(new GUIContent(menuPath), squareBlocksSeparatorIndex, OnSeparateSelected, index);
        }

        private void OnSeparateSelected(object userdata)
        {
            m_separateIndex = (int)userdata;
            if (_mRectangleBlock.separatorIndexes.Length > m_separateIndex)
                _mRectangleBlock.separatorIndexes[m_separateIndex] = !_mRectangleBlock.separatorIndexes[m_separateIndex];
            else
            {
                for (int i = 0; i < _mRectangleBlock.separatorIndexes.Length; i++)
                {
                    _mRectangleBlock.separatorIndexes[i] = false;
                }
            }
        }

        //Add_feature
        private void RemoveSpawnerTyps(int col, int row)
        {
            dirtyLevel = true;
            RectangleBlocks rectangleBlock = levelData.GetBlock(row, col);
            rectangleBlock.spawners.Clear();
        }

        private void DeleteBlock(RectangleBlocks rectangleBlock, Rectangle rectanglePrototype)
        {
            var index = Mathf.Min(layer, rectangleBlock.blocks.Count - 1);
            if (index < 0) return;
            if (rectangleBlock.blocks[index].levelTargetType == LevelTargetTypes.ExtraTargetType4)
            {
                var originalPos = rectangleBlock.blocks[index].originalPos;
                var size = rectangleBlock.blocks[index].size;
                for (int x = originalPos.x; x < originalPos.x + size.x; x++)
                {
                    for (int y = originalPos.y; y < originalPos.y + size.y; y++)
                    {
                        var originalBlock = levelData.GetBlock(y, x);
                        originalBlock.blocks.RemoveAt(index);
                        originalBlock.MergeEmptySquares();
                    }
                }
            }
            else
                rectangleBlock.blocks.RemoveAt(index);

            var sum = levelData.GetField().levelSquares.Max(i => i.blocks.Count);
            if (sum < layers)
                layers = sum;
            if (layer >= layers) layer = layers - 1;
        }

        private void AddBlock(RectangleBlocks rectangleBlock, Rectangle rectanglePrototype)
        {
            // layer++;
            var list = new List<RectangleBlocks>();
            var xMax = rectanglePrototype.sizeInSquares.x;
            var yMax = rectanglePrototype.sizeInSquares.y;
            if (rotateBig)
            {
                yMax = rectanglePrototype.sizeInSquares.x;
                xMax = rectanglePrototype.sizeInSquares.y;
            }

            for (int x = 0; x < xMax; x++)
            {
                for (int y = 0; y < yMax; y++)
                {
                    list.Add(levelData.GetBlock(rectangleBlock.position.y + y, rectangleBlock.position.x + x));
                }
            }

            int[] layersCount = new int[list.Count];
            for (var index = 0; index < list.Count; index++)
            {
                var squareBlockse = list[index];
                if (rectanglePrototype.type == LevelTargetTypes.PlateCabinet)
                {
                    layersCount[index] = Mathf.Min(layer, squareBlockse.blocks.Count - 1) + 12;
                }

                if (rectanglePrototype.type == LevelTargetTypes.PotionCabinet)
                {
                    layersCount[index] = Mathf.Min(layer, squareBlockse.blocks.Count - 1) + 6;
                }
                else
                {
                    layersCount[index] = Mathf.Min(layer, squareBlockse.blocks.Count - 1) + 1;
                }
            }

            var currentlayer = layersCount.Max();

            foreach (var squareBlockse in list)
            {
                ExpandBlocksList(squareBlockse, currentlayer);
                var squareTypeLayer = new SquareTypeLayer { levelTargetType = _levelTargetType };
                if (rectangleBlock != squareBlockse)
                    squareTypeLayer.anotherSquare = true;

                if (_levelTargetType == LevelTargetTypes.PlateCabinet || _levelTargetType == LevelTargetTypes.PotionCabinet)
                    squareTypeLayer.anotherSquare = false;

                squareTypeLayer.rotate = rotateBig;
                squareTypeLayer.originalPos = rectangleBlock.position;
                squareTypeLayer.size = new Vector2Int(xMax, yMax);
                squareBlockse.blocks.Insert(currentlayer, squareTypeLayer);
            }

            Rectangle.GetSquareTextures(rectangleBlock, layer);

            if (rectangleBlock.blocks.Count() > layers)
                layers = rectangleBlock.blocks.Count();
            if (currentlayer > layer) layer = currentlayer;
            layer = Mathf.Clamp(layer, 0, layers - 1);
        }

        private void ExpandBlocksList(RectangleBlocks sqBlock, int maxLayer)
        {
            var blocksCount = sqBlock.blocks.Count;
            if (maxLayer >= blocksCount)
            {
                for (int i = 0; i < maxLayer - blocksCount; i++)
                {
                    sqBlock.blocks.Add(new SquareTypeLayer { levelTargetType = LevelTargetTypes.EmptySquare });
                }
            }
        }

        private void SaveToScriptable(int _levelNumber)
        {
            levelData.InitTargetObjects();
            RectangleBlocks[] levelSquares = levelData.GetField(subLevelNumber - 1).levelSquares;
            for (int i = 0; i < levelSquares.Length; i++)
            {
                var item = levelSquares[i];
                var dir = item.direction;

                if (dir == Vector2.zero) item.direction = Vector2.down;
            }

            bool enterSquaresExist = levelSquares.Any(i => i.enterSquare);
            if (!enterSquaresExist)
            {
                for (int col = 0; col < levelData.GetField(subLevelNumber - 1).maxCols; col++)
                {
                    levelData.GetBlock(0, col).enterSquare = true;
                }
            }

            if (_scriptableObjectLevel.levels.Count() < _levelNumber)
                _scriptableObjectLevel.levels.Add(levelData);
            else
                _scriptableObjectLevel.levels[_levelNumber - 1] = levelData.DeepCopy(_levelNumber);
            LevelManagerScriptableObject.SaveLevel(levelPath, _levelNumber, levelData);

//        EditorUtility.SetDirty(levelScriptable);
//        AssetDatabase.SaveAssets();
        }

        private bool LoadLevel(int currentLevel)
        {
            levelData = LoadingController.LoadlLevel(currentLevel, levelData);
            _rectangleBlockSelected = null;
            layer = layers - 1;

            // PlayerPrefs.SetInt("OpenLevelTest", currentLevel);
            // PlayerPrefs.Save();

            if (levelData != null)
            {
                Initialize();
                return true;
            }

            return false;
        }

        #endregion
    }
}