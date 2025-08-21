

using UnityEditor;
using UnityEngine;

namespace Internal.Scripts.TargetScripts.TargetEditor.Editor
{
    [CustomPropertyDrawer(typeof(BooleanScoreShow))]
    public class ShowScoreDrawerGUI : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var show = property.FindPropertyRelative("ShowTheScore");
            var targetContainer = TargetEditorUtils.GetTargetContainer(property);
            if (targetContainer != null && targetContainer.name=="Stars")
                EditorGUI.PropertyField(position, show);

            EditorGUI.EndProperty();
        }
    }
}