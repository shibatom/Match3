using Nakama;
using System;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace NakamaOnline
{
    public class BattleController : MonoBehaviour
    {
        public int _localPlayerStatus;
        public int _otherPlayerStatus;

        private GameStateManager _stateManager;
        private string _localHeroes;
        private string _opponentHeroes;
        private GameConnection _connection;
        private NakamaUi _nakamaUi;

        private void Start()
        {
            _nakamaUi = GetComponent<NakamaUi>();
        }

        public async Task JoinMatch(GameConnection connection)
        {
            NakamaController.OnPhase = OnlinePhase.Joining;
            _connection = connection;
            _stateManager = new GameStateManager(_connection);
            IMatch match = await _connection.Socket.JoinMatchAsync(_connection.BattleConnection.Matched);
            _connection.BattleConnection.MatchId = match.Id;

            NakamaController.OnPhase = OnlinePhase.PreMatch;

            NkBug.Log("Player count is " + match.Presences.Count());
            if (match.Presences.Any())
            {
                NkBug.Log("A player joined before me, i will be the guest");
                string opponentId = match.Presences.First().UserId;
                _connection.BattleConnection.OpponentId = opponentId;
                _connection.BattleConnection.HostId = opponentId;
                SetInitialPlayerState();
            }
            else
            {
                NkBug.Log("No player is here, i will be the host");
                _connection.BattleConnection.HostId = _connection.Session.UserId;
                _connection.Socket.ReceivedMatchPresence += HandleOtherPlayerJoin;
            }

            _stateManager.OnUnits -= UnitsReceived;
            _stateManager.OnUnits += UnitsReceived;

            _stateManager.OnStatus -= StatusReceived;
            _stateManager.OnStatus += StatusReceived;

            _stateManager.OnDamage -= DamageReceived;
            _stateManager.OnDamage += DamageReceived;

            _stateManager.OnSlotChange -= SlotChangeReceived;
            _stateManager.OnSlotChange += SlotChangeReceived;

            _stateManager.OnHeroMerge -= HeroMergeReceived;
            _stateManager.OnHeroMerge += HeroMergeReceived;
        }

        private void SetInitialPlayerState()
        {
            if (IsHost())
            {
                NkBug.Log("Do host stuff");
                NkBug.Log("Send host units to opponent");

            }
            else
            {
                NkBug.LogWarning("Do guest stuff");
            }
        }

        private void HandleOtherPlayerJoin(IMatchPresenceEvent obj)
        {
            bool FindOtherPlayer(IUserPresence join) => join.UserId != _connection.Session.UserId;

            if (!obj.Joins.Any(FindOtherPlayer))
            {
                return;
            }

            string opponentId = obj.Joins.First(FindOtherPlayer).UserId;
            NkBug.Log("Other player joined");
            _connection.BattleConnection.OpponentId = opponentId;
            _connection.Socket.ReceivedMatchPresence -= HandleOtherPlayerJoin;

            SetInitialPlayerState();
        }


        private void UnitsReceived(MatchMessageUnits matchMessageUnits)
        {
            // Dont receive anymore
            _stateManager.OnUnits -= UnitsReceived;
            if (_connection.Session.UserId == matchMessageUnits.PlayerId) return;
            NkBug.Log("Something received from: " + matchMessageUnits.Units);


            StartCoroutine(_nakamaUi.MatchFound());
            _nakamaUi.StartCountdown();
        }

        public void SendHeroSlotChange(int start, int end)
        {
            var message = new MatchMessageChangeHeroSlot(_connection.Session.UserId, start, end);
            _stateManager.SendMatchStateMessage(MatchMessageType.SlotChange, message);
        }

        private void SlotChangeReceived(MatchMessageChangeHeroSlot matchMessageChangeHeroSlot)
        {
            if (_connection.Session.UserId == matchMessageChangeHeroSlot.PlayerId) return;
            NkBug.Log("Hero slot change received: " + matchMessageChangeHeroSlot.StartSlot + " to " + matchMessageChangeHeroSlot.EndSlot);

        }

        public void SendHeroMerge(int start, int end, int level, Character type)
        {
            var message = new MatchMessageMergeHero(_connection.Session.UserId, start, end, level, type);
            _stateManager.SendMatchStateMessage(MatchMessageType.Merge, message);
        }

        private void HeroMergeReceived(MatchMessageMergeHero matchMessageMergeHero)
        {
            if (_connection.Session.UserId == matchMessageMergeHero.PlayerId) return;
            NkBug.Log("Hero merge: " + matchMessageMergeHero.StartSlot + " to " + matchMessageMergeHero.EndSlot);

        }

        public void TogglePlayerStatus()
        {
            _localPlayerStatus = _localPlayerStatus == 0 ? 1 : 0;
            SendStatus(_connection.Session.UserId);
            CheckPlayersStatus();
        }

        private void SendStatus(string userId)
        {
            var message = new MatchMessagePlayerStatus(userId, _localPlayerStatus);
            _stateManager.SendMatchStateMessage(MatchMessageType.PlayerStatus, message);
        }

        private void StatusReceived(MatchMessagePlayerStatus matchMessagePlayerStatus)
        {
            if (_connection.Session.UserId != matchMessagePlayerStatus.PlayerId)
            {
                NkBug.Log("other player status received: " + matchMessagePlayerStatus.Status);
                _otherPlayerStatus = matchMessagePlayerStatus.Status;
                CheckPlayersStatus();
            }
        }

        public void SendDamage(string userId, bool isHostUnit, int slotNumber, int damage)
        {
            var message = new MatchMessageDamage( isHostUnit, slotNumber, damage);
            _stateManager.SendMatchStateMessage(MatchMessageType.Damage, message);
        }

        private void DamageReceived(MatchMessageDamage matchMessageDamage)
        {
        }

        private void CheckPlayersStatus()
        {
            _nakamaUi.ColorStatus(_localPlayerStatus, _otherPlayerStatus);
            if (_localPlayerStatus == 1 && _otherPlayerStatus == 1)
            {
                StartOnlineFight();
            }
        }

        private void StartOnlineFight()
        {
            NakamaController.OnPhase = OnlinePhase.InMatch;
            _nakamaUi.StopCounterDown();
            NkBug.Log("START FIGHT");

            _localPlayerStatus = _otherPlayerStatus = 0;
            _nakamaUi.ActiveStatus(false);
            _nakamaUi.ColorStatus(_localPlayerStatus, _otherPlayerStatus);
        }

        public bool IsHost()
        {
            return _connection.BattleConnection.HostId == _connection.Session.UserId;
        }

        public void LeaveOnFinish()
        {
            _stateManager?.LeaveGame();
            NakamaController.OnPhase = OnlinePhase.MatchFinished;
        }

        public void CountdownFinished()
        {
            _localPlayerStatus = 1;
            SendStatus(_connection.Session.UserId);
            CheckPlayersStatus();
        }
    }
}