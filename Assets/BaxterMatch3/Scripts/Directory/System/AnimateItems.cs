using System;
using System.Collections;
using Internal.Scripts;
using DG.Tweening;
using Internal.Scripts.Items;
using Internal.Scripts.Blocks;
using Internal.Scripts.Items.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Internal.Scripts.System
{
    /// <summary>
    /// Handles animation of blocks and ingredients flying to UI elements
    /// </summary>
    public class AnimateItems : MonoBehaviour
    {
        // Maintain original public interface
        public GameObject linkObject;
        public int linkObjectHash;
        public bool target;

        // Animation configuration
        private const float InitialScaleDuration = 0.4f;
        private const float MoveToMidDuration = 0.4f;
        private const float FloatEffectDuration = 0.2f;
        private const float FinalMoveDuration = 0.4f;
        private const float MidPositionYRangeMin = 1.5f;
        private const float MidPositionYRangeMax = 2f;
        private const float ArcHeight = 0.5f;
        private const float FinalScaleFactor = 0.5f;

        private LevelTargetTypes _currentLevelTargetType;
        private SpriteRenderer _spriteRenderer;
        private Vector3 _startScale;

        public void InitAnimation(GameObject obj, Vector2 pos, Vector2 scale, Action callBack, Sprite sprite = null, LevelTargetTypes levelTargetType = LevelTargetTypes.NONE)
        {
            _currentLevelTargetType = levelTargetType;
            transform.position = obj.transform.position;
            transform.localScale = Vector2.one;

            ConfigureSpriteRenderer(obj, sprite);

            StartCoroutine(AnimateItemRoutine(pos, callBack));

        }

        private void ConfigureSpriteRenderer(GameObject obj, Sprite sprite)
        {
            if (sprite == null) sprite = GetTargetSprite(obj);

            if (_currentLevelTargetType == LevelTargetTypes.Mails)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                _spriteRenderer.transform.localScale = Vector3.zero;
                _spriteRenderer.sprite = sprite;
                _spriteRenderer.sortingLayerName = "UI";
                _spriteRenderer.sortingOrder = 10;
            }
        }

        private Sprite GetTargetSprite(GameObject obj)
        {
            var colorableComponent = obj.GetComponent<ColorReciever>();
            var spriteRenderer = colorableComponent?.directSpriteRenderer ??
                               obj.GetComponent<SpriteRenderer>() ??
                               obj.GetComponentInChildren<SpriteRenderer>();

            return GetSpecialCaseSprite(obj) ?? spriteRenderer?.sprite;
        }

        private Sprite GetSpecialCaseSprite(GameObject obj)
        {
            var subSquare = obj.GetComponentInChildren<SubRectangle>();
            return subSquare != null ? subSquare.BrockenCup : null;
        }

        private IEnumerator AnimateItemRoutine(Vector2 pos, Action callBack)
        {
            callBack?.Invoke();
            var startPos = transform.localPosition;
            var midPos = startPos + new Vector3(0, Random.Range(MidPositionYRangeMin, MidPositionYRangeMax), 0);
            _startScale = Vector3.one;

            if (linkObject != null && linkObject.GetComponent<Rectangle>()?.sizeInSquares.magnitude >= 2)
            {
                transform.localScale = _startScale / 2f;
            }

            yield return HandleSpecialTypeAnimations();
            if (_currentLevelTargetType == LevelTargetTypes.Mails)
            {
                var animationSequence = CreateAnimationSequence(startPos, midPos, pos);
                yield return animationSequence.WaitForCompletion();
            }
             if (_currentLevelTargetType == LevelTargetTypes.PlateCabinet || _currentLevelTargetType == LevelTargetTypes.PotionCabinet)
            {
                
                yield return new WaitForSeconds(1f);
            }


            CleanupAndFinalize(callBack);
        }

        private IEnumerator HandleSpecialTypeAnimations()
        {
            if (_currentLevelTargetType != LevelTargetTypes.PlateCabinet && _currentLevelTargetType != LevelTargetTypes.PotionCabinet ) yield break;

            // var cupAnim = GetComponent<cupAnimation>();
            // cupAnim.lightParticle.Play();

            if (_spriteRenderer != null) _spriteRenderer.enabled = false;

            yield return new WaitForEndOfFrame();
          //  cupAnim.brockenParticle.Play();
        }

  private DG.Tweening.Sequence CreateAnimationSequence(Vector3 startPos, Vector3 midPos, Vector3 targetPos)
{
    var sequence = DOTween.Sequence();

    // Move to mid position & Scale up at the same time
    sequence.Join(transform.DOMove(midPos, MoveToMidDuration)
        .SetEase(DG.Tweening.Ease.OutBack));

    sequence.Join(transform.DOScale(_startScale * 0.8f, MoveToMidDuration)
        .SetEase(DG.Tweening.Ease.OutBack));  // Scaling up while moving

    // Floating effect at the mid position
    sequence.Append(transform.DOMoveY(midPos.y + 0.2f, FloatEffectDuration)
        .SetEase(DG.Tweening.Ease.InOutSine)
        .SetLoops(2, LoopType.Yoyo));

    // Move along a curved path to target position & scale back to Vector3.one
    sequence.Append(transform.DOPath(
        new[]
        {
            midPos,
            Vector3.Lerp(midPos, targetPos, 0.5f) + new Vector3(0, ArcHeight, 0),
            targetPos
        },
        FinalMoveDuration,
        PathType.CatmullRom
    ).SetEase(DG.Tweening.Ease.InOutQuad));

    sequence.Join(transform.DOScale(FinalScaleFactor, FinalMoveDuration) // Scale down while moving
        .SetEase(DG.Tweening.Ease.InOutQuad));

    return sequence;
}
        private void CleanupAndFinalize(Action callBack)
        {
            
            MainManager.Instance.animateItems.Remove(this);
            Destroy(gameObject);
        }
    }
}