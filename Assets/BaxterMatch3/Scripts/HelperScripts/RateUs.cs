using UnityEngine;

namespace HelperScripts
{
    public class RateUs : MonoBehaviour
    {
        private const string RateUrl = "market://details?id=com.example.android ";

        public void Rate()
        {
#if UNITY_ANDROID
            Application.OpenURL(RateUrl);
#elif UNITY_IOS
            // Application.OpenURL(InitScript.Instance.RateURLIOS);
#endif
            GlobalValue.UserHasRated = true;
            Close();
        }

        public void Close()
        {
            Destroy(gameObject);
        }
    }
}