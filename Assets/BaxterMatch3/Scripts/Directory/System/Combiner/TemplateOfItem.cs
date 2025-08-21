

using System;
using Internal.Scripts.Items;
using UnityEngine;

namespace Internal.Scripts.System.Combiner
{
    /// <summary>
    /// item for combine matrix
    /// </summary>
    [Serializable]
    public class TemplateOfItem
    {
        public Vector2 position;
        public bool item;

        public float angleToNode, distanceToNode;
        // public int color;

        public Item itemRef;

        public TemplateOfItem(Vector2 vector, bool _item/* , int c, Item _item */)
        {
            position = vector;
            item = _item;
            // color = c;
            // item = _item;
        }

        public TemplateOfItem DeepCopy()
        {
            var other = (TemplateOfItem)MemberwiseClone();
            return other;
        }

    }
}