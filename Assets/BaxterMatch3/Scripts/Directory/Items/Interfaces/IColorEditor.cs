

using UnityEngine;

namespace Internal.Scripts.Items.Interfaces
{
    public interface IColorEditor
    {
        Sprite[] Sprites { get; }
        Sprite randomEditorSprite { get; }
    }
}