using UnityEngine;

namespace StorySystem
{
    public enum Mood
    {
        Neutral,
        Angry,
        Sad,
        Worried,
        Terrified
    }

    public enum ActionTodo
    {
        none,
        showPetRescue,
        showCharacterInMap,
        showMissionToPass,
    }

    public enum BackGroundAction
    {
        none,
        maskBackground,
        FadeStoryToMap,
        showBackGroundStory,
    }


    [System.Serializable]
    public struct Line
    {
        public Character character;

        [TextArea(2, 5)] public string text;
        public Mood mood;
        public Sprite backgroundStory;
        public ActionTodo action;
        public BackGroundAction backGroundAction;
    }

    [CreateAssetMenu(fileName = "New Conversation", menuName = "Conversation")]
    public class Conversation : ScriptableObject
    {
        public Character speakerLeft;
        public Character speakerRight;
        public Character narratorCenter;
        public Line[] lines;
        public Question question;
        public Conversation nextConversation;
        public GameObject objectToShow;
    }
}