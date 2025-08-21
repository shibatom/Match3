
using BaxterMatch3.Animation.Directory.AnimationUI.Demo;
using TMPro;
using UnityEngine;

namespace HelperScripts
{
    public class ChangeNamePopup : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        //private SettingsMenu _settingsMenu;

        /*public void SetPopup(SettingsMenu settingsMenu)
        {
            _settingsMenu = settingsMenu;
        }*/

        public void ChangeUserName()
        {
            if (string.IsNullOrWhiteSpace(inputField.text) /*|| inputField.text.Length < 4*/)
                return;
            GameManager.Username = inputField.text;
            Close();
        }

        public void Close()
        {
            Destroy(gameObject);
        }
    }
}