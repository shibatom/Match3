using System.Collections;
using Internal.Scripts;
using Internal.Scripts.Level;
using Internal.Scripts.MapScripts.StaticMap.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HelperScripts
{
    public class MenuCompleteController : MonoBehaviour
    {
        public void Next()
        {
            GlobalValue.CurrentLevel = MainManager.Instance.currentLevel + 1;
            PersistantData.OpenNextLevel = true;
            MainManager.Instance.StartCoroutine(OpenMap());
        }

        private IEnumerator OpenMap()
        {
            Instantiate(Resources.Load("Loading"), transform.parent);
            yield return new WaitForEndOfFrame();
            SceneManager.LoadScene(Resources.Load<MapSwitcher>("Scriptable/MapSwitcher").GetSceneName());
        }
    }
}