

using UnityEditor;
using UnityEngine;
using Internal.Scripts.TargetScripts.TargetSystem;

namespace Internal.Scripts.Localization.Editor
{
    [CustomPropertyDrawer(typeof(LocalizationIndexFolderClass))]
    public class LanguageFoldDrawer : PropertyDrawer
    {
        private int _offset;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, "Text");
            if (property.isExpanded)
            {
                _offset = 5;
                position.y += EditorGUIUtility.singleLineHeight;
                ShowField(position, property, "description", "Description");
                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                ShowField(position, property, "failed", "Fail Description");
            }

            EditorGUI.EndProperty();
        }

        private void ShowField(Rect position, SerializedProperty property, string field, string label)
        {
            position.x += _offset;
            Rect r1 = position;
            r1.width = 100;
            EditorGUI.LabelField(r1, label);
            position.x += _offset;
            Rect r2 = position;
            r2.xMin = r1.xMax + 10;
            EditorGUI.PropertyField(r2, property.FindPropertyRelative(field));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2 : EditorGUIUtility.singleLineHeight;
        }
    }
}