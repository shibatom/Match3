

using UnityEngine;
using UnityEngine.UI;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Score progress bar handler
    /// </summary>
    public class ProgressBarController : MonoBehaviour
    {
        Image slider;
        public static ProgressBarController Instance;
        float maxWidth;
        public GameObject[] stars;

        private static bool[] _starsAwarded = new bool[3];

        private void OnEnable()
        {
            Instance = this;
            slider = GetComponent<Image>();
            maxWidth = 1;
            MainManager.OnLevelLoaded += InitBar;
            stars[0].transform.GetChild(0).gameObject.SetActive(false);
            stars[1].transform.GetChild(0).gameObject.SetActive(false);
            stars[2].transform.GetChild(0).gameObject.SetActive(false);
            if (MainManager.GetGameStatus() > GameState.PrepareGame)
                InitBar();
        }

        private void OnDisable()
        {
            MainManager.OnLevelLoaded -= InitBar;
        }

        public void InitBar()
        {
            ResetBar();
            PrepareStars();
        }

        public void UpdateDisplay(float x)
        {
            slider.fillAmount = maxWidth * x;
            if (maxWidth * x >= maxWidth)
            {
                slider.fillAmount = maxWidth;

                //	ResetBar();
            }

            CheckStars();
        }

        public void AddValue(float x)
        {
            UpdateDisplay(slider.fillAmount * 100 / maxWidth / 100 + x);
        }

        // Update is called once per frame
        private void Update()
        {
            UpdateDisplay(MainManager.Score * 100f / (MainManager.Instance.levelData.star1 / ((MainManager.Instance.levelData.star1 * 100f / MainManager.Instance.levelData.star3)) * 100f) /
                          100f);
        }

        public bool IsFull()
        {
            if (slider.fillAmount >= maxWidth)
            {
                ResetBar();
                return true;
            }

            return false;
        }

        public void ResetBar()
        {
            UpdateDisplay(0.0f);
        }

        private void PrepareStars()
        {
            if (MainManager.Instance != null && MainManager.Instance?.levelData != null)
            {
                var width = GetComponent<RectTransform>().rect.width;
                stars[0].transform.localPosition = new Vector3(MainManager.Instance.levelData.star1 * 100f / MainManager.Instance.levelData.star3 * width / 100 - (width / 2f),
                    stars[0].transform.localPosition.y, 0);
                stars[1].transform.localPosition = new Vector3(MainManager.Instance.levelData.star2 * 100f / MainManager.Instance.levelData.star3 * width / 100 - (width / 2f),
                    stars[1].transform.localPosition.y, 0);
                stars[0].transform.GetChild(0).gameObject.SetActive(false);
                stars[1].transform.GetChild(0).gameObject.SetActive(false);
                stars[2].transform.GetChild(0).gameObject.SetActive(false);
            }
        }

        private void CheckStars()
        {
            var star1Anim = stars[0].transform.GetChild(0).gameObject;
            var star2Anim = stars[1].transform.GetChild(0).gameObject;
            var star3Anim = stars[2].transform.GetChild(0).gameObject;
            var Score = MainManager.Score;
            var levelData = MainManager.Instance?.levelData;
            if (levelData == null) return;

            if (Score >= levelData.star1)
            {
                if (!star1Anim.activeSelf && !_starsAwarded[0])
                    CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.getStarIngr);
                star1Anim.SetActive(true);
                _starsAwarded[0] = true;
            }

            if (Score >= levelData.star2)
            {
                if (!star2Anim.activeSelf && !_starsAwarded[1])
                    CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.getStarIngr);
                star2Anim.SetActive(true);
                _starsAwarded[1] = true;
            }

            if (Score >= levelData.star3)
            {
                if (!star3Anim.activeSelf && !_starsAwarded[2])
                    CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.getStarIngr);
                star3Anim.SetActive(true);
                _starsAwarded[2] = true;
            }
        }
    }
}