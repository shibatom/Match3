

using UnityEngine;
using UnityEngine.Events;

namespace Internal.Scripts.System
{
    public class FireActionOnEnable : MonoBehaviour
    {
        public UnityEvent[] myEvent;
 
        void OnEnable()
        {
            foreach (var unityEvent in myEvent)
            {
                unityEvent.Invoke();
            
            }
        }
    }
}