#if UNITY_EDITOR
using ParrelSync;
#endif

namespace NakamaOnline
{
    /// <summary>
    /// Stores commonly used values which may be changed at edit-time.
    /// </summary>
    public static class GameConstants
    {
        private const string CreateAssetMenu = "NakamaOnline/";
        public const string CreateAssetMenuGameConnection = CreateAssetMenu + "GameConnection";
        public const string LeaderboardId = "Match3_SET";
#if UNITY_EDITOR
        public static string DeviceIdKey = "DeviceId" + (ClonesManager.IsClone() ? "clone" : "");
        public static string AuthTokenKey = "nakama.authToken" + (ClonesManager.IsClone() ? "clone" : "");
        public static string RefreshTokenKey = "nakama.refreshToken" + (ClonesManager.IsClone() ? "clone" : "");
#else
        public static string DeviceIdKey = "NakamaDeviceIdKey";
        public static string AuthTokenKey = "nakama.authToken";
        public static string RefreshTokenKey = "nakama.refreshToken";
#endif
    }
}