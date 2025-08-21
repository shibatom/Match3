using UnityEngine;
using UnityEngine.UIElements;

namespace HelperScripts
{
    public class CustomImage : Image
    {
        public new class UxmlFactory : UxmlFactory<CustomImage, UxmlTraits>
        {
        }

        public new class UxmlTraits : Image.UxmlTraits
        {
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var customImage = (CustomImage)ve;
                // Initialize custom properties if any
            }
        }

        // Your custom methods and properties here
        public void SetCustomTintColor(Color color)
        {
            tintColor = color;
            MarkDirtyRepaint();
        }

        public void SetCustomImage(Texture2D texture)
        {
            image = texture;
            MarkDirtyRepaint();
        }

        public void SetCustomSprite(Sprite sprite)
        {
            this.sprite = sprite;
            MarkDirtyRepaint();
        }
    }
}