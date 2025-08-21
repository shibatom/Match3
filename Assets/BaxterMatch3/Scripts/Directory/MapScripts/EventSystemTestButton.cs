

using System;
using UnityEngine;

namespace Internal.Scripts.MapScripts
{
    public class EventSystemTestButton : MonoBehaviour
    {
        public event EventHandler Click;

        public void OnMouseUpAsButton()
        {
            if (Click != null)
                Click(this, EventArgs.Empty);
        }
    }
}