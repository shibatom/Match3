using System;
using System.Collections.Generic;
using Internal.Scripts.Localization;
using Internal.Scripts.TargetScripts.TargetEditor;
using Malee.List;
using UnityEngine;
using UnityEngine.Serialization;

namespace Internal.Scripts.TargetScripts.TargetSystem
{
    /// <summary>
    /// target container keeps the object should be collected, its count, sprite, color
    /// </summary>
    [Serializable]
    public class TargetContainer
    {
        public string name = "";

        public LocalizationIndexFolderClass localization;
        public CollectingTypes collectAction;
        public SetCount setCount;
        [Reorderable] public SpritesArrays defaultSprites;
        public List<GameObject> prefabs = new List<GameObject>();

        public TargetContainer DeepCopy()
        {
            var other = (TargetContainer)MemberwiseClone();

            return other;
        }

        public string GetDescription()
        {
            return LanguageManager.GetText(localization.description.index, localization.description.text);
        }

        public string GetFailedDescription()
        {
            return LanguageManager.GetText(localization.failed.index, localization.failed.text);
        }
    }

    [Serializable]
    public class LocalizationIndexFolderClass
    {
        [Tooltip("Default text")] public LanguageIndex description;
        public LanguageIndex failed;
    }

    public enum CollectingTypes
    {
        Destroy,
        ReachBottom,
        Spread,
        Clear
    }

    public enum SetCount
    {
        Manually,
        FromLevel
    }

    [Serializable]
    public class SpritesArrays : ReorderableArray<SpritesArray>
    {
    }

    [Serializable]
    public class SpritesArray
    {
        [FormerlySerializedAs("sprites0")] [Reorderable]
        public SpriteListt sprites;

        public SpritesArray Clone()
        {
            return (SpritesArray)MemberwiseClone();
        }
    }
}