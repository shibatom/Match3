

using UnityEngine;

namespace Internal.Scripts
{
    public class GeneralDataProtectionRegulation : MonoBehaviour
    {
        public GDPRPopupManager go;

        void Start()
        {
            Invoke("CheckForGDPR", 1f);
        }

        private void CheckForGDPR()
        {
            if (PlayerPrefs.GetInt("npa", -1) == -1)
            {
                go.gdprObject.SetActive(true);
                Time.timeScale = 0;
            }
        }

        public void OnUserClickAccept()
        {
            PlayerPrefs.SetInt("npa", 0);
            go.gdprObject.SetActive(false);
            Time.timeScale = 1;
        }

        public void OnUserClickCancel()
        {
            PlayerPrefs.SetInt("npa", 1);
            go.gdprObject.SetActive(false);
            Time.timeScale = 1;
        }

        public void OnUserClickPrivacyPolicy()
        {
            // Add Your Privacy Policy Address Here
            Application.OpenURL("https://privacyPolicy");
        }
    }
}