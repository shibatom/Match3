

using UnityEditor;

namespace Internal.Scripts.System.Combiner.Editor
{
    public class FillingWindowMatrices : EditorWindow
    {
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            FillingWindowMatrices window = (FillingWindowMatrices)GetWindow(typeof(FillingWindowMatrices));
            window.Show();
        }
    }
}