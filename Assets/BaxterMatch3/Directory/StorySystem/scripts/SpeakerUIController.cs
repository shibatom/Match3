using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace StorySystem
{
    public class SpeakerUIController : MonoBehaviour
    {
        public Image portrait;
        public TextMeshProUGUI fullName;
        public TextMeshProUGUI dialog;
        public Mood mood;
        public Sprite backgroundStory;
        private Vector3 characterDefaultPos;
        private Character speaker;
        private Sprite _sprite;

        public GameObject lineHolderPrefab;

        public Character Speaker


        {
            get { return speaker; }
            set
            {
                speaker = value;
                // portrait.sprite = speaker.portrait;
                fullName.text = speaker.fullName;
            }
        }

        private void OnEnable()
        {
            //HideAllChildObjects();
        }

        private void HideAllChildObjects()
        {
            // Iterate through all child objects
            foreach (Transform child in transform)
            {
                // Set the child object's active state to false
                child.gameObject.SetActive(false);
            }
        }

        public string Dialog
        {
            get { return dialog.text; }
            set { dialog.text = value; }
        }

        public TextMeshProUGUI DialogHolder
        {
            get { return dialog; }
            set { dialog = value; }
        }

        public Mood Mood
        {
            set
            {
                portrait.sprite = value switch
                {
                    Mood.Neutral => speaker.portrait,
                    Mood.Angry => speaker.portraitAngry,
                    Mood.Sad => speaker.portraitSad,
                    Mood.Worried => speaker.portraitWorried,
                    Mood.Terrified => speaker.portraitTerrified,
                    _ => speaker.portrait
                };
            }
        }

        public Sprite BackgroundStory
        {
            get { return _sprite; }
            set
            {
                if (_sprite != value)
                {
                    _sprite = value;
                    OnSpriteChanged(_sprite);
                }
            }
        }

        private void OnSpriteChanged(Sprite sprite)
        {
        }


        public bool HasSpeaker()
        {
            return speaker != null;
        }

        public bool SpeakerIs(Character character)
        {
            return speaker == character;
        }

        public void Show()
        {
            gameObject.SetActive(true);

            ConversationController.Instance.isButtonOnCooldown = true;
            // Perform the local move on the X-axis
            var moveTween = gameObject.transform.DOLocalMoveX(0, 0.3f);
            gameObject.transform.GetChild(0).GetComponent<Image>().DOFade(1f, 0.3f);

            // Set up a callback for when the movement is complete
            moveTween.OnComplete(OnMoveComplete);
        }

        // This method will be called when the movement is complete
        private void OnMoveComplete()
        {
            // Your code to execute after the movement is complete
            ConversationController.Instance.isButtonOnCooldown = false;
        }


        public void Hide()
        {
            if (speaker.name == "millo")
            {
                characterDefaultPos.x = -140f;
            }

            if (speaker.name == "banner")
            {
                characterDefaultPos.x = 190f;
            }

            if (speaker.name == "narrator")
            {
                characterDefaultPos.x = -1686f;
            }

            // Start the animation and provide a callback function
            gameObject.transform.DOLocalMoveX(characterDefaultPos.x, 0.25f, true).OnComplete(() =>
            {
                // This code will be executed when the animation is complete
                gameObject.transform.GetChild(0).GetComponent<Image>().DOFade(0, 0.1f);
            });
        }

        public void FullHide()
        {
            if (speaker.name == "millo")
            {
                characterDefaultPos.x = -900f;
            }

            if (speaker.name == "banner")
            {
                characterDefaultPos.x = 900f;
            }

            if (speaker.name == "narrator")
            {
                characterDefaultPos.x = -1686f;
            }

            // Start the animation and provide a callback function
            gameObject.transform.DOLocalMoveX(characterDefaultPos.x, 0.25f, true).OnComplete(() =>
            {
                // This code will be executed when the animation is complete
                Debug.LogError("FullHide  ");
                gameObject.transform.GetChild(0).GetComponent<Image>().DOFade(0, 0.1f);
            });
        }
    }
}