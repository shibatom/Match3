

using System;
using UnityEngine;

namespace Internal.Scripts.Integrations.Network
{
    [Serializable]
    public class JsonReader
    {
        public static T[] getJsonArray<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            ArrayWrapper<T> arrayWrapper = null;
            try
            {
                arrayWrapper = JsonUtility.FromJson<ArrayWrapper<T>>(newJson);
            }
            catch
            {
                Debug.Log("Unexpected nod: " + json);
            }

            return arrayWrapper?.array;
        }

        [Serializable]
        private class ArrayWrapper<T>
        {
            public T[] array;
        }
    }
}