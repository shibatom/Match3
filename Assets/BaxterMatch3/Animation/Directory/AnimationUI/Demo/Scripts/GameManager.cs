using UnityEngine;

namespace BaxterMatch3.Animation.Directory.AnimationUI.Demo
{
    public class GameManager : MonoBehaviour
    {
        public static int CurrentLevel
        {
            get => PlayerPrefs.GetInt("LastLevelPassed", 0);
            set => PlayerPrefs.SetInt("LastLevelPassed", value);
        }

        public static string Username
        {
            get
            {
#if UNITY_EDITOR
                return PlayerPrefs.GetString("UserName", $"ID_{Random.Range(0, 1000000)}");
#endif
                return PlayerPrefs.GetString("UserName", $"Guest_{Random.Range(0, 1000000)}");
            }
            set => PlayerPrefs.SetString("UserName", value);
        }

        void OnEnable()
        {
            AnimationUI.OnSetActiveAllInput += SetActiveAllInput;
        }

        void OnDisable()
        {
            AnimationUI.OnSetActiveAllInput -= SetActiveAllInput;
        }

        public void SetActiveAllInput(bool isActive)
        {
            transform.GetChild(0).gameObject.SetActive(!isActive);
        }
    }
}