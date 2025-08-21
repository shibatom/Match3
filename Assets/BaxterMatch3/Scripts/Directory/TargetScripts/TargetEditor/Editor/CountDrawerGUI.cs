

using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEngine;

namespace Internal.Scripts.TargetScripts.TargetEditor.Editor
{
    [CustomPropertyDrawer(typeof(CountClass))]
    public class CountDrawerGUI : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var count = property.FindPropertyRelative("count");
            var targetContainer = TargetEditorUtils.GetTargetContainer(property);
            if (targetContainer != null && targetContainer.setCount == SetCount.Manually)
                EditorGUI.PropertyField(position, count);

            EditorGUI.EndProperty();
        }
    }
}