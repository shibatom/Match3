

using UnityEngine;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Tutorial popup from the settings info button
    /// </summary>
    public class Tutorial : MonoBehaviour
    {
        public GameObject[] tutorials;
        private int _i;

        private void OnEnable()
        {
            foreach (var item in tutorials)
            {
                item.SetActive(false);
            }

            SetTutorial();
        }

        public void Next()
        {
            tutorials[_i].SetActive(false);
            _i++;
            _i = Mathf.Clamp(_i, 0, tutorials.Length - 1);
            SetTutorial();
        }

        public void Back()
        {
            tutorials[_i].SetActive(false);
            _i--;
            _i = Mathf.Clamp(_i, 0, tutorials.Length - 1);
            SetTutorial();
        }

        private void SetTutorial()
        {
            tutorials[_i].SetActive(true);
        }
    }
}