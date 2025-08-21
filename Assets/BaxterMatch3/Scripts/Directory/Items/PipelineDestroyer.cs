using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Destroy processing. Delayed destroying items from array
    /// </summary>
    public class PipelineDestroyer : MonoBehaviour
    {
        public static PipelineDestroyer Instance;
        public List<BunchDestroyer> pipeline = new List<BunchDestroyer>();

        private void Start()
        {
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
            {
                Debug.Log("Destroying duplicate DestroyingPipeline instance.");
                Destroy(gameObject);
            }

            Debug.Log("Starting DestroyingPipeline Coroutine.");
            StartCoroutine(myDestroyingPipelineCor());
        }

        public void DestroyItems(List<Item> items, Delays delays, Action callback)
        {
            if (MainManager.Instance.DebugSettings.DestroyLog)
            {
                foreach (var item in items)
                {
                    Debug.Log($"Add to pipeline: {item.name} at position {item.transform.position}");
                }
            }

            Debug.Log($"DestroyItems called with {items.Count} items, delays: {delays}, callback: {callback}");
            var bunch = new BunchDestroyer();
            bunch.items = items.ToList();
            bunch.callback = callback;
            bunch.delays = delays;
            pipeline.Add(bunch);

            Debug.Log($"Pipeline now has {pipeline.Count} bunches.");
        }

        private IEnumerator myDestroyingPipelineCor()
        {
            while (true)
            {
                for (var i = 0; i < pipeline.Count; i++)
                {
                    var bunch = pipeline[i];

                    Debug.Log($"Processing bunch {i + 1}/{pipeline.Count} with {bunch.items.Count} items.");

                    if (bunch.items.Any())
                    {
                        var squares = bunch.items
                            .Where(itm => itm != null && itm.square != null)
                            .Select(x => x.square.GetSubSquare())
                            .ToArray();

                        Debug.Log($"Checking squares for {squares.Length} items.");
                        //  LevelManager.THIS.levelData.GetTargetObject().CheckSquares(squares);
                    }

                    if (bunch.delays.before != null)
                    {
                        Debug.Log("Waiting for delay before.");
                        yield return bunch.delays.before;
                    }

                    // Collect items into a list and call DestroyGroup
                    List<Item> itemsToDestroy = new List<Item>();

                    for (var j = 0; j < bunch.items.Count; j++)
                    {
                        var item = bunch.items[j];
                        if (bunch.delays.beforeevery != null)
                        {
                            Debug.Log("Waiting for delay before every item.");
                            yield return Activator.CreateInstance(bunch.delays.beforeevery.GetType());
                        }

                        if (item != null)
                        {
                            Debug.Log($"Adding item {item.name} to destroy group.");
                            item.combinationID = bunch.GetHashCode();
                            itemsToDestroy.Add(item); // Collect item into the list
                        }

                        if (bunch.delays.afterevery != null)
                        {
                            Debug.Log("Waiting for delay after every item.");
                            yield return Activator.CreateInstance(bunch.delays.afterevery.GetType());
                        }
                    }

                    // Pass the collected items to DestroyGroup
                    if (itemsToDestroy.Count > 0)
                    {
                        Debug.Log($"Destroying {itemsToDestroy.Count} items.");
                        DestroyGroup destroyGroup = new DestroyGroup(itemsToDestroy);
                        yield return destroyGroup.DestroyGroupCor(showScore: false, useParticles: false, useExplosionEffect: false);
                    }

                    if (bunch.delays.after != null)
                    {
                        Debug.Log("Waiting for delay after.");
                        yield return bunch.delays.after;
                    }

                    pipeline.Remove(bunch);
                    Debug.Log($"Removed bunch. Pipeline now has {pipeline.Count} bunches.");

                    bunch.callback?.Invoke();
                    Debug.Log("Callback invoked.");
                }

                yield return new WaitForFixedUpdate();
            }
        }


        private IEnumerator DestroyingPipelineCor()
        {
            while (true)
            {
                for (var i = 0; i < pipeline.Count; i++)
                {
                    var bunch = pipeline[i];
                    if (bunch.items.Any())
                        MainManager.Instance.levelData.GetTargetObject().CheckSquares(bunch.items.Where(i => i != null && i.square != null).Select(x => x.square.GetSubSquare()).ToArray());

                    if (bunch.delays.before != null)
                        yield return bunch.delays.before;
                    for (var j = 0; j < bunch.items.Count; j++)
                    {
                        var item = bunch.items[j];
                        if (bunch.delays.beforeevery != null)
                            yield return Activator.CreateInstance(bunch.delays.beforeevery.GetType());
                        if (item != null)
                        {
                            item.combinationID = bunch.GetHashCode();
                            item.DestroyItem(true);
                        }

                        if (bunch.delays.afterevery != null)
                        {
                            yield return Activator.CreateInstance(bunch.delays.afterevery.GetType());
                        }
                    }

                    if (bunch.delays.after != null)
                        yield return bunch.delays.after;
                    pipeline.Remove(bunch);
                    if (bunch.callback != null)
                        bunch.callback();
                }

                yield return new WaitForFixedUpdate();
            }
        }
    }


    public class BunchDestroyer
    {
        public List<Item> items;
        public Action callback;
        public Delays delays;
    }

    public struct Delays
    {
        public CustomYieldInstruction before, beforeevery, afterevery, after;
    }
}