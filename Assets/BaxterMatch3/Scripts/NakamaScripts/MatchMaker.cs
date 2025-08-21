using System;
using System.Collections.Generic;
using Nakama;
using UnityEngine;

namespace NakamaOnline
{
    public class MatchMaker : MonoBehaviour
    {
        /// <summary>
        /// Mathmaker ticker used to leave queue or join match.
        /// </summary>
        private IMatchmakerTicket _ticket;
        private GameConnection _connection;
        private NakamaController _nakamaController;
        private BattleController _battleController;
        private NakamaUi _nakamaUi;
        
        // Outside namespace dependencies
        //private SaveAndLoadManager _saveAndLoadManager;

        public void Init(GameConnection connection)
        {
            _connection = connection;
            _nakamaController ??= GetComponent<NakamaController>();
            _battleController ??= GetComponent<BattleController>();
            //_saveAndLoadManager ??= FindObjectOfType<SaveAndLoadManager>();
            _nakamaUi ??= GetComponent<NakamaUi>();
        }

        /// <summary>
        /// Joins matchmaker queue and shows this panel.
        /// </summary>
        public async void StartMatchmaking()
        {
            await _nakamaController.SocketConnectionCheck();
            //_saveAndLoadManager.SaveGame();
            NkBug.Log("Start matchmaking");

            _connection.Socket.ReceivedMatchmakerMatched += OnMatchmakerMatched;
            // Join the matchmaker
            try
            {
                NakamaController.OnPhase = OnlinePhase.MatchMaking;
                // Example for matchmaking conditions
                // var query = "+properties.region:europe +properties.rank:>=5 +properties.rank:<=10";
                var serverQuery = "+properties.rank:>=5";

                // Example for my matchmaking properties
                var stringProperties = new Dictionary<string, string>
                {
                    { "engine", "unity" },
                    { "region", "europe" }
                };
                var serverNumericProperties = new Dictionary<string, double>()
                {
                    { "rank", 5 }
                };
                // Acquires matchmaking ticket used to join a match
                _ticket = await _connection.Socket.AddMatchmakerAsync(
                    query: serverQuery,
                    minCount: 2,
                    maxCount: 2,
                    stringProperties: null,
                    numericProperties: serverNumericProperties);
            }
            catch (Exception e)
            {
                NkBug.LogWarning("An error has occured while joining the matchmaker: " + e);
                NakamaController.OnPhase = OnlinePhase.None;
                _nakamaUi.CancelMatchButton();
            }
        }

        /// <summary>
        /// Leaves matchmaker queue and hides this panel.
        /// </summary>
        public async void CancelMatchmaking()
        {
            try
            {
                await _connection.Socket.RemoveMatchmakerAsync(_ticket);
                _nakamaUi.CancelMatchButton();
                NkBug.Log("Removed match maker async");
                NakamaController.OnPhase = OnlinePhase.None;
            }
            catch (Exception e)
            {
                NkBug.LogWarning("An error has occured while removing from matchmaker: " + e);
            }

            _connection.Socket.ReceivedMatchmakerMatched -= OnMatchmakerMatched;
            _ticket = null;
        }

        /// <summary>
        /// Invoked whenever matchmaker finds an opponent.
        /// </summary>
        private void OnMatchmakerMatched(IMatchmakerMatched matched)
        {
            _connection.BattleConnection = new BattleConnection(matched);
            _connection.Socket.ReceivedMatchmakerMatched -= OnMatchmakerMatched;

            NkBug.Log("Matchmaker matched called");
            
            _battleController.JoinMatch(_connection);
        }
    }
}