using UnityEngine;
using DG.Tweening;

namespace Internal.Scripts.Items
{
    public class Objectshaker : MonoBehaviour
    {
        private Vector3 originalPosition;

        //public OrientationGameCameraHandle orientationGameCameraHandle;
        private void OnEnable()
        {
            BombItem.OnShakeRequested += ShakeBoard; // Subscribe to event
            DiscoBallItem.OnShakeRequested += ShakeBoard; // Subscribe to event
            RocketItem.OnShakeRequested += ShakeBoard;
            MainManager.OnShakeRequested += ShakeBoard;
            // Store the initial position of the object when the game starts
            //  originalPosition = transform.localPosition;
        }

        private void OnDisable()
        {
            BombItem.OnShakeRequested -= ShakeBoard; // Unsubscribe to event
            DiscoBallItem.OnShakeRequested -= ShakeBoard; // Unsubscribe to event
            RocketItem.OnShakeRequested -= ShakeBoard; // Unsubscribe to event
            MainManager.OnShakeRequested -= ShakeBoard;
        }

        private void ShakeBoard(float duration, float strength)
        {
            var CamPos = MainManager.Instance.field.GetPosition();
            Vector2 cameraCenter = MainManager.Instance.orientationGameCameraHandle.GetCenterOffset();
            //  if (DOTween.IsTweening(transform))
            transform.DOKill();
            // Shake the object's position and return it to the original position after shaking
            transform.DOShakePosition(duration, strength, 20)
                .OnComplete(() => transform.localPosition = CamPos + cameraCenter);
            // transform.localPosition = CamPos + cameraCenter;
        }
    }
}