using UnityEngine;

namespace Internal.Scripts.Items
{
    public class TrailRendererController : MonoBehaviour
    {
        private TrailRenderer trailRenderer;
        private bool isInitialized = false;

        // This method ensures the TrailRenderer is initialized before it's used
        private void EnsureInitialized()
        {
            if (!isInitialized)
            {
                trailRenderer = gameObject.AddComponent<TrailRenderer>();
                trailRenderer.autodestruct = false; // Set to true if you want it to be destroyed automatically after the trail ends
                isInitialized = true;
            }
        }

        // Public method to initialize with specific parameters (optional)
        public void Initialize(Material trailMaterial, float time = 0.5f, float startWidth = 0.5f, float endWidth = 0.1f)
        {
            EnsureInitialized();

            trailRenderer.material = trailMaterial;
            trailRenderer.time = time;
            trailRenderer.startWidth = startWidth;
            trailRenderer.endWidth = endWidth;

            // Set default transparency (fade out) over the trail's lifetime
            SetTrailColor(Color.white, Color.clear); // Default to white fading to transparent
        }

        // Method to activate or deactivate the TrailRenderer
        public void SetTrailActive(bool isActive)
        {
            EnsureInitialized();

            trailRenderer.enabled = isActive;
        }

        // Method to set the trail color dynamically, with a fade to transparency
        public void SetTrailColor(Color startColor, Color endColor)
        {
            EnsureInitialized();

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(startColor, 0.0f), // Start color
                    new GradientColorKey(endColor, 1.0f) // End color
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(startColor.a, 0.0f), // Full opacity at the start
                    new GradientAlphaKey(0.0f, 1.0f) // Fully transparent at the end
                }
            );
            trailRenderer.colorGradient = gradient;
        }

        // Method to destroy the TrailRenderer component
        public void DestroyTrail()
        {
            if (trailRenderer != null)
            {
                Destroy(trailRenderer);
                isInitialized = false; // Reset initialization flag
            }
        }

        // Method to destroy the TrailRenderer and this controller
        public void DestroyTrailAndController()
        {
            DestroyTrail(); // Destroy the trail renderer first
            Destroy(this); // Then destroy this component
        }
    }
}