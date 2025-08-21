using System.Collections;
using BaxterMatch3.Animation.Directory.AnimationUI.Demo;
using Internal.Scripts;
using Internal.Scripts.Level;
using Internal.Scripts.MapScripts.StaticMap.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HelperScripts
{
    public class MenuFailController : MonoBehaviour
    {
        [SerializeField] private GameObject lockedBoosters;
        [SerializeField] private GameObject unlockedBoosters;

        private void Start()
        {
            if (GameManager.CurrentLevel >= 3)
            {
                lockedBoosters.SetActive(false);
                unlockedBoosters.SetActive(true);
            }
            else
            {
                lockedBoosters.SetActive(true);
                unlockedBoosters.SetActive(false);
            }
        }

        public void TryAgain()
        {
            CentralSoundManager.Instance.PlayOneShot(CentralSoundManager.Instance.click);
            GameObject gm = new GameObject();
            gm.AddComponent<HandleLevelRestart>();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }


        public void BackToMap()
        {
            Time.timeScale = 1;
            // LevelManager.THIS.gameStatus = GameState.GameOver;
            // CloseMenu();
            gameObject.SetActive(false);
            MainManager.Instance.gameStatus = GameState.Map;
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