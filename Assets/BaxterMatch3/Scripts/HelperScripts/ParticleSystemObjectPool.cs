using Internal.Scripts.System.Pool;
using UnityEngine;

namespace HelperScripts
{
    public class ParticleSystemObjectPool : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] ps;

        // When the object is activated, clear and start the particle system.
        void OnEnable()
        {
            if (ps != null)
            {
                foreach (ParticleSystem particleSystem in ps)
                {
                    particleSystem.Clear();
                    particleSystem.Play();
                }
            }
        }

        // This is called when the particle system stops (ensure Stop Action is set to Callback)
        void OnParticleSystemStopped()
        {
            // Return this object to the pool via the pool manager.
            ObjectPoolManager.Instance.PutBack(gameObject);
        }
    }
}