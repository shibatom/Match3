

using UnityEngine;

namespace Internal.Scripts.MapScripts
{
    public class MapLevel : MonoBehaviour
    {
        private Vector3 _originalScale;
        private bool _isScaled;
        public float OverScale = 1.05f;
        public float ClickScale = 0.95f;

        public int Number;
        public bool IsLocked;
        public Transform Lock;
        public Transform PathPivot;
        public Object LevelScene;
        public string SceneName;

        public int StarsCount;
        public Transform StarsHoster;
        public Transform Star1;
        public Transform Star2;
        public Transform Star3;

        public Transform SolidStarsHoster;
        public Transform SolidStars0;
        public Transform SolidStars1;
        public Transform SolidStars2;
        public Transform SolidStars3;
        public GameObject idleEffect;

        public void Awake()
        {
            _originalScale = transform.localScale;
        }

        #region Enable click

        public void OnMouseEnter()
        {
            Debug.LogError("OnMouseEnter  ");
            if (LevelCampaign.GetIsClickEnabled())
                Scale(OverScale);
        }

        public void OnMouseDown()
        {
            Debug.LogError("OnMouseDown  ");
            if (LevelCampaign.GetIsClickEnabled())
                Scale(ClickScale);
        }

        public void OnMouseExit()
        {
            Debug.LogError("OnMouseExit  ");
            if (LevelCampaign.GetIsClickEnabled())
                ResetScale();
        }

        private void Scale(float scaleValue)
        {
            transform.localScale = _originalScale * scaleValue;
            _isScaled = true;
        }

        public void OnDisable()
        {
            if (LevelCampaign.GetIsClickEnabled())
                ResetScale();
        }

        public void OnMouseUpAsButton()
        {
            Debug.LogError("OnMouseUpAsButton  ");
            if (LevelCampaign.GetIsClickEnabled())
            {
                ResetScale();
                LevelCampaign.OnLevelSelected(Number);
            }
        }

        private void ResetScale()
        {
            if (_isScaled)
                transform.localScale = _originalScale;
        }

        #endregion

        public void UpdateState(int starsCount, bool isLocked)
        {
            StarsCount = starsCount;
            UpdateStars(isLocked ? 0 : starsCount);
            IsLocked = isLocked;
            Lock.gameObject.SetActive(isLocked);
        }

        public void UpdateStars(int starsCount)
        {
            Star1?.gameObject.SetActive(starsCount >= 1);
            Star2?.gameObject.SetActive(starsCount >= 2);
            Star3?.gameObject.SetActive(starsCount >= 3);
        }

        public void UpdateStarsType(StarsType starsType)
        {
            StarsHoster.gameObject.SetActive(starsType == StarsType.Separated);
        }

        public void SetEffect()
        {
            //FindObjectsOfType<IdleCircleMapEffect>().ForEachY(x => Destroy(x.gameObject));
            var i = Instantiate(idleEffect, transform.position, Quaternion.identity, transform);
            i.transform.localScale = new Vector3(1.24f, 1, 1);
        }
    }
}