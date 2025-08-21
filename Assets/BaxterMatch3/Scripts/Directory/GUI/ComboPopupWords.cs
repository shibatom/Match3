

using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Congratz words for a combo
    /// </summary>
    public class ComboPopupWords : MonoBehaviour
    {
        [SerializeField] private GameObject[] gratzWords;
        [SerializeField] private GameObject completeWord;
        [SerializeField] private GameObject failedWord;
        [SerializeField] private GameObject noMoreMatches;

        private bool _shouldStopWords;

        private void OnEnable()
        {
            _shouldStopWords = false;
            foreach (var word in gratzWords)
                word.SetActive(false);
            completeWord.SetActive(false);
            failedWord.SetActive(false);
            noMoreMatches.SetActive(false);
        }

        public void ShowGratzWord(GratzWordState word)
        {
            if (_shouldStopWords) return;

            switch (word)
            {
                case GratzWordState.Grats:
                    gratzWords[Random.Range(0, gratzWords.Length)].SetActive(true);
                    break;
                case GratzWordState.LevelCompleted:
                    _shouldStopWords = true;
                    foreach (var word2 in gratzWords)
                        word2.SetActive(false);
                    completeWord.SetActive(true);
                    break;
                case GratzWordState.LevelFailed:
                    _shouldStopWords = true;
                    foreach (var word1 in gratzWords)
                        word1.SetActive(false);
                    failedWord.SetActive(true);
                    break;
                case GratzWordState.NoMoreMatch:
                    noMoreMatches.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(word), word, null);
            }
        }


        private void LateUpdate()
        {
            transform.position = MainManager.Instance.field.GetPosition();
        }
    }


    public enum GratzWordState
    {
        Grats,
        LevelCompleted,
        LevelFailed,
        NoMoreMatch
    }
}