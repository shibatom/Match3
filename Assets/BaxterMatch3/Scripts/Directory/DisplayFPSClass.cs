using UnityEngine;

namespace Internal
{
    public class DisplayFPSClass : MonoBehaviour
    {
        private float deltaTime = 0.0f;

        void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        void OnGUI()
        {
            float fps = 1.0f / deltaTime;
            int width = Screen.width, height = Screen.height;
            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(0, 0, width, height * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = height * 2 / 100;
            style.normal.textColor = Color.white;
            string fpsText = Mathf.Ceil(fps).ToString() + " FPS";
            GUI.Label(rect, fpsText, style);
        }
    }
}