

using UnityEngine;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Character animations for combos
    /// </summary>
    public class CharacterAnimationSetter : MonoBehaviour
    {
        public Animator anim;

        private void OnEnable()
        {
            anim.SetTrigger("Game");
            MainManager.OnCombo += OnCombo;
        }

        private void OnDisable()
        {
            MainManager.OnCombo -= OnCombo;
        }

        void OnCombo()
        {
            anim.SetTrigger("Cool");
        }
    }
}