

using Internal.Scripts.System;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEditor;

namespace Internal.Scripts.TargetScripts.TargetEditor.Editor
{
    public static class TargetEditorUtils
    {
        private static TargetEditorScriptable target;

        public static TargetEditorScriptable TargetEditorScriptable
        {
            get
            {
                if (target == null)
                {
                    target = AssetDatabase.LoadAssetAtPath<TargetEditorScriptable>("Assets/BaxterMatch3/Directory/Resources/Levels/TargetEditorScriptable.asset");
                }

                return target;
            }
        }

        public static TargetContainer GetTargetContainer(SerializedProperty property)
        {
            var propertyParent = PropertyUtilities.GetParent(property) as TargetObject;
            return TargetEditorScriptable.GetTargetByName(propertyParent.targetType.GetTarget().name);
        }
    }
}