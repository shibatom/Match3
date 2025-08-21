using System.Collections.Generic;
using Internal.Scripts;
using Internal.Scripts.MapScripts;
using Internal.Scripts.System;
using UnityEngine;
using UnityEngine.UI;


namespace HelperScripts
{
    public class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private GameObject changeNamePopup;
        [SerializeField] private GameObject soundOffIndicator;
        [SerializeField] private GameObject musicOffIndicator;
        [SerializeField] private GameObject vibrationOffIndicator;


        private void OnEnable()
        {
            soundOffIndicator.SetActive(!GlobalValue.IsSoundOn);
            musicOffIndicator.SetActive(!GlobalValue.IsMusicOn);
            vibrationOffIndicator.SetActive(!GlobalValue.IsVibrationOn);
            ReferencerUI.Instance.PlayButton.SetActive(false);
            MapCamera.IsPopupOpen = true;
        }

        private void OnDisable()
        {
            ReferencerUI.Instance.PlayButton.SetActive(true);
            MapCamera.IsPopupOpen = false;
        }

        public void SoundButtonFunction()
        {
            GlobalValue.IsSoundOn = !GlobalValue.IsSoundOn;
            soundOffIndicator.SetActive(!GlobalValue.IsSoundOn);
            MainManager.PlayButtonClickSound();
        }

        public void MusicButtonFunction()
        {
            GlobalValue.IsMusicOn = !GlobalValue.IsMusicOn;
            musicOffIndicator.SetActive(!GlobalValue.IsMusicOn);
            MainManager.PlayButtonClickSound();
            CentralMusicManager.Instance.ChangeMusicPlayStatus();
        }

        public void VibrationButtonFunction()
        {
            GlobalValue.IsVibrationOn = !GlobalValue.IsVibrationOn;
            vibrationOffIndicator.SetActive(!GlobalValue.IsVibrationOn);
            MainManager.PlayButtonClickSound();
        }

        public void ChangeNameButtonFunction()
        {
            Instantiate(changeNamePopup, transform);
            MainManager.PlayButtonClickSound();
        }

        public Texture2D textureA; // First texture
        public Texture2D textureB; // Second texture (alternative)
        private bool isUsingTextureA = true;
        public Sprite[] sprites;
        public Image[] uiImages;

        private Dictionary<string, Sprite> textureASprites = new Dictionary<string, Sprite>();
        private Dictionary<string, Sprite> textureBSprites = new Dictionary<string, Sprite>();

        void Start()
        {
            // Preload sprite mappings for both textures
            LoadSprites(textureA, textureASprites);
            LoadSprites(textureB, textureBSprites);

            // If uiImages isn't assigned in the Inspector, find all UI Images in the scene
            if (uiImages == null || uiImages.Length == 0)
            {
                uiImages = FindObjectsOfType<Image>(true);
            }
        }

        private void LoadSprites(Texture2D texture, Dictionary<string, Sprite> spriteMap)
        {
            if (texture == null)
            {
                Debug.LogError($"Texture is NULL: {texture}");
                return;
            }

            // Load all sprites from the texture (assumes they are in Resources folder with the same name as the texture)
            Sprite[] sprites = Resources.LoadAll<Sprite>(texture.name);

            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogError($"No sprites found for texture: {texture.name}");
                return;
            }

            foreach (var sprite in sprites)
            {
                spriteMap[sprite.name] = sprite;
                Debug.Log($"Loaded Sprite: {sprite.name} from {texture.name}");
            }
        }

        public void SwapTexture()
        {
            isUsingTextureA = !isUsingTextureA;
            Dictionary<string, Sprite> currentSpriteMap = isUsingTextureA ? textureASprites : textureBSprites;

            Debug.Log($"Swapping to {(isUsingTextureA ? "TextureA" : "TextureB")}");

            // Refresh the list to include active images
            uiImages = FindObjectsOfType<Image>(true);

            foreach (Image img in uiImages)
            {
                if (img.sprite != null)
                {
                    string spriteName = img.sprite.name;
                    if (currentSpriteMap.ContainsKey(spriteName))
                    {
                        img.sprite = currentSpriteMap[spriteName];
                        Debug.Log($"Swapped {img.name} to sprite: {spriteName} from {(isUsingTextureA ? "TextureA" : "TextureB")}");
                    }
                    else
                    {
                        Debug.LogWarning($"Sprite '{spriteName}' not found in {(isUsingTextureA ? "TextureA" : "TextureB")}'s sprite map.");
                    }
                }
                else
                {
                    Debug.LogWarning($"UI Image {img.name} has no sprite assigned.");
                }
            }

            Canvas.ForceUpdateCanvases();
            Debug.Log("UI refreshed after texture swap.");
        }

        public void Close()
        {
            MainManager.PlayButtonClickSound();
            gameObject.SetActive(false);
        }
    }
}