

using UnityEngine;

namespace Internal.Scripts.Integrations
{
    [CreateAssetMenu(fileName = "UnityAdsID", menuName = "UnityAdsID", order = 1)]
    public class UnityAdsHolder : ScriptableObject
    {
        public bool enable;
        public string androidID;
        public string iOSID;
        [Space(20)] public string unityRewardedAndroid;
        public string unityRewardediOS;
        public string unityInterstitialAndroid;
        public string unityInterstitialiOS;
    }
}