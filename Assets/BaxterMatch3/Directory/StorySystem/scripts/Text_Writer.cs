using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace StorySystem
{
    public class Text_Writer : MonoBehaviour
    {
        private static Text_Writer instance;

        private List<Text_WriterSingle> text_WriterSingleList;

        private void Awake()
        {
            instance = this;
            text_WriterSingleList = new List<Text_WriterSingle>();
        }

        public static Text_WriterSingle AddWriter_static(TextMeshProUGUI uiText, string textToWrite, float timePerCharacter
            , bool invisibleCharacter, bool removeWriterBeforeAdd, Action onComplete)
        {
            if (removeWriterBeforeAdd)
            {
                instance.RemoveWriter(uiText);
            }

            return instance.AddWriter(uiText, textToWrite, timePerCharacter, invisibleCharacter, onComplete);
        }

        private Text_WriterSingle AddWriter(TextMeshProUGUI uiText, string textToWrite, float timePerCharacter
            , bool invisibleCharacter, Action onComplete)
        {
            Text_WriterSingle text_WriterSingle = new Text_WriterSingle(uiText, textToWrite, timePerCharacter, invisibleCharacter, onComplete);

            text_WriterSingleList.Add(text_WriterSingle);
            return text_WriterSingle;
        }

        public static void RemoveWriter_Static(TextMeshProUGUI uiText)
        {
            instance.RemoveWriter(uiText);
        }

        private void RemoveWriter(TextMeshProUGUI uiText)
        {
            for (int i = 0; i < text_WriterSingleList.Count; i++)
            {
                if (text_WriterSingleList[i].GetUIText() == uiText)
                {
                    text_WriterSingleList.RemoveAt(i);
                    i--;
                }
            }
        }

        private void Update()
        {
            for (int i = 0; i < text_WriterSingleList.Count; i++)
            {
                bool destroyInstance = text_WriterSingleList[i].Update();
                if (destroyInstance)
                {
                    text_WriterSingleList.RemoveAt(i);
                    i--;
                }
            }
        }

        internal static Text_WriterSingle AddWriter_static(TextMeshProUGUI messageText, string message, float v1, bool v2, bool v3, object v4)
        {
            throw new NotImplementedException();
        }

        public class Text_WriterSingle
        {
            private TextMeshProUGUI uiText;
            private string textToWrite;
            private int characterIndex;
            private float timePerCharacter;
            private float timer;
            private bool invisibleCharacter;
            private Action onComplete;

            public Text_WriterSingle(TextMeshProUGUI uiText, string textToWrite, float timePerCharacter
                , bool invisibleCharacter, Action onComplete)
            {
                this.uiText = uiText;
                this.textToWrite = textToWrite;
                this.timePerCharacter = timePerCharacter;
                this.characterIndex = 0;
                this.invisibleCharacter = invisibleCharacter;
                this.onComplete = onComplete;
            }

            public bool Update()
            {
                if (this.uiText != null)
                {
                    timer -= Time.deltaTime;
                    while (timer < 0)
                    {
                        timer += timePerCharacter;
                        characterIndex++;
                        string text = textToWrite.Substring(0, characterIndex);
                        if (invisibleCharacter)
                        {
                            text += "<color=#00000000>" + textToWrite.Substring(characterIndex) + "</color>";
                        }

                        uiText.text = text;
                        if (characterIndex >= textToWrite.Length)
                        {
                            if (onComplete != null) onComplete();
                            return true;
                        }
                    }
                }

                return false;
            }

            public TextMeshProUGUI GetUIText()
            {
                return uiText;
            }

            public bool IsActive()
            {
                return characterIndex < textToWrite.Length;
            }

            public void WriteAllAndDestroy()
            {
                uiText.text = textToWrite;
                characterIndex = textToWrite.Length;
                if (onComplete != null) onComplete();
                Text_Writer.RemoveWriter_Static(uiText);
            }
        }
    }
}