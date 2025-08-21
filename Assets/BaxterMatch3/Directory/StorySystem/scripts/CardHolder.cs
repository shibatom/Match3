
using UnityEngine;

namespace StorySystem
{
    public class CardHolder : MonoBehaviour
    {
        public CardConfig[] cards;
        public GameObject cardWindow;


        void Start()
        {
            /*  var selectedLevel = PlayerPrefs.GetInt("OpenLevel");
              var card = cards[0];

              if (StoryManager.Instance.HasLEvelContainsCard(selectedLevel))
              {
                  Debug.Log("sallog level has card" + selectedLevel);
                  //cardWindow.SetActive(true);
                  //cardWindow.GetComponent<CardMenu>().SetWheelReward(card);
              }
              */
        }
    }
}