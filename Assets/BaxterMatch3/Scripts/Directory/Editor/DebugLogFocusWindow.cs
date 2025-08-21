

using UnityEditor;

namespace Internal.Scripts.Editor
{
    public class DebugLogFocusWindow : EditorWindow
    {
        private static DebugLogFocusWindow _window;

        /*public static void ShowWindow()
        {
            GetWindow(typeof(DebugLogKeeperWindow));
        }*/

        public void OnFocus()
        {
            _window = (DebugLogFocusWindow)GetWindow(typeof(DebugLogFocusWindow));
            _window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            {
            }
            EditorGUILayout.EndVertical();
        }
    }
}