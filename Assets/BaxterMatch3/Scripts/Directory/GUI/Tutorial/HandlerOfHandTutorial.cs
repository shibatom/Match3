

using System.Collections;
using UnityEngine;
using Internal.Scripts.Items;

namespace Internal.Scripts.GUI
{
    public class HandlerOfHandTutorial : MonoBehaviour
    {
        public TutorialManager tutorialManager;
        private Item _tipItem;
        private Vector3 _vDirection;

        private void OnEnable()
        {
            _tipItem = ArtificialIntelligence.Instance.TipItem;
            _tipItem.tutorialUsableItem = true;
            MainManager.Instance.tutorialTime = true;
            _vDirection = ArtificialIntelligence.Instance.vDirection;
            PrepareAnimateHand();
        }

        private void PrepareAnimateHand()
        {
            var positions = tutorialManager.GetItemsPositions();
            StartCoroutine(AnimateHand(positions));
        }

        private IEnumerator AnimateHand(Vector3[] positions)
        {
            float speed = 1;
            var posNum = 0;

            transform.position = _tipItem.transform.position;
            posNum++;
            var offset = new Vector3(0.5f, -.5f);
            Vector2 startPos = transform.position + offset;
            Vector2 endPos = transform.position + _vDirection + offset;
            var distance = Vector3.Distance(startPos, endPos);
            float fracJourney = 0;
            var startTime = Time.time;

            while (fracJourney < 1)
            {
                var distCovered = (Time.time - startTime) * speed;
                fracJourney = distCovered / distance;
                transform.position = Vector2.Lerp(startPos, endPos, fracJourney);
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForFixedUpdate();
            PrepareAnimateHand();
        }
    }
}