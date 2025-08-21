using Nakama.TinyJson;
using UnityEngine.TextCore.Text;


namespace NakamaOnline
{
    /// <summary>
    /// Used to easily get op code for sending and reading match state messages
    /// </summary>
    public enum MatchMessageType
    {
        Units = 0,
        PlayerStatus = 1,
        Damage = 2,
        SlotChange = 3,
        Merge = 4,
        // UnitSpawned = 0,
        // UnitMoved = 1,
        // UnitAttacked = 2,
        // SpellActivated = 3,
        // StartingHand = 4,
        // CardPlayRequest = 5,
        // CardPlayed = 6,
        // CardCanceled = 7
    }


    /// <summary>
    /// Base class for all match messages
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MatchMessage<T>
    {
        /// <summary>
        /// Parses json gained from server to MatchMessage class object
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T Parse(string json)
        {
            return json.FromJson<T>();
        }

        /// <summary>
        /// Creates string with json to be send as match state message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static string ToJson(T message)
        {
            return message.ToJson();
        }
    }

    public class MatchMessageUnits : MatchMessage<MatchMessageUnits>
    {
        public readonly string PlayerId;
        public readonly string Units;

        public MatchMessageUnits(string playerId, string units)
        {
            PlayerId = playerId;
            Units = units;
        }
    }

    public class MatchMessagePlayerStatus : MatchMessage<MatchMessagePlayerStatus>
    {
        public readonly string PlayerId;
        public readonly int Status;

        public MatchMessagePlayerStatus(string playerId, int status)
        {
            PlayerId = playerId;
            Status = status;
        }
    }

    public class MatchMessageDamage : MatchMessage<MatchMessageDamage>
    {
        public readonly bool IsHostUnit;
        public readonly int SlotNumber;
        public readonly int Damage;

        public MatchMessageDamage(bool isHostUnit, int slotNumber, int damage)
        {
            IsHostUnit = isHostUnit;
            SlotNumber = slotNumber;
            Damage = damage;
        }
    }

    public class MatchMessageChangeHeroSlot : MatchMessage<MatchMessageChangeHeroSlot>
    {
        public readonly string PlayerId;
        public readonly int StartSlot;
        public readonly int EndSlot;

        public MatchMessageChangeHeroSlot(string playerId, int startSlot, int endSlot)
        {
            PlayerId = playerId;
            StartSlot = startSlot;
            EndSlot = endSlot;
        }
    }

    public class MatchMessageMergeHero : MatchMessage<MatchMessageMergeHero>
    {
        public readonly string PlayerId;
        public readonly int StartSlot;
        public readonly int EndSlot;
        public readonly int Level;
        public readonly Character Type;

        public MatchMessageMergeHero(string playerId, int startSlot, int endSlot, int level, Character type)
        {
            PlayerId = playerId;
            StartSlot = startSlot;
            EndSlot = endSlot;
            Level = level;
            Type = type;
        }
    }
}