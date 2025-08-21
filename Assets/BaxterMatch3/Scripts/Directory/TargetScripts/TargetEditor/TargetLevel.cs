using System;
using System.Linq;
using Internal.Scripts.Level;
using Internal.Scripts.TargetScripts.TargetSystem;
using Malee.List;
using UnityEditor;
using UnityEngine;

namespace Internal.Scripts.TargetScripts.TargetEditor
{
    [CreateAssetMenu(fileName = "TargetLevel", menuName = "TargetLevel", order = 1)]
    public class TargetLevel : ScriptableObject
    {
        [Reorderable(null, "Target", null)] public TargetList targets;
        private TargetEditorScriptable targetsEditor;

        private void OnEnable()
        {
            var t = Resources.Load("Levels/Targets/" + this.name);
        }

#if UNITY_EDITOR
        public void LoadFromLevel(LevelData levelData, TargetEditorScriptable _targetsEditor, bool checkCount = true)
        {
            targetsEditor = _targetsEditor;
            SpriteListt sprites = new SpriteListt();
            this.targets.Clear();

            for (var index = 0; index < levelData.subTargetsContainers.Count; index++)
            {
                var container = levelData.subTargetsContainers[index];
                if (container.count > 0 || !checkCount)
                {
                    Sprite spr = null;
                    if (container.extraObject != null) spr = (Sprite)container.extraObject;
                    var targetContainer = levelData.GetTargetEditor();
                    var sprites1 = SpriteList(targetContainer, spr);
                    ResetSprites(levelData, targetContainer, sprites1, index, container.count);
                }
            }

            sprites = new SpriteListt();
            sprites.Add(new SpriteObjectt { icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BaxterMatch3/Directory/Textures/Win Scene/Star_02.png") });
            this.targets.Add(CreateTarget(levelData.GetTargetByNameEditor("Stars"), sprites, 1, levelData));
            saveData();
        }

        private void ResetSprites(LevelData levelData, TargetContainer targetContainer, SpriteListt sprites1, int index, int containerCount)
        {
            if (levelData.target.name == "Ingredients")
            {
                if (index == 0)
                    targets.Add(CreateTarget(targetContainer, sprites1, containerCount, levelData));
                else
                {
                    sprites1 = new SpriteListt();
                    sprites1.Add(new SpriteObjectt { icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BaxterMatch3/Directory/Textures/Items/ingredient_02.png") });
                    targets.Add(CreateTarget(targetContainer, sprites1, containerCount, levelData));
                }
            }
            else if (targetContainer.name != "Stars")
            {
                targets.Insert(0, CreateTarget(targetContainer, sprites1, containerCount, levelData));
            }
        }

        private SpriteListt SpriteList(TargetContainer type, Sprite spr, bool GroupTarget = false)
        {
            SpriteListt sprites;
            var targetContainer = targetsEditor.targets.First(i => i.name == type.name);
            if (spr != null && targetContainer.defaultSprites.Any(i => i.sprites.Any(x => x.icon.name == spr.name)))
            {
                sprites = targetContainer.defaultSprites.First(i => i
                    .sprites.Any(x => x.icon.name == spr.name)).sprites.Copy();
            }
            else sprites = targetContainer.defaultSprites[0].sprites.Copy();

            return sprites;
        }

        TargetObject CreateTarget(TargetContainer type, SpriteListt sprites, int count, LevelData levelData)
        {
            var targetObject = new TargetObject();
            targetObject.sprites = new SpriteListt();
            targetObject.targetType = new TargetType();
            Debug.LogError(" GetTargetIndex  " + type.name);
            targetObject.targetType.type = levelData.GetTargetIndex(type.name);
            targetObject.sprites = sprites;
            targetObject.CountDrawer = new CountClass();
            targetObject.CountDrawer.count = count;
            return targetObject;
        }

        public void saveData()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public void ChangeTarget(TargetContainer targetContainer, int newselectedTarget, LevelData levelData, TargetEditorScriptable targetEditorScriptable)
        {
            var targetDelete = targets.Where(i => i.targetType.GetTarget().name != "Stars").ToList();
            foreach (var targetObject in targetDelete) targets.Remove(targetObject);
            var newTarget = targetEditorScriptable.targets[newselectedTarget];
            var list = targetEditorScriptable.targets.Where(i => i.name == newTarget.name).SelectMany(i => i.defaultSprites).OrderByDescending(i => i.sprites[0].icon.name);
            foreach (var sprArray in list)
            {
                ResetSprites(levelData, newTarget, sprArray.sprites, 0, 0);
            }

            saveData();
        }
#endif
    }

    [Serializable]
    public class TargetList : ReorderableArray<TargetObject>
    {
    }

    [Serializable]
    public class SpriteListt : ReorderableArray<SpriteObjectt>
    {
        public SpriteListt Copy()
        {
            SpriteListt listt = new SpriteListt();
            foreach (var item in this)
            {
                listt.Add(item.Copy());
            }

            return listt;
        }
    }

    [Serializable]
    public class SpriteObjectt
    {
        public Sprite icon;
        public bool uiSprite;

        public SpriteObjectt Copy()
        {
            return (SpriteObjectt)MemberwiseClone();
        }
    }

    [Serializable]
    public class TargetObject
    {
        public TargetType targetType;
        [Reorderable] public SpriteListt sprites;
        public CountClass CountDrawer;

        [Tooltip("Not finish the level even the target is complete until move/time is out")]
        public bool NotFinishUntilMoveOut;

        public BooleanScoreShow ShowTheScoreForStar;
    }

    [Serializable]
    public class BooleanScoreShow
    {
        public bool ShowTheScore;
    }

    [Serializable]
    public class CountClass
    {
        public int count;
    }

    [Serializable]
    public class TargetType
    {
        public int type;
        private TargetEditorScriptable _targetEditorScriptable;

        public TargetEditorScriptable EditorScriptable
        {
            get
            {
                if (_targetEditorScriptable == null) _targetEditorScriptable = Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
                return _targetEditorScriptable;
            }
        }

        public TargetContainer GetTarget()
        {
            //Debug.LogError("GetTarget " + EditorScriptable.targets.Count +  "  type "+type);
            return EditorScriptable.targets[type];
        }
    }
}