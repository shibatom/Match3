using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Internal.Scripts.Items
{
    public class TextureSwapManager : MonoBehaviour
    {
        public Texture2D textureA; // First texture
        public Texture2D textureB; // Second texture (alternative)
        private bool isUsingTextureA = true;

        private Dictionary<string, Sprite> textureASprites = new Dictionary<string, Sprite>();
        private Dictionary<string, Sprite> textureBSprites = new Dictionary<string, Sprite>();

        void Start()
        {
            // Preload sprite mappings for both textures
            LoadSprites(textureA, textureASprites);
            LoadSprites(textureB, textureBSprites);
        }

        private void LoadSprites(Texture2D texture, Dictionary<string, Sprite> spriteMap)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(texture.name);
            foreach (var sprite in sprites)
            {
                spriteMap[sprite.name] = sprite;
            }
        }

        public void SwapTexture()
        {
            isUsingTextureA = !isUsingTextureA; // Toggle between textures
            Dictionary<string, Sprite> currentSprites = isUsingTextureA ? textureASprites : textureBSprites;

            // Update all UI images
            Image[] uiImages = FindObjectsOfType<Image>();
            foreach (Image img in uiImages)
            {
                if (img.sprite != null && currentSprites.TryGetValue(img.sprite.name, out Sprite newSprite))
                {
                    img.sprite = newSprite; // Assign the matching sprite from the new texture
                }
            }

            Debug.Log($"Texture swapped! Now using {(isUsingTextureA ? "TextureA" : "TextureB")}");
        }
    }
}