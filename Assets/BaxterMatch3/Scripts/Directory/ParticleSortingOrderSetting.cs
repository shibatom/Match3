

using UnityEngine;

namespace Internal.Scripts
{
    public class ParticleSortingOrderSetting : MonoBehaviour
    {
        public int sortingOrder = 3;

        private void Start()
        {
            GetComponent<ParticleSystem>().GetComponent<Renderer>().sortingLayerID = 0;
            GetComponent<ParticleSystem>().GetComponent<Renderer>().sortingOrder = sortingOrder;
        }
    }
}