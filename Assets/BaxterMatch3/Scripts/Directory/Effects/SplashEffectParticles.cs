using UnityEngine;

namespace Internal.Scripts.Effects
{
    /// <summary>
    /// Simple item explosion effect
    /// </summary>
    [ExecuteInEditMode]
    public class SplashEffectParticles : MonoBehaviour
    {
        float index;
        ParticleSystem ps;
        public GameObject attached;

        public void SetColor(int index_)
        {
            ps = GetComponent<ParticleSystem>();
            index = index_ + 1;
            ps.Play();
        }

        public void mySetColor(int color)
        {
            ApplyColorToParticleSystems(transform, color);
        }

        private void ApplyColorToParticleSystems(Transform currentTransform, int color)
        {
            var particleSystem = currentTransform.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                main.startColor = color switch
                {
                    4 => new ParticleSystem.MinMaxGradient(new Color32(0x1D, 0x67, 0xD3, 255)) // Blue
                    ,
                    5 => new ParticleSystem.MinMaxGradient(new Color32(0xE3, 0x17, 0x9D, 255)) // Pink/Magenta
                    ,
                    3 => new ParticleSystem.MinMaxGradient(new Color32(0x1D, 0xC4, 0x10, 255)) // Green
                    ,
                    1 => new ParticleSystem.MinMaxGradient(new Color32(0xFF, 0x00, 0x00, 255)) // Red
                    ,
                    _ => main.startColor
                };
            }

            int childIndex = 0;
            foreach (Transform child in currentTransform)
            {
                if (childIndex != 2)
                {
                    ApplyColorToParticleSystems(child, color);
                }

                childIndex++;
            }
        }

        void OnEnable()
        {
            is_alive = false;
        }

        private bool is_alive = false;

        public void RandomizeParticleSeed()
        {
            RandomizeSeedRecursive(transform);
        }

        private void RandomizeSeedRecursive(Transform currentTransform)
        {
            ParticleSystem particleSystem = currentTransform.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                bool wasPlaying = particleSystem.isPlaying;


                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);


                particleSystem.randomSeed = (uint)Random.Range(0, int.MaxValue);
                transform.GetChild(0).GetComponent<ParticleSystem>().randomSeed = particleSystem.randomSeed;

                if (wasPlaying)
                {
                    particleSystem.Play();
                }
            }
        }
    }
}