using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace NakamaOnline
{
    public class NakamaBanUser : MonoBehaviour
    {
        private float countdownTime = 30f; // 10-second countdown timer for quit the game
        public Text accountId;

        private void OnEnable()
        {
            string id = "acc" + PlayerPrefs.GetString("AccountIdNakama");
            accountId.text = id;
            // Start the countdown to quit the application when the script is activated
            StartCoroutine(StartQuitCountdown());
        }

        private IEnumerator StartQuitCountdown()
        {
            yield return new WaitForSeconds(countdownTime);
            Application.Quit();
        }
    }
}