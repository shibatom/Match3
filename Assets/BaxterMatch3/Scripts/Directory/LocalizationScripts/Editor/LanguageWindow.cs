

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Internal.Scripts.Localization.Editor
{
    public class LanguageWindow : EditorWindow
    {
        private static List<TempStruct> _array;
        private static LanguageWindow _window;
        private Vector2 _scrollPos;
        private SystemLanguage _lang;
        private Dictionary<int, string> _dic;
        private IOrderedEnumerable<LanguageAndText> _findObjectsOfLocalizeText;
        private List<TempStruct> _list;

        // Add menu item named "My Window" to the Window menu
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            _window = (LanguageWindow)GetWindow(typeof(LanguageWindow));
            _window.Show();
        }

        private void OnEnable()
        {
            _lang = SystemLanguage.English;
        }

        struct TempStruct
        {
            public GameObject obj;
            public int id;
            public string text;
        }

        public void OnFocus()
        {
            _findObjectsOfLocalizeText = Resources.FindObjectsOfTypeAll<LanguageAndText>().OrderBy(i => i.instanceID);
            LanguageManager.LoadLanguage(_lang.ToString());
            _dic = LanguageManager._dic;
        }

        void OnGUI()
        {
            _lang = (SystemLanguage)EditorGUILayout.EnumPopup(_lang);
            _list = GetList();
            if (GUILayout.Button("Save"))
            {
            }

            EditorGUILayout.BeginVertical();
            _scrollPos =
                EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Width(position.width), GUILayout.Height(position.height - 100));
            foreach (var langLine in _list)
            {
                GUILayout.BeginHorizontal();
                {
                }
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        List<TempStruct> GetList()
        {
            List<TempStruct> list = new List<TempStruct>();
            foreach (var langLine in _dic)
            {
                var l = _findObjectsOfLocalizeText.Where(i => i.instanceID == langLine.Key);
                list.AddRange(l.Select(localizeText => new TempStruct { obj = localizeText.gameObject, id = localizeText.instanceID, text = langLine.Value }));
            }

            return list;
        }
    }
}