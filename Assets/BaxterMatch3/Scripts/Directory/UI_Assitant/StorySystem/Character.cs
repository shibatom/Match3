using UnityEngine;

namespace StorySystem
{
    [CreateAssetMenu(fileName = "New Character", menuName = "Character")]
    public class Character : ScriptableObject
    {
        public string fullName;
        public Sprite portrait;
        public Sprite portraitAngry;
        public Sprite portraitSad;
        public Sprite portraitWorried;
        public Sprite portraitTerrified;
    }
}