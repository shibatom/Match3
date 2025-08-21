

using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace Internal.Scripts.GUI
{
    public class TargetHolder : MonoBehaviour
    {
        public static TargetContainer target;
        public static int level;
        // Use this for initialization
        void Start()
        {
            DontDestroyOnLoad(this);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}