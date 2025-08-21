

using System.Collections.Generic;
using UnityEngine;

// [CreateAssetMenu(fileName = "Pooler", menuName = "Directory/Add pooler", order = 1)]
namespace Internal.Scripts.System.Pool
{
    public class PoolManagerScriptable : ScriptableObject
    {
        public List<PooledObjectInstance> itemsToPool;

    }
}