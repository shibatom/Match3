

using UnityEngine;

namespace Internal.Scripts.Items
{
    public interface IChopperTargetable
    {
        GameObject GetChopperTarget { get; set; }
        GameObject GetGameObject { get; }
        Item GetItem { get; }
        int TargetByChopperIndex { get; set; }
    }
}