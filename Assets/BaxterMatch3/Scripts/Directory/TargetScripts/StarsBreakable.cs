

using Internal.Scripts;
using Internal.Scripts.Level;
using Internal.Scripts.TargetScripts.TargetSystem;
using UnityEngine;

namespace Internal.Scripts.TargetScripts
{
    /// <summary>
    /// Stars target
    /// </summary>
    public class StarsBreakable : Target
    {
        public override int GetDestinationCountSublevel()
        {
            var count = 0;
            count += LevelData.THIS.star1;
            return count;
        }

        public override int GetDestinationCount()
        {
            var count = 0;
            count += LevelData.THIS.star1;
            return count;
        }
        public override void InitTarget(LevelData levelData)
        {

        }

        public override int CountTarget()
        {
            return GetDestinationCount();
        }

        public override int CountTargetSublevel()
        {
            return GetDestinationCountSublevel();
        }


        public override void FulfillTarget<T>(T[] items)
        {
        }

        public override void DestroyEvent(GameObject obj)
        {
            Debug.Log(obj);
        }

        public override int GetCount(string spriteName)
        {
            return CountTarget();
        }

        public override bool IsTotalTargetReached()
        {
            return MainManager.Score >= MainManager.Instance.levelData.star3;
        }

        public override bool IsTargetReachedSublevel()
        {
            return MainManager.Score >= MainManager.Instance.levelData.star3;
        }
    }
}