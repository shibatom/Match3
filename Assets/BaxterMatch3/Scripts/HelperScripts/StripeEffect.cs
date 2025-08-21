using Spine.Unity;
using System.Collections.Generic;
using Internal.Scripts.Blocks;
using UnityEngine;
using Internal.Scripts.Items;
using Internal.Scripts;


namespace HelperScripts
{
    public class StripeEffect : MonoBehaviour
    {
        [SerializeField] SkeletonMecanim skeletonMecanim;

        [SpineBone(dataField: "skeletonMecanim")] [SerializeField]
        public string boneNameMechanim;

        [SerializeField] SkeletonAnimation skeletonAnimation;

        [SpineBone(dataField: "skeletonAnimation")] [SerializeField]
        public string boneName;

        [SerializeField] Spine.Bone bone;
        [SerializeField] float maxDistance = .4f;
        [SerializeField] LayerMask squareDetectionLayerMask;
        [SerializeField] LayerMask itemDetectionLayerMask;
        [SerializeField] bool doesNeedToStartDestroy = false;
        [SerializeField] bool isStrip = false;

        public bool startDestroy = true;

        private readonly HashSet<Rectangle> _processedSquares = new HashSet<Rectangle>();
        private readonly HashSet<Item> _processedItems = new HashSet<Item>();

        private Rectangle _tempRec;
        private Item _tempItem;

        private void Update()
        {
            if (bone != null && startDestroy)
            {
                Vector3 boneWorldPosition = bone.GetWorldPosition(transform);
                CheckForRec(boneWorldPosition);
                CheckForItem(boneWorldPosition);
            }
        }

        private void CheckForItem(Vector3 boneWorldPosition)
        {
            Collider2D itemOverlapHit = Physics2D.OverlapPoint(boneWorldPosition, itemDetectionLayerMask);
            if (itemOverlapHit != null)
            {
                if (itemOverlapHit.transform.IsChildOf(transform) == false)
                {
                    if (itemOverlapHit.TryGetComponent(out _tempItem))
                    {
                        if (_processedItems.Contains(_tempItem) == false)
                        {
                            _processedItems.Add(_tempItem);
                            _tempItem.DestroyItem(true, true, destroyNeighbours: false);
                        }
                    }
                }
            }
        }

        private void CheckForRec(Vector3 boneWorldPosition)
        {
            Collider2D squareOverlapHit = Physics2D.OverlapPoint(boneWorldPosition, squareDetectionLayerMask);
            if (squareOverlapHit != null)
            {
                if (squareOverlapHit.transform.IsChildOf(transform) == false)
                {
                    if (squareOverlapHit.TryGetComponent(out _tempRec))
                    {
                        if (_tempRec.type is LevelTargetTypes.Eggs or LevelTargetTypes.Pots
                            or LevelTargetTypes.BreakableBox or
                            LevelTargetTypes.PlateCabinet or LevelTargetTypes.PotionCabinet
                            or LevelTargetTypes.HoneyBlock)
                        {
                            if (_processedSquares.Contains(_tempRec) == false)
                            {
                                _processedSquares.Add(_tempRec);
                                _tempRec.DestroyBlock(destroyNeighbour: false);
                            }
                        }
                    }
                }
            }
        }

        private void Awake()
        {
            if (skeletonAnimation != null && !string.IsNullOrEmpty(boneName))
            {
                if (skeletonAnimation.Skeleton != null)
                {
                    bone = skeletonAnimation.Skeleton.FindBone(boneName);
                }
            }
            else if (skeletonMecanim != null && !string.IsNullOrEmpty(boneNameMechanim))
            {
                if (skeletonMecanim.Skeleton != null)
                {
                    bone = skeletonMecanim.Skeleton.FindBone(boneNameMechanim);
                }
            }
        }

        private void OnEnable()
        {
            if (!isStrip )
                gameObject.AddComponent<In_GameBlocker>();
            _processedSquares.Clear();
            _processedItems.Clear();
            //StartCoroutine(SetGameBlockerWithDelay());
        }

        /*private IEnumerator SetGameBlockerWithDelay()
        {
            yield return null;
            if (!isStrip || transform.parent.localRotation.eulerAngles.z != 0)
                gameObject.AddComponent<In_GameBlocker>();
        }*/

        private void OnDisable()
        {
            if (isStrip)
            {
                MainManager.Instance.EndBusyOperation();
            }

            if (doesNeedToStartDestroy)
            {
                startDestroy = false; // Stop the destruction process when disabled
            }
        }

        public void StopDestroy()
        {
            startDestroy = false;
        }
    }
}