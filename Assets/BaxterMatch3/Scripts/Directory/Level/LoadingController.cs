

using Internal.Scripts.System;
using UnityEngine;

namespace Internal.Scripts.Level
{
    /// <summary>
    /// Loading level 
    /// </summary>
    public static class LoadingController
    {
        private static LevelData _levelData;

        public static LevelData LoadForPlay(int currentLevel, LevelData levelData)
        {
            levelData = new LevelData(Application.isPlaying, currentLevel);
            levelData = LoadlLevel(currentLevel, levelData).DeepCopyForPlay(currentLevel);
            levelData.LoadTargetObject();
            levelData.InitTargetObjects(true);
            return levelData;
        }

        public static LevelData LoadlLevel(int currentLevel, LevelData levelData)
        {
            levelData = LevelManagerScriptableObject.LoadLevel(currentLevel);
            levelData.CheckLayers();
            // LevelData.THIS = levelData;
            levelData.LoadTargetObject();
            // levelData.InitTargetObjects();

            return levelData;
        }


        public static int GetLastLevelNum()
        {
            return Resources.LoadAll<LevelContainer>("Levels").Length;
        }
    }
}