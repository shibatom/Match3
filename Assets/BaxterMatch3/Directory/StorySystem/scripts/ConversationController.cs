using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using CodeMonkey.Utils;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using Internal.Scripts.Level;
using System.Collections.Generic;
using System;

namespace StorySystem
{
    [System.Serializable]
    public class QuestionEvent : UnityEvent<Question>
    {
    }

    public class ConversationController : MonoBehaviour
    {
        // Singleton instance
        public static ConversationController Instance;

        private TextMeshProUGUI messageText;
        private Text_Writer.Text_WriterSingle text_WriterSingle;
        private AudioSource talkingAudioSource;


        private Conversation conversation;
        public Conversation defaultConversation;
        public QuestionEvent questionEvent;
        public GameObject background;

        public DialogueScroller _dialogueScroller;

        public GameObject speakerLeft;
        public GameObject speakerRight;
        public GameObject narratorCenter;

        private SpeakerUIController speakerUILeft;
        private SpeakerUIController speakerUIRight;
        private SpeakerUIController narratorUICenter;

        public float cooldownDuration = 0.01f;
        public bool isButtonOnCooldown { get; set; } = false;

        private int characterIndex;
        private float timePerCharacter;
        private float timer;

        public event EventHandler OnLineStarts;
        bool hasNextLineStarted = false;
        string nowIsSpeacking = "";
        bool isCharNextSpeacker = false;

        private List<Sprite> spriteList = new List<Sprite>();


        private int activeLineIndex;
        private bool conversationStarted = false;


        public Conversation myconverstation
        {
            get { return conversation; }
            set { conversation = value; }
        }

        public void ChangeConversation(Conversation nextConversation)
        {
            conversationStarted = false;
            conversation = nextConversation;
            AdvanceLine();
        }

        private void Awake()
        {
            Instance = this;

            transform.Find("SkipButton").GetComponent<Button_UI>().ClickFunc = () => { EndConversation(); };
            speakerUILeft = speakerLeft.GetComponent<SpeakerUIController>();
            speakerUIRight = speakerRight.GetComponent<SpeakerUIController>();
            narratorUICenter = narratorCenter.GetComponent<SpeakerUIController>();
            OnLineStarts += lineLstiner;

            transform.Find("tapAnyWhereButton").GetComponent<Button_UI>().ClickFunc = () =>
            {
                if (!isButtonOnCooldown) // Check if the button is not on cooldown
                {
                    AdvanceLine();
                    if (!isButtonOnCooldown)
                    {
                        isButtonOnCooldown = true; // Set the cooldown flag
                        StartCoroutine(CooldownTimer());
                    }
                }
            };
        }

        private void lineLstiner(object sender, EventArgs e)
        {
            hasNextLineStarted = true;
        }

        private void Start()
        {
        }

        private IEnumerator CooldownTimer()
        {
            yield return new WaitForSeconds(cooldownDuration);


            isButtonOnCooldown = false;
        }

        public void EndConversation()
        {
            StoryManager.Instance.StoryStop();
            isButtonOnCooldown = false;

            conversation = defaultConversation;
            conversationStarted = false;
            _dialogueScroller.DestroyAllChildren();

            speakerUILeft.Hide();
            speakerUIRight.Hide();
            narratorUICenter.Hide();
        }

        private void Initialize()
        {
            conversationStarted = true;
            activeLineIndex = 0;


            speakerUILeft.Speaker = conversation.speakerLeft;
            speakerUIRight.Speaker = conversation.speakerRight;
            narratorUICenter.Speaker = conversation.narratorCenter;
        }

        public void SetLevelStoryConversation(LevelData selectedLevelData, Conversation levelConversation)
        {
            /*gameObject.SetActive(true);*/


            myconverstation = levelConversation;
        }

        public void AdvanceLine()
        {
            transform.Find("tapAnyWhereButton").gameObject.SetActive(true);
            if (conversation == null) return;
            if (!conversationStarted)
            {
                Initialize();
            }

            if (activeLineIndex < conversation.lines.Length)
            {
                if (activeLineIndex == conversation.lines.Length - 1 && conversation.nextConversation == null)
                {
                    transform.Find("tapAnyWhereButton").gameObject.SetActive(false);
                }

                DisplayLine();
            }

            else
                AdvanceConversation();
        }


