using System;
using System.Collections;
using UnityEngine;

namespace Internal.Scripts.Effects
{
    /// <summary>
    /// Lightning effect between a DiscoBall item and a target item in the game.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LightningEffect : MonoBehaviour
    {
        public float speed = 10f;
        public float fadeOutTime = 1f;
        public float fadeInTime = 0.5f;
        public int segmentCount = 20;

        public LineRenderer lineRenderer;
        private Vector3 startPosition;
        private Vector3 targetPosition;

        private bool targetReached;
        public event Action OnLightningReachedTarget;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();

            lineRenderer.positionCount = segmentCount;
        }


        public void SetLight(Vector3 startPos, Vector3 endPos, int colorID)
        {
            Debug.Log($"Lightning: colorID is {colorID}");
            startPosition = startPos;
            targetPosition = endPos;
            transform.position = startPosition;

            Color color = GetColorById(colorID);
            lineRenderer.material.SetColor("_Color02", color);

            StartCoroutine(AnimateLightning());
        }

        private IEnumerator AnimateLightning()
        {
            float elapsedTime = 0f;
            Vector3[] positions = new Vector3[segmentCount];

            while (!targetReached)
            {
                elapsedTime += Time.deltaTime * speed;
                float distance = Mathf.Clamp01(elapsedTime);
                Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, distance);
                StartCoroutine(FadeInLightning());
                //  Create jagged line segments
                for (int i = 0; i < segmentCount; i++)
                {
                    float segmentProgress = (float)i / (segmentCount - 1);
                    Vector3 segmentPos = Vector3.Lerp(startPosition, currentPosition, segmentProgress);
                    positions[i] = segmentPos;
                }

                lineRenderer.SetPositions(positions);

                if (Vector3.Distance(currentPosition, targetPosition) < 0.1f)
                {
                    targetReached = true;
                    OnLightningReachedTarget?.Invoke();
                    yield return new WaitForSeconds(0.5f);
                    StartCoroutine(FadeOutLightning());
                }

                yield return null;
            }
        }

        private IEnumerator FadeOutLightning()
        {
            float elapsedTime = 0f;
            Color startColor = lineRenderer.startColor;
            Color endColor = lineRenderer.endColor;

            Vector2 initialTextureScale = lineRenderer.textureScale;
            Vector2 targetTextureScale = new Vector2(3f, 1.1f);

            while (elapsedTime < fadeOutTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeOutTime;


                Color currentStartColor = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0), t);
                Color currentEndColor = Color.Lerp(endColor, new Color(endColor.r, endColor.g, endColor.b, 0), t);

                lineRenderer.startColor = currentStartColor;
                lineRenderer.endColor = currentEndColor;


                lineRenderer.textureScale = Vector2.Lerp(initialTextureScale, targetTextureScale, t);

                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator FadeInLightning()
        {
            float elapsedTime = 0f;
            Color startColor = lineRenderer.startColor;
            Color endColor = lineRenderer.endColor;


            Color transparentStartColor = new Color(startColor.r, startColor.g, startColor.b, 0);
            Color transparentEndColor = new Color(endColor.r, endColor.g, endColor.b, 0);

            lineRenderer.startColor = transparentStartColor;
            lineRenderer.endColor = transparentEndColor;

            Vector2 initialTextureScale = lineRenderer.textureScale;
            Vector2 targetTextureScale = new Vector2(0.05f, 1.1f);

            while (elapsedTime < fadeInTime) // Using fadeInTime as duration; adjust if needed
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeInTime;

                // Fade in alpha from 0 to original color alpha
                Color currentStartColor = Color.Lerp(transparentStartColor, startColor, t);
                Color currentEndColor = Color.Lerp(transparentEndColor, endColor, t);

                lineRenderer.startColor = currentStartColor;
                lineRenderer.endColor = currentEndColor;

                // Lerp texture scale from (0, 1.1) to original scale
                lineRenderer.textureScale = Vector2.Lerp(initialTextureScale, targetTextureScale, t);

                yield return null;
                // Ensure final color and texture scale are fully set
                lineRenderer.startColor = startColor;
                lineRenderer.endColor = endColor;
                lineRenderer.textureScale = targetTextureScale;
            }
        }

        private Color GetColorById(int colorID)
        {
            // Define a color array where 1 = red, 2 = yellow, etc.
            Color[] colors = { Color.red, Color.yellow, Color.green, Color.blue };

            // Check if colorID is within bounds (0 to colors.Length - 1)
            if (colorID >= 0 && colorID < colors.Length)
            {
                return colors[colorID];
            }

            return Color.black; // Default color for invalid ID
        }
    }
}