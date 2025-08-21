

using UnityEditor;
using UnityEngine;
using Internal.Scripts.Items.Interfaces;

namespace Internal.Scripts.Level.ItemsPerLevel.Editor
{
    public class ItemsInsideEditor : EditorWindow
    {
        private static GameObject _prefab;
        private static int _numLevel;
        private static ParticleSystem _ps;

        // Add menu item named "My Window" to the Window menu
        public static void ShowWindow(GameObject itemPrefab, int level)
        {
            _ps = Resources.Load<ParticleSystem>("Prefabs/Particles/FireworkSplash");
            ItemsInsideEditor window = (ItemsInsideEditor)EditorWindow.GetWindow(typeof(ItemsInsideEditor), true, itemPrefab.name + " editor");

            _prefab = Resources.Load<GameObject>("Items/" + itemPrefab.name);
            _numLevel = level;
            //Show existing window instance. If one doesn't exist, make one.
            GetWindow(typeof(ItemsInsideEditor));
        }

        private void OnGUI()
        {
            if (_prefab)
            {
                GUILayout.BeginVertical();
                {
                    var sprs = _prefab.GetComponent<ColorReciever>().GetSpritesOrAdd(_numLevel);
                    for (var index = 0; index < sprs.Length; index++)
                    {
                        var spr = sprs[index];
                        sprs[index] = (Sprite)EditorGUILayout.ObjectField(spr, typeof(Sprite), true, GUILayout.Width(50), GUILayout.Height(50));
                        if (sprs[index] != spr)
                        {
                            PrefabUtility.SavePrefabAsset(_prefab);
                            _ps.textureSheetAnimation.SetSprite(index, sprs[index]);
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            else Close();
        }
    }
}