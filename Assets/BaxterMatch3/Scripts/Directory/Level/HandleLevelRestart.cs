

using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Internal.Scripts.GUI;

namespace Internal.Scripts.Level
{
    /// <summary>
    /// restart level helper
    /// </summary>
    public class HandleLevelRestart : MonoBehaviour
    {
        private void Awake()
        {
            MainManager.Instance.Limit = MainManager.Instance.levelData.limit;
            DontDestroyOnLoad(this);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(WaitForLoad(scene));
        }

        private IEnumerator WaitForLoad(Scene scene)
        {
            yield return new WaitUntil(() => MainManager.Instance != null);
            if (scene.name == "GameScene")
            {
                Debug.Log("restart");

                GUIUtilities.Instance.StartGame();
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            Debug.Log("OnDisable");
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}