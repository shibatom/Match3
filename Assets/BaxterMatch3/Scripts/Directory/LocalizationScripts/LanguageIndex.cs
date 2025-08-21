

using System;
using UnityEngine;

namespace Internal.Scripts.Localization
{
    [Serializable]
    public class LanguageIndex
    {
        [Tooltip("Default text")]
        public string text;
        [Tooltip("Localization line index")]
        public int index;
    }
}