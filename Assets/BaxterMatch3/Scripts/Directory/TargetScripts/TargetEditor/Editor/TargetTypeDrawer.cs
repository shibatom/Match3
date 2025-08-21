

using System.Linq;
using Internal.Scripts.System;
using Internal.Scripts.TargetScripts.TargetEditor;
using Internal.Scripts.TargetScripts.TargetEditor.Editor;
using UnityEditor;
using UnityEngine;

namespace Internal.TargetScripts.TargetEditor.Editor
{
    [CustomPropertyDrawer(typeof(TargetType))]
    public class TargetTypeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            var targetType = property.FindPropertyRelative("type");
            targetType.intValue = EditorGUI.Popup(position, targetType.intValue, GetTargetsNames());
            if (EditorGUI.EndChangeCheck())
            {
                var targetsEditor = TargetEditorUtils.TargetEditorScriptable;
                var targetObject = PropertyUtilities.GetParent(property) as TargetObject;
                SpriteListt sprites = targetsEditor.GetTargetByName(GetTargetsNames()[targetType.intValue]).defaultSprites.FirstOrDefault()?.sprites.Copy();
                targetObject.sprites = sprites;
                var targetLevel = property.serializedObject.targetObject as TargetLevel;
                //Debug.LogError(" targetType.intValue " + targetType.intValue);
                targetObject.targetType.type = targetType.intValue;
                targetLevel.saveData();
                property.serializedObject.Update();
            }

            EditorGUI.EndProperty();
        }

        public string[] GetTargetsNames()
        {
            return TargetEditorUtils.TargetEditorScriptable.targets.Select(i => i.name).ToArray();
        }
    }
}