

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Internal.Scripts.MapScripts
{
	public class TestLevelGUI : MonoBehaviour {
		public int LevelNumber;

		public void OnGUI () {
			GUILayout.BeginVertical ();

			if (GUILayout.Button ("Complete with 1 star")) {
				LevelCampaign.CompleteLevel (LevelNumber, 1);
				GoBack ();
			}

			if (GUILayout.Button ("Complete with 2 star")) {
				LevelCampaign.CompleteLevel (LevelNumber, 2);
				GoBack ();
			}

			if (GUILayout.Button ("Complete with 3 star")) {
				LevelCampaign.CompleteLevel (LevelNumber, 3);
				GoBack ();
			}

			if (GUILayout.Button ("Back")) {
				GoBack ();
			}

			GUILayout.EndVertical ();
		}

		private void GoBack () {
			SceneManager.LoadScene ("demo");
		}
	}
}