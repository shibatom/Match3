

using System;
using System.Collections.Generic;
using System.Linq;
using Internal.Scripts;
using Internal.Scripts.Items;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Internal.Scripts.System.Pool
{
    /// <summary>
    /// object pool. Uses to keep and activate items and effects
    /// </summary>
    [Serializable]
    public class PooledObjectInstance
    {
        public GameObject objectToPool;
        public string poolName;
        public int amountToPool;
        public bool shouldExpand = true;
        public bool inEditor = true;
    }

    [ExecuteInEditMode]
    public class ObjectPoolManager : MonoBehaviour
    {
        public const string DefaultRootObjectPoolName = "Pooled Objects";

        public static ObjectPoolManager Instance;
        public string rootPoolName = DefaultRootObjectPoolName;
        public List<PoolNameHolder> pooledObjects = new List<PoolNameHolder>();
        private List<PooledObjectInstance> itemsToPool;
        private PoolManagerScriptable PoolSettings;


        void OnEnable()
        {
            LoadFromScriptable();
            Instance = this;
        }

        private void LoadFromScriptable()
        {
            PoolSettings = Resources.Load("Scriptable/PoolSettings") as PoolManagerScriptable;
            itemsToPool = PoolSettings.itemsToPool;
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            ClearNullElements();

            foreach (var item in itemsToPool)
            {
                if (item == null) continue;
                if (item.objectToPool == null) continue;
                var pooledCount = pooledObjects.Count(i => i.name == item.objectToPool.name);
                for (int i = 0; i < item.amountToPool - pooledCount; i++)
                {
                    CreatePooledObject(item);
                }
            }
        }

        private void ClearNullElements()
        {
            pooledObjects.RemoveAll(i => i == null);
        }

        private GameObject GetParentPoolObject(string objectPoolName)
        {
            // Use the root object pool name if no name was specified
            // if (string.IsNullOrEmpty(objectPoolName))
            //     objectPoolName = rootPoolName;

            // if (GameObject.Find(rootPoolName) == null) new GameObject { name = rootPoolName };
            GameObject parentObject = GameObject.Find(objectPoolName);
            // Create the parent object if necessary
            if (parentObject == null)
            {
                parentObject = new GameObject();
                parentObject.name = objectPoolName;

                // Add sub pools to the root object pool if necessary
                if (objectPoolName != rootPoolName)
                    parentObject.transform.parent = transform;
            }

            return parentObject;
        }

        public void HideObjects(string tag)
        {
            // Debug.Log("hide");
            var objects = GameObject.FindObjectsOfType<PoolNameHolder>().Where(i => i.name == tag);
            foreach (var item in objects)
                item.gameObject.SetActive(false);
        }

        public void PutBack(GameObject obj)
        {
            if (MainManager.Instance.DebugSettings.FallingLog) DebugLogManager.Log(obj + " pooled", DebugLogManager.LogType.Falling);
            obj.SetActive(false);
//        Destroy(obj);
            Item item = obj.GetComponent<Item>();
            if (item != null)
            {
                if (item.transform.childCount > 0)
                    item.transform.GetChild(0).localScale = Vector3.one;
            }
        }

        public GameObject GetPooledObject(string tag, Object activatedBy = null, bool active = true, bool canBeActive = false)
        {
            ClearNullElements();

            PoolNameHolder obj = null;
            Item item = null;

            foreach (var t in pooledObjects)
            {
                if (t == null) continue;

                if ((!t.gameObject.activeSelf || canBeActive) && t.name == tag)
                {
                    item = t.GetComponent<Item>();

                    if (item && item.canBePooled)
                    {
                        obj = t;
                        break;
                    }
                    else if (!item)
                    {
                        obj = t;
                        break;
                    }
                }
            }

            if (obj == null)
            {
                if (itemsToPool == null) LoadFromScriptable();

                foreach (var itemToPool in itemsToPool)
                {
                    if (itemToPool != null && itemToPool.objectToPool == null) continue;

                    if (itemToPool.objectToPool.name == tag)
                    {
                        if (itemToPool.shouldExpand)
                        {
                            obj = CreatePooledObject(itemToPool);
                            break;
                        }
                    }
                }
            }

            if (MainManager.Instance.DebugSettings.FallingLog) DebugLogManager.Log(obj + " unpooled by " + activatedBy, DebugLogManager.LogType.Falling);

            if (obj != null)
            {
                obj.gameObject.SetActive(active);
                return obj.gameObject;
            }

            Debug.LogWarning($"Failed to pool object with tag: {tag}. Object not found in pool or cannot be expanded.");
            return null;
        }


        private PoolNameHolder CreatePooledObject(PooledObjectInstance item)
        {
            // if (!Application.isPlaying && !item.inEditor)
            // {
            //     Debug.Log("not play not editor - " + item.objectToPool);
            //     return null;
            // }
            GameObject obj = Instantiate(item.objectToPool);
            // Get the parent for this pooled object and assign the new object to it
            var parentPoolObject = GetParentPoolObject(item.poolName);
            obj.transform.parent = parentPoolObject.transform;
            var poolBehaviour = obj.AddComponent<PoolNameHolder>();
            poolBehaviour.name = item.objectToPool.name;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                //obj = PrefabUtility.ConnectGameObjectToPrefab(obj, item.objectToPool);
                PrefabUtility.RevertPrefabInstance(obj, InteractionMode.AutomatedAction);
            }
#endif

            obj.SetActive(false);
            pooledObjects.Add(poolBehaviour);


            return poolBehaviour;
        }

        public void DestroyObjects(string tag)
        {
            for (int i = 0; i < pooledObjects.Count; i++)
            {
                if (pooledObjects[i].name == tag)
                {
                    DestroyImmediate(pooledObjects[i]);
                }
            }

            ClearNullElements();
        }
    }
}