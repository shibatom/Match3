using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

namespace StorySystem
{
    public static class imageFader
    {
        private static Image CreateTempChildImage(Image image)
        {
            Image result = null;

            string tempChildName = GetTempChildName(image);
            Transform foundTransform = image.transform.Find(tempChildName);
            GameObject tempGo = foundTransform != null ? foundTransform.gameObject : null;

            if (tempGo == null)
            {
                tempGo = new GameObject("TempCloneChild");
                var rt = image.GetComponent<RectTransform>();

                var rtPrime = tempGo.AddComponent<RectTransform>();
                rtPrime.SetParent(rt);
                rtPrime.localScale = Vector3.one;
                rtPrime.anchorMin = Vector2.zero;
                rtPrime.anchorMax = Vector2.one;
                rtPrime.sizeDelta = Vector2.zero;
                rtPrime.anchoredPosition = Vector2.zero;

                result = tempGo.AddComponent<Image>();
                result.preserveAspect = image.preserveAspect;
            }
            else
            {
                result = tempGo.GetComponent<Image>();
            }

            return result;
        }

        private static string GetTempChildName(Image target) => string.Format("TempCloneChild_{0}", target.GetInstanceID());

        public static float GetAlpha(this Image image) => image.color.a;

        public static void SetAlpha(this Image image, float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }


        private static void RemoveTempChildImage(Image childImage)
        {
            if (childImage != null)
            {
                Object.Destroy(childImage.gameObject);
            }
        }

        public static Tweener DOCrossfadeImage(this Image image, Sprite to, float duration, System.Action OnComplete = null)
        {
            Image childImage = CreateTempChildImage(image);
            float progress = 0f;
            const float finalAlpha = 1f;

            childImage.SetAlpha(0f);
            childImage.sprite = to;

            return DOTween.To(
                    () => progress,
                    (curProgress) =>
                    {
                        progress = curProgress;
                        float childAlpha = finalAlpha * progress;
                        float imageAlpha = finalAlpha - childAlpha;
                        image.SetAlpha(imageAlpha);
                        childImage.SetAlpha(childAlpha);
                    },
                    1f, duration)
                .OnComplete(() =>
                {
                    image.sprite = to;
                    image.SetAlpha(childImage.GetAlpha());
                    RemoveTempChildImage(childImage);
                    OnComplete?.Invoke();
                })
                .OnKill(() =>
                {
                    //Note: If you expect this tween will cancel and wish to halt the
                    //  animation, remove this next line. It will look bad when you
                    //  start another CrossFadeImage animation on this
                    RemoveTempChildImage(childImage);
                });
        }
    }
}