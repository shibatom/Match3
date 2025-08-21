using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Malee.List;
using TMPro;
using Internal.Scripts.System;

namespace Internal.Scripts.Localization
{
    public class LanguageAndText : MonoBehaviour
    {
        private TextMeshProUGUI textObject;
        public int instanceID;

        [SerializeField, Reorderable(elementNameProperty = "language"), HideInInspector]
        public TmpList objects;

        private string _originalText;
        private string _currentText;

        private void Awake()
        {
            textObject = GetComponent<TextMeshProUGUI>();
        }

        private void OnEnable()
        {
            //take text from target editor
            if (instanceID == 33 || instanceID == 45) return;
            _originalText = textObject.text;
            _currentText = LanguageManager.GetText(instanceID, _originalText);
            textObject.text = _currentText;
        }
    }

    [Serializable]
    public class TmpList : ReorderableArray<LanguageObject>
    {
        private SystemLanguage GetSystemLanguage(DebugSettings _debugSettings)
        {
            SystemLanguage lang;
            if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
                lang = _debugSettings.TestLanguage;
            else
                lang = Application.systemLanguage;
            return this.Any(i => i.language == lang) ? _debugSettings.TestLanguage : SystemLanguage.English;
        }

        public IEnumerable<LanguageObject> GetText(DebugSettings _debugSettings)
        {
            var systemLanguage = GetSystemLanguage(_debugSettings);
            return GetText(systemLanguage);
        }

        public IEnumerable<LanguageObject> GetText(SystemLanguage systemLanguage)
        {
            return this.Where(i => i.language == systemLanguage);
        }
    }
}