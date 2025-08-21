using System;
using Nakama;
using System.Linq;

namespace NakamaOnline
{
    public class GameStateManager
    {
        public event Action<MatchMessageUnits> OnUnits;
        public event Action<MatchMessagePlayerStatus> OnStatus;
        public event Action<MatchMessageDamage> OnDamage;
        public event Action<MatchMessageChangeHeroSlot> OnSlotChange;
        public event Action<MatchMessageMergeHero> OnHeroMerge;

        /// <summary>
        /// Indicates if player is already leaving match
        /// </summary>
        private bool _isLeaving;

        private GameConnection _connection;

        public GameStateManager(GameConnection connection)
        {
            _connection = connection;

            // Listen to incomming match messages and user connection changes
            _connection.Socket.ReceivedMatchPresence += OnMatchPresence;
            _connection.Socket.ReceivedMatchState += ReceiveMatchStateMessage;
            _connection.Socket.Closed += OnSocketDisconnect;
        }

        /// <summary>
        /// Starts procedure of leaving match by local player
        /// </summary>
        public async void LeaveGame()
        {
            if (_isLeaving)
            {
                return;
            }

            _isLeaving = true;

            _connection.Socket.ReceivedMatchPresence -= OnMatchPresence;
            _connection.Socket.ReceivedMatchState -= ReceiveMatchStateMessage;
            _connection.Socket.Closed -= OnSocketDisconnect;

            try
            {
                //Sending request to Nakama server for leaving match
                await _connection.Socket.LeaveMatchAsync(_connection.BattleConnection.MatchId);
            }
            catch (Exception e)
            {
                NkBug.LogWarning("Error leaving match: " + e.Message);
            }

            _connection.BattleConnection = null;
        }

        /// <summary>
        /// This method sends match state message to other players through Nakama server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="opCode"></param>
        /// <param name="message"></param>
        public void SendMatchStateMessage<T>(MatchMessageType opCode, T message)
            where T : MatchMessage<T>
        {
            try
            {
                //Packing MatchMessage object to json
                string json = MatchMessage<T>.ToJson(message);

                //Sending match state json along with opCode needed for unpacking message to server.
                //Then server sends it to other players
                _connection.Socket.SendMatchStateAsync(_connection.BattleConnection.MatchId, (long)opCode, json);
            }
            catch (Exception e)
            {
                NkBug.LogError("Error while sending match state: " + e.Message);
            }
        }

        /// <summary>
        /// This method is used by host to invoke locally event connected with match message which is sent to other players.
        /// Should be always runned on host client after sending any message, otherwise some of the game logic would not be runned on host game instance.
        /// Don't use this method when client is not a host!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="opCode"></param>
        /// <param name="message"></param>
        public void SendMatchStateMessageSelf<T>(MatchMessageType opCode, T message)
            where T : MatchMessage<T>
        {
            //Choosing which event should be invoked basing on opCode and firing event
            switch (opCode)
            {
                //UNITS
                // case MatchMessageType.UnitSpawned:
                //     OnUnitSpawned?.Invoke(message as MatchMessageUnitSpawned);
                //     break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Reads match messages sent by other players, and fires locally events basing on opCode.
        /// </summary>
        /// <param name="opCode"></param>
        /// <param name="messageJson"></param>
        public void ReceiveMatchStateHandle(long opCode, string messageJson)
        {
            //Choosing which event should be invoked basing on opCode, then parsing json to MatchMessage class and firing event
            switch ((MatchMessageType)opCode)
            {
                case MatchMessageType.Units:
                    var matchMessageUnits = MatchMessageUnits.Parse(messageJson);
                    OnUnits?.Invoke(matchMessageUnits);
                    break;
                case MatchMessageType.PlayerStatus:
                    var matchMessagePlayerStatus = MatchMessagePlayerStatus.Parse(messageJson);
                    OnStatus?.Invoke(matchMessagePlayerStatus);
                    break;
                case MatchMessageType.Damage:
                    var matchMessageDamage = MatchMessageDamage.Parse(messageJson);
                    OnDamage?.Invoke(matchMessageDamage);
                    break;
                case MatchMessageType.SlotChange:
                    var matchMessageChangeHeroSlot = MatchMessageChangeHeroSlot.Parse(messageJson);
                    OnSlotChange?.Invoke(matchMessageChangeHeroSlot);
                    break;
                case MatchMessageType.Merge:
                    var matchMessageMergeHero = MatchMessageMergeHero.Parse(messageJson);
                    OnHeroMerge?.Invoke(matchMessageMergeHero);
                    break;
            }
        }

        /// <summary>
        /// Method fired when any user leaves or joins the match
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMatchPresence(IMatchPresenceEvent e)
        {
            if (e.Leaves.Any())
            {
                NkBug.LogWarning($"OnMatchPresence() User(s) left the game");
                // if other user leaves in match, set online flags to false and calculate game base on client
                if (NakamaController.OnPhase == OnlinePhase.PreMatch) NakamaUi.LeaveOngoingMatch();
                
                LeaveGame();
                NakamaController.OnPhase = OnlinePhase.SomebodyLeftInMatch;
            }
        }

        private void OnSocketDisconnect()
        {
            NkBug.LogWarning($"Socket disconnected from server");
            NakamaUi.LeaveOngoingMatch();
            LeaveGame();

            NakamaController.OnPhase = OnlinePhase.DisconnectedFromMatch;
        }

        /// <summary>
        /// Decodes match state message json from byte form of matchState.State and then sends it to ReceiveMatchStateHandle
        /// for further reading and handling
        /// </summary>
        /// <param name="matchState"></param>
        private void ReceiveMatchStateMessage(IMatchState matchState)
        {
            string messageJson = System.Text.Encoding.UTF8.GetString(matchState.State);

            if (string.IsNullOrEmpty(messageJson))
            {
                return;
            }

            ReceiveMatchStateHandle(matchState.OpCode, messageJson);
        }
    }
}