        private void DisplayLine()
        {
            Line line = conversation.lines[activeLineIndex];

            nowIsSpeacking = line.character.name;


            if (activeLineIndex < conversation.lines.Length - 1)
            {
                Line nextline = conversation.lines[activeLineIndex + 1];

                if (nowIsSpeacking == nextline.character.name)
                {
                    isCharNextSpeacker = true;
                }

                else
                {
                    isCharNextSpeacker = false;
                }
            }

            else
            {
                isCharNextSpeacker = false;
            }

            Character character = line.character;

            Transform characterDefaultPos = gameObject.transform;

            switch (line.action)
            {
                case ActionTodo.showPetRescue:
                    StoryManager.Instance.ShowRescuePet(true);
                    break;

                case ActionTodo.showCharacterInMap:
                    StoryManager.Instance.ShowRescuePet(false);
                    StoryManager.Instance.ShowMapInStory();
                    break;

                case ActionTodo.showMissionToPass:
                    StoryManager.Instance.ShowStoryTutorial();
                    break;

                case ActionTodo.none:

                    break;
            }

            switch (line.backGroundAction)
            {
                case BackGroundAction.maskBackground:
                    StoryManager.Instance.fadeInvertedMask();
                    break;
            }

            if (speakerUILeft.SpeakerIs(character))
            {
                _dialogueScroller.transform.DOLocalMoveX(240, 0.3f);
                SetDialog(speakerUILeft, speakerUIRight, narratorUICenter, line);
            }

            if (speakerUIRight.SpeakerIs(character))
            {
                _dialogueScroller.transform.DOLocalMoveX(-200, 0.3f);
                SetDialog(speakerUIRight, speakerUILeft, narratorUICenter, line);
            }

            if (narratorUICenter.SpeakerIs(character))
            {
                SetDialog(narratorUICenter, speakerUILeft, speakerUIRight, line);
            }

            activeLineIndex += 1;
            hasNextLineStarted = false;
        }

        private void AdvanceConversation()
        {
            if (conversation.question != null)
                questionEvent.Invoke(conversation.question);
            else if (conversation.nextConversation != null)
            {
                ChangeConversation(conversation.nextConversation);
            }
            else

                EndConversation();
        }

        void SetDialog(
            SpeakerUIController activeSpeakerUI, SpeakerUIController inactiveSpeakerUI, SpeakerUIController inactiveSpeakerUI2, Line line)
        {
            _dialogueScroller.InstansiateLineHolder(activeSpeakerUI);
            if (activeSpeakerUI == narratorUICenter)
            {
                activeSpeakerUI.Show();
                inactiveSpeakerUI.FullHide();
                inactiveSpeakerUI2.FullHide();
            }
            else
            {
                activeSpeakerUI.Show();
                inactiveSpeakerUI.Hide();
                inactiveSpeakerUI2.Hide();
            }

            activeSpeakerUI.Dialog = "";
            activeSpeakerUI.Mood = line.mood;
            activeSpeakerUI.BackgroundStory = line.backgroundStory;
            Sprite backgroudSprite = activeSpeakerUI.BackgroundStory;

            if (backgroudSprite != null)
                StoryManager.Instance.ShowStoryBackground(backgroudSprite);

            // StopAllCoroutines();

            EffectTypewriter(line.text, activeSpeakerUI);


            /* if (!isCharNextSpeacker)
             {
                 activeSpeakerUI.MoveBack();
             }*/

            hasNextLineStarted = false;
        }

        private void EffectTypewriter(string text, SpeakerUIController controller)
        {
            //messageText = transform.Find("Text(TMP)").GetComponent<TextMeshProUGUI>();
            messageText = controller.dialog.gameObject.GetComponent<TextMeshProUGUI>();


            string message = text;
            if (text_WriterSingle != null && text_WriterSingle.IsActive())
            {
                // Currently active TextWriter
                text_WriterSingle.WriteAllAndDestroy();
            }
            else
            {
                string[] messageArray = new string[]
                {
                    "This is the assistant speaking, hello and goodbye, see you next time!",
                    "Hey there!",
                    "This is a really cool and useful effect",
                    "Let's learn some code and make awesome games!",
                    "Check out Battle Royale Tycoon on Steam!",
                };

                // string message = messageArray[Random.Range(0, messageArray.Length)];
                StartTalkingSound();
                //text_WriterSingle = Text_Writer.AddWriter_static(messageText, message, .01f, true, false);
            }

            text_WriterSingle = Text_Writer.AddWriter_static(messageText, message, .01f, true, true, HideSpeckerUI);

            void HideSpeckerUI()
            {
            }
        }


        private void ShowStoryBackground(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }


            Image imageComponent = background.transform.Find("Jungle").gameObject.GetComponent<Image>();
            imageComponent.DOCrossfadeImage(sprite, 1f);

            Image imageComponent2 = background.transform.Find("Jungle[1]").gameObject.GetComponent<Image>();
        }


        // Returns true on complete

        private void StartTalkingSound()
        {
            //talkingAudioSource.Play();
        }

        private void StopTalkingSound()
        {
            //   talkingAudioSource.Stop();
        }
    }
}