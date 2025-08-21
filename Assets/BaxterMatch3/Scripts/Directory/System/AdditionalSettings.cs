

using UnityEngine;

namespace Internal.Scripts.System
{
    // [CreateAssetMenu(fileName = "AdditionalSettings", menuName = "AdditionalSettings", order = 1)]
    public class AdditionalSettings : ScriptableObject
    {
        [Header("Striped should stop on Indestructible")]
        public bool StripedStopByUndestroyable;
        
        [Header("Double multicolor item should destroy Breakables")]
        public bool DoubleMulticolorDestroySolid = true;
        
        [Header("Boost can destroy Breakables directly")]
        public bool SelectableSolidBlock;
        
        [Header("DiscoBall can be destroyed by boost bomb and Chopper")]
        public bool MulticolorDestroyByBoostAndChopper;
    }
}