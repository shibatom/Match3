

using System;
using System.Collections;
using Internal.Scripts;
using Internal.Scripts.Items;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Internal.Scripts.Effects
{
    /// <summary>
    /// Trail effect for win animations
    /// </summary>
    public class ImplementationOfTrailEffect : MonoBehaviour
    {
        public Item target;

        public GameObject explosion;

        public void StartAnim(Action<Item> callback_)
        {
            StartCoroutine(StartAnimation(callback_));
        }

        private IEnumerator StartAnimation(Action<Item> callback)
        {
            var offset = 2.5f;
            var duration = .3f;
            var curveX = new AnimationCurve(new Keyframe(0, transform.localPosition.x), new Keyframe(duration, target.transform.position.x));
            var curveY = new AnimationCurve(new Keyframe(0, transform.localPosition.y), new Keyframe(duration, target.transform.position.y));
            curveX.AddKey(duration * .5f, curveX.Evaluate(duration * .5f) + Random.Range(-offset, offset));
            curveY.AddKey(duration * .5f, curveY.Evaluate(duration * .5f) + Random.Range(-offset, offset));

            var startTime = Time.time;
            float distCovered = 0;
            while (distCovered < duration)
            {
                distCovered = (Time.time - startTime);
                transform.localPosition = new Vector3(curveX.Evaluate(distCovered), curveY.Evaluate(distCovered), 0);
                if (MainManager.Instance.skipWin) yield break;
                yield return new WaitForFixedUpdate();
            }

            var explosionObject = Instantiate(explosion);
            explosionObject.transform.position = transform.position;
            callback(target);
            yield return new WaitForSeconds(3);
            Destroy(gameObject);
        }
    }
}