

using Internal.Scripts.System;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Debug item menu by right mouse button. Works only in Unity editor
    /// </summary>
    public class TestItemChanger : MonoBehaviour
    {
        private Item _item;

        public void ShowMenuItems(Item item)
        {
            _item = item;
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (_item != null)
            {
                if (GUILayout.Button("Select item"))
                {
                    Selection.objects = new Object[] { _item.gameObject };
                }

//            if (GUILayout.Button("Show in console"))
//            {
//                Debug.Log("\nCPAPI:{\"cmd\":\"Search\" \"text\":\"" + item.instanceID + "\"}");
//            }
                if (GUILayout.Button("Select square"))
                {
                    Selection.objects = new Object[] { _item.square.gameObject };
                }

                foreach (var itemType in EnumUtil.GetValues<ItemsTypes>())
                {
                    if (GUILayout.Button(itemType.ToString()))
                    {
                        _item.SetType(itemType, null);
                        _item = null;
                        // item.debugType = itemType;
                    }
                }

                if (GUILayout.Button("Destroy"))
                {
                    _item.DestroyItem(true);
                    //LevelManager.THIS.FindMatches();
                    _item = null;
                }
            }
        }
#endif
    }
}