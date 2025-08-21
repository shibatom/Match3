

using Internal.Scripts.Blocks;
using UnityEngine;

namespace Internal.Scripts
{
    public interface ISquareItemCommon
    {
        bool IsBottom();
        Sprite GetSprite();
        SpriteRenderer GetSpriteRenderer();
        SpriteRenderer[] GetSpriteRenderers();

        LevelTargetTypes GetType();
    }
}