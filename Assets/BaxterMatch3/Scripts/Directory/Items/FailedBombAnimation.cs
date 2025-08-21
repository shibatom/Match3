

using System.Collections;
using Internal.Scripts.Effects;
using Internal.Scripts.Items.Interfaces;
using Internal.Scripts.System.Pool;
using UnityEngine;
using UnityEngine.Events;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Time bomb explosion animation
    /// </summary>
    public class FailedBombAnimation : MonoBehaviour
    {
        public GameObject obj;
        [SerializeField] private GameObject wave;

        public void BombFailed(Vector3 endPos, float speed, bool hide, UnityAction myCallback)
        {
            StartCoroutine(BombFailedCor(endPos, speed, hide, myCallback));
        }

        IEnumerator BombFailedCor(Vector3 endPos, float speed, bool hide, UnityAction myCallback)
        {
            obj.GetComponent<SpriteRenderer>().sortingOrder = 5;

            Vector3 startPos = obj.transform.position;
            float startTime = Time.time;
            float distance = Vector3.Distance(startPos, endPos);
            Vector2 startScale = obj.transform.localScale;
            Vector2 endtScale = startScale * 1.3f;
            float fracJourney = 0;
            if (distance > 0.5f)
            {
                while (fracJourney < 1)
                {
                    // speed += 0.2f;
                    float distCovered = (Time.time - startTime) * speed;
                    fracJourney = distCovered / distance;
//					obj.transform.position = Vector2.Lerp (startPos, endPos, fracJourney);
                    obj.transform.localScale = Vector2.Lerp(startScale, endtScale, fracJourney);
                    yield return new WaitForFixedUpdate();
                }
            }

//			if(hide) obj.SetActive(false);
            startTime = Time.time;
            Vector3 originPosition = obj.transform.position;
            Quaternion originRotation = obj.transform.rotation;
            float shake_intensity = 0.03f;
            float shake_decay = 0.002f;
            while (Time.time - startTime < 1)
            {
                obj.transform.position = originPosition + Random.insideUnitSphere * shake_intensity;
                obj.transform.rotation = Quaternion.Euler(
                    originRotation.x + Random.Range(-shake_intensity, shake_intensity) * 0.2f,
                    originRotation.y + Random.Range(-shake_intensity, shake_intensity) * 0.2f,
                    originRotation.z + Random.Range(-shake_intensity, shake_intensity) * 0.2f);
                shake_intensity -= shake_decay;
                yield return new WaitForFixedUpdate();
            }

            CentralSoundManager.Instance.PlayLimitSound(CentralSoundManager.Instance.explosion);

            startTime = Time.time;
//			if (!hide)
            {
                var partcl2 = ObjectPoolManager.Instance.GetPooledObject("FireworkSplash");
                if (partcl2 != null)
                {
                    partcl2.transform.position = obj.transform.position;
                    partcl2.GetComponent<SplashEffectParticles>().SetColor(GetComponent<ColorReciever>().color);
                }

                GetComponent<TimeBombItem>().square.Item = null;
                GetComponent<TimeBombItem>().square = null;
                obj.SetActive(false);
            }
            if (wave != null)
            {
                wave.SetActive(true);
                wave.transform.position = endPos;
                wave.GetComponent<SpriteRenderer>().sortingOrder = 15;
                while (Time.time - startTime < 1)
                {
                    wave.transform.localScale += Vector3.one * Time.deltaTime * 25;
                    wave.transform.Translate(0, 0, Time.deltaTime * 150);
                    yield return new WaitForFixedUpdate();
                }

                wave.SetActive(false);
            }

            myCallback?.Invoke();
        }
    }
}