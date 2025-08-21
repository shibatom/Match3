using UnityEngine;

namespace Internal.Scripts.Items.Interfaces
{
    public class IColorShadow : MonoBehaviour
    {
        public SpriteRenderer directSpriteRenderer;

        public ColorReciever colorableComponent;

        // Start is called before the first frame update
        void Awake()
        {
            colorableComponent = this.GetComponentInParent<ColorReciever>();
            directSpriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}