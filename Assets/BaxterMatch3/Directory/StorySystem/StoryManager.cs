using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Internal.Scripts.MapScripts;
using Internal.Scripts.Level;


namespace StorySystem
{
    public class StoryManager : MonoBehaviour
    {
        // Singleton instance
        public static StoryManager Instance;

        // Story state
        public bool hasStoryFinished = false;

        // UI elements
        public GameObject[] storylines;
        public GameObject conversation;
        public GameObject specificGameObject;
        public GameObject characterCanvas;
        public GameObject characterFrame;
        public GameObject petRescue;
        public GameObject[] lockedImages;

        // Map camera
        public MapCamera mapCamera;
        public WaypointsMover WaypointsMover;

        // Story elements
        public Image backgroundStory1;
        public Image invertedMask;
        public Image backgroundStory2;

        // Animation parameters
        public float targetAlpha = 1f;
        public float lerpSpeed = 0.1f;

        // Level data
        private LevelData levelData;
        private int currentLevel;

        // Events
        public event EventHandler OnAlphaChanged;

        public delegate void LevelChangedEventHandler(int levelNumber);

        public LevelChangedEventHandler OnLevelChanged;

        // Initialization
        private void Awake()
        {
            Instance = this; // Singleton instance
        }

        private void Start()
        {
        }

        // Enable and disable event listeners
        private void OnEnable()
        {
            // Add necessary event listeners here
        }

        private void OnDisable()
        {
            // Remove necessary event listeners here
        }

        // Event handler for alpha change
        private void StoryManager_OnAlphaChanged(object sender, EventArgs e)
        {
            HighlightSelectedObject(true);
        }

        // Check if level contains story
        public bool HasLevelContainsStory(int level)
        {
            levelData = LoadingController.LoadlLevel(level, levelData);
            Conversation levelConversation = levelData.GetConversation();

            if (levelConversation != null)
            {
                currentLevel = level;
                ConversationController conversationController = conversation.GetComponent<ConversationController>();
                conversationController.SetLevelStoryConversation(levelData, levelConversation);
                return true;
            }

            return false;
        }

        public bool HasLEvelContainsCard(int level)
        {
            levelData = LoadingController.LoadlLevel(level, levelData);
            CardConfig levelCard = levelData.GetCard();

            if (levelCard != null)
            {
                return true;
            }

            return false;
        }

        // Start the story
        public void StartStory()
        {
            var conversationController = Instance.conversation.GetComponent<ConversationController>();
            conversationController.AdvanceLine();
        }

        // Stop the story
        public void StoryStop()
        {
            ShowRescuePet(false);

            HighlightSelectedObject(false);

            ShowMapInStory();

            TiltCamera(false);

            gameObject.SetActive(false);
            hasStoryFinished = true;
        }

        public void ShowStoryBackground(Sprite sprite)
        {
            invertedMask.DOFade(1, 1);
            CrossFadeBackground(sprite);
        }

        // Show story tutorial
        public void ShowStoryTutorial()
        {
            //OnAlphaChanged += StoryManager_OnAlphaChanged;

            StartCoroutine(IncreaseAlphaCoroutine(invertedMask, 0.5f, lerpSpeed));

            TiltCamera(false);

            HighlightSelectedObject(true);
        }

        public void fadeInvertedMask()
        {
            invertedMask.DOFade(0.5f, 1);
        }

        // Show map in the story
        public void ShowMapInStory()
        {
            backgroundStory2.DOFade(0f, 1f);
            backgroundStory1.DOFade(0f, 1f);
            invertedMask.DOFade(0f, 1f);
        }

        private void CrossFadeBackground(Sprite sprite)
        {
            backgroundStory2.DOCrossfadeImage(sprite, 1f);
        }

        // Highlight the first level
        public void HighLightFirstLevel()
        {
            // Add implementation here
        }

        // Coroutine to decrease alpha
        private IEnumerator DecreaseAlphaCoroutine(Image image, float targetAlpha, float lerpSpeed)
        {
            float alpha = image.color.a;
            while (alpha > targetAlpha + 0.01)
            {
                float newAlpha = Mathf.Lerp(alpha, targetAlpha, lerpSpeed);
                image.color = new Color(image.color.r, image.color.g, image.color.b, newAlpha);
                yield return null;
                alpha = newAlpha;
            }

            OnAlphaChanged?.Invoke(this, EventArgs.Empty);
            yield break;
        }

        // Coroutine to increase alpha
        private IEnumerator IncreaseAlphaCoroutine(Image image, float targetAlpha, float lerpSpeed)
        {
            float alpha = image.color.a;

            while (alpha < targetAlpha)
            {
                float newAlpha = Mathf.Lerp(alpha, targetAlpha * 2, lerpSpeed);
                image.color = new Color(image.color.r, image.color.g, image.color.b, newAlpha);
                yield return null;
                alpha = newAlpha;
            }

            OnAlphaChanged?.Invoke(this, EventArgs.Empty);
            yield break;
        }

        // Show rescue pet
        public void ShowRescuePet(bool turnFlag = false)
        {
            TurnInvertedMask(turnFlag);

            HighLightPetRescue(turnFlag);

            TiltCamera(turnFlag);
        }

        // Tilt camera
        public void TiltCamera(bool turnFlag)
        {
            int lastLevelReached = LevelCampaign.GetLastReachedLevel();
            Vector3 CharacterPosition = LevelCampaign.Instance.getCharacterPosition();
            float targetPositionYaxix = CharacterPosition.y;

            if (turnFlag)
            {
                targetPositionYaxix = lockedImages[currentLevel - 1].transform.position.y - 2;
            }

            mapCamera.gameObject.transform.DOLocalMoveY(targetPositionYaxix, 1);
        }

        private void TurnInvertedMask(bool turnFlag)
        {
            if (turnFlag)
                invertedMask.DOFade(0.5f, 0.5f);
            else
                invertedMask.DOFade(0, 0.5f);
        }

        // Highlight selected object
        void HighlightSelectedObject(bool turnFlag)
        {
            int lastLevelReached = LevelCampaign.GetLastReachedLevel();
            SpriteRenderer specificSprite = specificGameObject.transform.Find($"Level{lastLevelReached:D2}").GetComponent<SpriteRenderer>();
            Canvas characterCanvasCanvas = characterCanvas.GetComponent<Canvas>();
            SpriteRenderer characterFrameSprite = characterFrame.GetComponent<SpriteRenderer>();

            if (turnFlag)
            {
                specificSprite.sortingLayerName = "UI";
                specificSprite.sortingOrder = 8;
                characterCanvasCanvas.sortingLayerName = "UI";
                characterFrameSprite.sortingLayerName = "UI";
            }
            else
            {
                specificSprite.sortingLayerName = "Default";
                specificSprite.sortingOrder = 1;
                characterCanvasCanvas.sortingLayerName = "Default";
                characterFrameSprite.sortingLayerName = "Default";
            }
        }

        void HighLightPetRescue(bool turnFlag)
        {
            Debug.Log("sallog laslevel " + currentLevel);
            SpriteRenderer petRescueSprite = lockedImages[currentLevel - 1].gameObject.GetComponent<SpriteRenderer>();
            if (turnFlag)
                petRescueSprite.sortingLayerName = "UI";
            else
                petRescueSprite.sortingLayerName = "Default";
        }
    }
}