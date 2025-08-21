

using UnityEngine;
using System;
using System.Collections.Generic;
using Internal.Scripts.Level;
using Internal.Scripts.System;

namespace Internal.Scripts.Localization
{
    public class LanguageManager : MonoBehaviour
    {
        public static LanguageManager Instance;
        private static DebugSettings _debugSettings;
        public static Dictionary<int, string> _dic;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
            DontDestroyOnLoad(this);
            _debugSettings = Resources.Load("Scriptable/DebugSettings") as DebugSettings;
            LoadLanguage(GetSystemLanguage(_debugSettings));
        }

        public static void LoadLanguage(string language)
        {
            var txt = Resources.Load<TextAsset>("Localization/" + language);
            if (txt == null) txt = Resources.Load<TextAsset>("Localization/" + SystemLanguage.English);
            _dic = new Dictionary<int, string>();
            string[] lines = txt.text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string inp_ln in lines)
            {
                string[] l = inp_ln.Split(':');
                var n = l[0];
                var text = l[1];
                _dic.Add(int.Parse(n), text.Trim());
            }
        }

        public static string GetSystemLanguage(DebugSettings _debugSettings)
        {
            if (PersistantData.SelectedLanguage != null) return PersistantData.SelectedLanguage;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
                return _debugSettings.TestLanguage.ToString();
            return Application.systemLanguage.ToString();
        }

        public static string GetText(int instanceId, string defaultText)
        {
            if (_dic == null || _dic.Count == 0)
            {
                //Debug.LogError(GetSystemLanguage(_debugSettings) + " language file not exist");
                return "";
            }

            return _dic[instanceId] != "" ? _dic[instanceId] : defaultText;
        }
    }
}