

using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Internal.Scripts.System;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Proportional scale of icon
    /// </summary>
    [ExecuteInEditMode]
    public class AdjustUIScales : MonoBehaviour
    {
        private float _side = 200;
        public GridLayoutGroup rect;

        private void OnEnable()
        {
            rect = GetComponent<GridLayoutGroup>();
        }

        // Update is called once per frame
        private void Update()
        {
            var count = transform.GetChildren().Where(i => i.gameObject.activeSelf).Count();
            if (count == 4) _side = 150;
            else if (count > 4) _side = 130;
            else if (count == 3) _side = 150;
            else if (count < 3) _side = 280f / count;

            rect.cellSize = Vector2.one * _side;
        }
    }
}