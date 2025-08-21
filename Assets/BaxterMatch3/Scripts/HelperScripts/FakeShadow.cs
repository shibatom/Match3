using UnityEngine;

namespace HelperScripts
{
    public class FakeShadow : MonoBehaviour
    {
        [SerializeField] private Vector3 shadowOffset = new Vector3(0.1f, -0.1f, 0); // Offset for the shadow

        private GameObject shadowSprite; // Reference to the shadow sprite
        private bool isAnimating = false; // Flag to track animation state
        private SpriteRenderer originalRenderer;
        private SpriteRenderer shadowRenderer;

        void Awake()
        {
            originalRenderer = GetComponent<SpriteRenderer>();
            if (originalRenderer == null)
            {
                Debug.LogError("No SpriteRenderer found on the object.");
            }
        }

        private void CreateShadowSprite()
        {
            if (shadowSprite != null) return;

            // Create new GameObject for shadow
            shadowSprite = new GameObject("Shadow");
            shadowSprite.transform.SetParent(this.transform.parent); // Same parent as original object
            shadowSprite.transform.position = transform.position + shadowOffset;
            // Add and setup SpriteRenderer
            shadowRenderer = shadowSprite.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = originalRenderer.sprite;
            shadowRenderer.sortingOrder = originalRenderer.sortingOrder - 1; // Place behind original sprite
            shadowSprite.transform.localScale = new Vector3(0.6f, 0.6f, 0);
            shadowRenderer.sortingLayerName = this.GetComponent<SpriteRenderer>().sortingLayerName;

            // Set shadow color (black with reduced alpha)
            shadowRenderer.color = new Color(0, 0, 0, 0.5f);

            // Initially set inactive
            shadowSprite.SetActive(false);
        }

        void Update()
        {
            if (isAnimating && shadowSprite != null)
            {
                // Update the shadow's position with offset
                shadowSprite.transform.position = transform.position + shadowOffset;
                shadowSprite.transform.rotation = transform.rotation;


                // Update sprite if original sprite changes
                if (shadowRenderer.sprite != originalRenderer.sprite)
                {
                    shadowRenderer.sprite = originalRenderer.sprite;
                }
            }
        }

        public void StartShadowAnimation()
        {
            if (originalRenderer == null) return;

            CreateShadowSprite();
            isAnimating = true;
            shadowSprite.SetActive(true);
        }

        public void StopShadowAnimation()
        {
            isAnimating = false;
            if (shadowSprite != null)
            {
                shadowSprite.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (shadowSprite != null)
            {
                Destroy(shadowSprite);
            }
        }
    }
}