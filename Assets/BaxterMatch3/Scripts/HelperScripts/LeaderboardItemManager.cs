using UnityEngine;
using UnityEngine.UI;

namespace HelperScripts
{
    public class LeaderboardItemManager : MonoBehaviour
    {
        public Image itemBG;
        public Sprite sprite1;
        public Sprite sprite2;
        public Sprite sprite3;
        public Image medalImage;
        public Sprite medal1;
        public Sprite medal2;
        public Sprite medal3;
        public Text rankText;
        public Text username;
        public Text score, subScore;

        private static int counter = 0;

        public void SetItem(int spriteNumber, int rank, string name, string score, string subscore)
        {
            if (counter is 0 or 1 or 2 or 3 or 4 or 10 or 20 or 50)
                Debug.LogError("SetItem  counter " + counter + "  spriteNumber  " + spriteNumber + " rank " + rank + " name " + name + "  score " + score + " subscore  " + subscore);
            counter++;
            switch (spriteNumber)
            {
                case 1:
                    itemBG.sprite = sprite1;
                    break;
                case 2:
                    itemBG.sprite = sprite2;
                    break;
                case 3:
                    itemBG.sprite = sprite3;
                    break;
            }

            if (rank > 0 && rank < 4)
            {
                medalImage.enabled = true;
                switch (rank)
                {
                    case 1:
                        medalImage.sprite = medal1;
                        break;
                    case 2:
                        medalImage.sprite = medal2;
                        break;
                    case 3:
                        medalImage.sprite = medal3;
                        break;
                }
            }
            else
            {
                medalImage.enabled = false;
                rankText.text = $"{rank}";
            }

            username.text = name;
            this.score.text = score;
            this.subScore.text = subscore;
        }
    }
}