

using UnityEngine;

namespace Internal.Scripts.Level
{
    /// <summary>
    /// Auto destructor for particles
    /// </summary>
    public class DestroyAfterSeconds : MonoBehaviour
    {
        public float sec;

        private void Start()
        {
            Destroy(gameObject, sec);
        }
    }
}