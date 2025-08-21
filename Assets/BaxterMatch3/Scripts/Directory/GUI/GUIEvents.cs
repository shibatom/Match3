

using System.Collections;
using Internal.Scripts.MapScripts.StaticMap.Editor;
using Internal.Scripts.System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// GUI events for Facebook, Settings and main scene
    /// </summary>
    public class GUIEvents : MonoBehaviour
    {
        public GameObject loading;

        private IEnumerator Start()
        {
            if (MainManager.Instance) yield break;
            yield return new WaitForSeconds(1);
            Play();
        }

        private void Update()
        {
            if (name == "FaceBook" || name == "Share" || name == "FaceBookLogout")
            {
                if (!MainManager.Instance.FacebookEnable)
                    gameObject.SetActive(false);
            }
        }

        public void Settings()
        {
            CentralSoundManager.Instance.GetComponent<AudioSource>().PlayOneShot(CentralSoundManager.Instance.click);

            ReferencerUI.Instance.Settings.gameObject.SetActive(true);
        }

        public void Play()
        {
            LeanTween.delayedCall(1, () => SceneManager.LoadScene(Resources.Load<MapSwitcher>("Scriptable/MapSwitcher").GetSceneName()));
            CentralSoundManager.Instance.GetComponent<AudioSource>().PlayOneShot(CentralSoundManager.Instance.click);
        }

        public void Pause()
        {
            CentralSoundManager.Instance.GetComponent<AudioSource>().PlayOneShot(CentralSoundManager.Instance.click);

            if (MainManager.Instance.gameStatus == GameState.Playing)
                GameObject.Find("CanvasGlobal").transform.Find("MenuPause").gameObject.SetActive(true);
        }

        public void FaceBookLogin()
        {
#if FACEBOOK
			FacebookManager.THIS.CallFBLogin ();
#endif
        }

        public void FaceBookLogout()
        {
#if FACEBOOK
			FacebookManager.THIS.CallFBLogout ();

#endif
        }

        public void Share()
        {
#if FACEBOOK
			FacebookManager.THIS.Share ();
#endif
        }
    }
}