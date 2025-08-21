

using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Internal.Scripts.Items
{
    [Serializable]
    public class CreateTestAsset : PlayableAsset
    {
        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            return Playable.Create(graph);
        }
    }
}