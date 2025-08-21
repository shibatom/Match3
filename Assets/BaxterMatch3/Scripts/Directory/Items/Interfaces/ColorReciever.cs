

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Internal.Scripts.Level;

namespace Internal.Scripts.Items.Interfaces
{
    [ExecuteAlways]
    public class ColorReciever : MonoBehaviour /* , IPoolable */
    {
        // Public fields
        public int color; // The color index of the component
        public List<SpritesInsideLevel> Sprites = new List<SpritesInsideLevel>(); // List of sprites per level
        public Sprite randomEditorSprite; // Sprite used for randomization in the editor
        public SpriteRenderer directSpriteRenderer; // Direct reference to the SpriteRenderer
        public GameObject shadowObject; // Reference to the shadow GameObject
        public List<SpritesInsideLevel> ShadowSprites = new List<SpritesInsideLevel>(); // List of shadow sprites per level

        // Private fields
        private Item itemComponent; // Reference to the Item component
        private ColorReciever[] iColorableComponents; // Array of IColorableComponent children
        private SpriteRenderer spriteRenderer; // Reference to the SpriteRenderer component
        private IColorChangable[] iColorChangables; // Array of IColorChangable children
        private SpriteRenderer shadowSpriteRenderer; // Reference to the shadow SpriteRenderer component
        private Material ShadowMaterial;

        private int shadowColor;

        // Public field with default value
        public bool RandomColorOnAwake = true; // Flag to randomize color on Awake

        // Called when the script instance is being loaded
        private void Awake()
        {
            itemComponent = GetComponent<Item>(); // Get the Item component
            iColorableComponents = GetComponentsInChildren<ColorReciever>(); // Get all IColorableComponent children
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // Get the SpriteRenderer component
            spriteRenderer.enabled = true;
            iColorChangables = GetComponentsInChildren<IColorChangable>(); // Get all IColorChangable children
            if (shadowObject)
                shadowObject.TryGetComponent(out shadowSpriteRenderer); // Get the shadow SpriteRenderer component
        }

        // Called when the object becomes enabled and active
        private void OnEnable()
        {
            ActivateShadow(false);
            if (itemComponent && !itemComponent.Combinable) color = GetHashCode(); // Set color to hash code if item is not combinable
            spriteRenderer.enabled = true; // Ensure the sprite renderer is enabled
        }

        public void SetColor(int _color)
        {
            if (_color < 0 || _color >= GetSprites(MainManager.Instance.currentLevel).Length) return; // Validate color index

            var component = itemComponent;
            if (component != null && component.currentType != ItemsTypes.DiscoBall)
                color = _color; // Set the color if the item is not multicolor

            if (directSpriteRenderer == null) directSpriteRenderer = spriteRenderer; // Set the direct sprite renderer if not already set

            if (GetSprites(MainManager.Instance.currentLevel).Length > 0 && directSpriteRenderer)
                directSpriteRenderer.sprite = GetSprites(MainManager.Instance.currentLevel)[_color]; // Set the sprite based on the color index

            foreach (var i in iColorChangables) i.OnColorChanged(_color); // Notify all IColorChangable children of the color change

            // Set the shadow sprite
            if (shadowSpriteRenderer != null && ShadowSprites.Count > 0)
            {
                Sprite sprite = GetShadowSprite(MainManager.Instance.currentLevel, _color);
                shadowSpriteRenderer.sprite = sprite;
                spriteRenderer.material.SetTexture("_ShadowTex", sprite.texture);
            }
        }

        // Method to randomize the color using a color generator
        public void RandomizeColor(IColorGettable colorGettable)
        {
            color = colorGettable.GenColor(itemComponent.square); // Generate a random color
            shadowColor = color;
            SetColor(color); // Set the generated color
            foreach (var i in iColorableComponents) i.SetColor(color); // Set the color for all IColorableComponent children
        }

        // Method to get the sprites for a specific level
        public Sprite[] GetSprites(int level)
        {
            if (level == 0) level = 1; // Default to level 1 if level is 0
            if (Sprites.Any(i => i.level == level))
                return Sprites.First(i => i.level == level).Sprites; // Return sprites for the specified level

            return Sprites[0].Sprites; // Return default sprites if level not found
        }

        // Method to get a specific sprite based on level and color
        public Sprite GetSprite(int level, int color)
        {
            var list = GetSprites(level);
            if (color < list.Length) return list[color]; // Return the sprite if color index is valid
            else if (list.Any()) return list[0]; // Return the first sprite if color index is invalid
            return null; // Return null if no sprites are available
        }

        // Method to get the shadow sprite for a specific level and color
        public Sprite GetShadowSprite(int level, int color)
        {
            var list = GetShadowSprites(level);
            if (color < list.Length) return list[color]; // Return the shadow sprite if color index is valid
            else if (list.Any()) return list[0]; // Return the first shadow sprite if color index is invalid
            return null; // Return null if no shadow sprites are available
        }

        // Method to get the shadow sprites for a specific level
        public Sprite[] GetShadowSprites(int level)
        {
            if (level == 0) level = 1; // Default to level 1 if level is 0
            if (ShadowSprites.Any(i => i.level == level))
                return ShadowSprites.First(i => i.level == level).Sprites; // Return shadow sprites for the specified level

            return ShadowSprites[0].Sprites; // Return default shadow sprites if level not found
        }

        // Method to get or add sprites for a specific level
        public Sprite[] GetSpritesOrAdd(int level)
        {
            if (Sprites.All(i => i.level != level))
            {
                var sprites = Sprites[0].Sprites;
                var other = new Sprite[sprites.Length];
                for (var i = 0; i < sprites.Length; i++) other[i] = sprites[i];

                Sprites.Add(new SpritesInsideLevel { level = level, Sprites = other }); // Add new sprites for the specified level
            }

            return GetSprites(level); // Return sprites for the specified level
        }

        // Method to activate the shadow
        public int ActivateShadow(bool activate)
        {
            // Debug.Log($"IcolorableComp: ActivateShadow Called and shadowObj is {shadowObject}");
            if (shadowObject != null)
            {
                shadowObject.SetActive(activate); // Activate or deactivate the shadow object
                if (activate)
                {
                    Material material = shadowObject.GetComponent<SpriteRenderer>().material;
                    if (shadowColor == 0)
                        material.SetColor("_Color", Color.red);
                    if (shadowColor == 1)
                        material.SetColor("_Color", Color.yellow);
                    if (shadowColor == 2)
                        material.SetColor("_Color", Color.green);
                    if (shadowColor == 3)
                        material.SetColor("_Color", Color.blue);
                    shadowObject.GetComponent<SpriteRenderer>().sprite = shadowSpriteRenderer.sprite;
                }
            }

            return shadowColor;
        }
    }
}