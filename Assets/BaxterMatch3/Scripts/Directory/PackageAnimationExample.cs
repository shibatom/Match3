

using UnityEngine;

namespace Internal.Scripts
{
    public class PackageAnimationExample : MonoBehaviour
    {
        public GameObject expl;
        [SerializeField] private int count;

        private void OnEnable()
        {
            for (int i = 0; i < count; i++)
            {
                Instantiate(expl);
            }
        }
    }
}