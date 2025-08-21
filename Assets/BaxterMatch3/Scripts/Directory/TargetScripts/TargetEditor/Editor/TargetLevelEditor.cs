

using Internal.Scripts.Level;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEngine;

namespace Internal.Scripts.TargetScripts.TargetEditor.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TargetLevel))]
    public class TargetLevelEditor : UnityEditor.Editor
    {
        private TargetLevel targetLevel;
        private LevelData levelData;
        private TargetEditorScriptable targetsEditor;

        private void OnEnable()
        {
            var currentLevel = int.Parse(serializedObject.targetObject.name.Replace("TargetLevel", ""));
            targetsEditor = TargetEditorUtils.TargetEditorScriptable;
            levelData = LoadingController.LoadlLevel(currentLevel, levelData);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            serializedObject.Update();
            targetLevel = (TargetLevel)target;
            if (GUILayout.Button("Show all targets"))
            {
                Scripts.Editor.TargetEditor.Init();
            }

            EditorGUI.BeginChangeCheck();
            /*if (GUILayout.Button("Load old target from level"))
            {
                targetLevel.LoadFromLevel(levelData, targetsEditor);
            }*/

            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
                targetLevel.saveData();
        }
    }
}