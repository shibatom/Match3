

using UnityEditor;
using UnityEngine;

namespace Internal.Scripts.System.Combiner.Editor
{
    [CustomEditor(typeof(ItemCombinations), true)]
    [CanEditMultipleObjects]
    public class ItemCombineGUIEditor : UnityEditor.Editor
    {
        ItemCombinations myTargets;
        private GameObject targetEditorObject;
        Texture2D bonusItem;
        Texture2D item;
        private GameObject prefab;
        int[] array;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            // combine editor coming soon...
            serializedObject.Update();
            myTargets = (ItemCombinations)target;

            EditorGUILayout.LabelField("Combination editor");
            GUILayout.BeginVertical();
            {
                // foreach (ItemCombineBehaviour item in myTargets)
                // {
                if (myTargets.matrix.Count == 0) myTargets.Init();
                else if (myTargets?.matrix[0] == null) myTargets.Init();
                // }
                //        for (int i = 0; i < itemCombineBehaviour.matrix.GetLength(0); i++)
                // {
                //     var matrix = (ItemTemplate[])itemCombineBehaviour.matrix.GetValue(i);
                // ItemCombineBehaviour itemCombineBehaviour = (ItemCombineBehaviour)myTargets[0];
                var martices = myTargets.matrix;
                foreach (var matrix in martices)
                {
                    for (int row = 0; row < ItemCombinations.maxRows; row++)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            for (int col = 0; col < ItemCombinations.maxCols; col++)
                            {
                                if (GUILayout.Button(GetValue(col, row, matrix.items), GUILayout.Height(25), GUILayout.Width(25)))
                                {
                                    ChangeItem(col, row, matrix.items);
                                }
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.Space(10);
                }

            }
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Add matrix"))
                {
                    myTargets.AddItem();
                }

                if (GUILayout.Button("Remove matrix"))
                {
                    myTargets.RemoveItem();
                }
            }
            GUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
        }

        void ChangeItem(int col, int row, TemplateOfItem[] currentMatrix)
        {
            bool isItem = myTargets.GetItemTemplate(col, row, currentMatrix).item;
            var item = myTargets;
            // foreach (ItemCombineBehaviour item in myTargets)
            // {
            item.GetItemTemplate(col, row, currentMatrix).item = !isItem;
            prefab = PrefabUtility.GetOutermostPrefabInstanceRoot(item.gameObject);
            prefab.GetComponent<ItemCombinations>().matrix = item.matrix;
            EditorUtility.SetDirty(prefab);
            // }
            AssetDatabase.SaveAssets();
        }

        GUIContent GetValue(int col, int row, TemplateOfItem[] currentMatrix)
        {
            // ItemCombineBehaviour @object = (ItemCombineBehaviour)myTargets[0];
            var item = myTargets;

            var value = item.GetItemTemplate(col, row, currentMatrix);
            if (!value.item)
                return new GUIContent("", "unused");
            if (value.item)
                return new GUIContent("x", "item");
            return new GUIContent(bonusItem, "bonus item");
        }

    }
}