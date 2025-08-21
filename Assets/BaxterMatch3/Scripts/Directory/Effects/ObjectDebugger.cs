using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Internal.Scripts.Effects
{
    public class ObjectDebugger : MonoBehaviour
    {
        // Start is called before the first frame update
        void OnEnable()
        {
            Debug.Log("Object activated by: " + gameObject.name, gameObject);
        }

        void OnDisable()
        {
            Debug.Log("Object deactivated by: " + gameObject.name, gameObject);
        }
    }
}