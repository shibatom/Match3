

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Internal.Scripts.Editor
{
    [InitializeOnLoad]
    public class CustomUpdate
    {
        static CustomUpdate()
        {
            EditorApplication.update += InitProject;
        }

        private static void InitProject()
        {
            EditorApplication.update -= InitProject;
            if (EditorApplication.timeSinceStartup < 10 || !EditorPrefs.GetBool(Application.dataPath + "AlreadyOpened"))
            {
                if (EditorSceneManager.GetActiveScene().name != "GameScene" && Directory.Exists("Assets/BaxterMatch3/Directory/Scenes"))
                {
                    EditorSceneManager.OpenScene("Assets/BaxterMatch3/Directory/Scenes/GameScene.unity");
                }

                EditorPrefs.SetBool(Application.dataPath + "AlreadyOpened", true);
            }
        }
    }
}