

using UnityEditor;

namespace Internal.Scripts.System.Orientation.Editor
{
    [CustomEditor(typeof(CameraOrientationHandler))]

    public class CameraHandleEditorGUI : UnityEditor.Editor
    {
        CameraOrientationHandler myTarget;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

    }
}