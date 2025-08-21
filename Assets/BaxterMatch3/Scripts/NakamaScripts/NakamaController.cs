using Nakama;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using BaxterMatch3.Animation.Directory.AnimationUI.Demo;


namespace NakamaOnline
{
    public class NkBug
    {
        private static bool enable = true;

        public static void Log(string log)
        {
            if (!enable) return;
            Debug.Log($"<b>NK- <color=#00ff00ff>{log}</color></b>");
        }

        public static void LogWarning(string log)
        {
            if (!enable) return;
            Debug.Log($"<b>NK- <color=#ffa500ff>{log}</color></b>");
        }

        public static void LogError(string log)
        {
            if (!enable) return;
            Debug.Log($"<b>NK- <color=#ff0000ff>{log}</color></b>");
        }
    }

    public enum OnlinePhase
    {
        None,
        MatchMaking,
        Joining,
        PreMatch,
        InMatch,
        SomebodyLeftInMatch,
        DisconnectedFromMatch,
        MatchFinished
    }

    public class NakamaController : MonoBehaviour
    {
        #region Properties

        public static NakamaController ins;
        public static OnlinePhase OnPhase = OnlinePhase.None;
        [SerializeField] private GameConnection _connection;
        [SerializeField] private ServerInfo _serverInfo;
        private IApiLeaderboardRecord cachedOurResult;
        private IApiLeaderboardRecordList cachedListResult;
        private bool cachedLeaderboard;
        private DateTime refreshedTime;
        private int timeout = 10000;
        private MatchMaker _matchMaker;
        public GameObject BanPopup;

        #endregion

        #region Init

        private void Awake()
        {
            if (ins == null)
            {
                ins = this;
            }
            else if (ins != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            Application.runInBackground = true;
#endif
            // DontDestroyOnLoad(this);
            ConnectDevice();

            _matchMaker = GetComponent<MatchMaker>();
        }

        #endregion

        #region Connection & Configuration

        private async void ConnectDevice()
        {
            if (string.IsNullOrEmpty(_serverInfo.serverKey)) return;
            Debug.Log("ConnectDevice  ");
            // _loadingMenu.Show(true);

            string deviceId = GetDeviceId();

            if (!string.IsNullOrEmpty(deviceId))
            {
                PlayerPrefs.SetString(GameConstants.DeviceIdKey, deviceId);
            }

            NkBug.Log("Device Id: " + deviceId);
            await Initialize(deviceId);
            Debug.Log("ConnectDevice  end");
        }

        public async Task<bool> SocketConnectionCheck(bool init = true)
        {
            Debug.LogError("SocketConnectionCheck 1  ");
            // check socket is initialized
            if (_connection.Socket == null)
            {
                ConnectDevice();
                return false;
            }

            Debug.LogError("SocketConnectionCheck 2  ");
            // every time socket disconnects get a new one
            if (!_connection.Socket.IsConnected && init)
            {
                var connected = await Initialize(GetDeviceId());
                if (!connected) return false;
            }

            Debug.LogError("SocketConnectionCheck 3  ");
            return _connection.Socket.IsConnected;
        }

        private async Task<bool> Initialize(string deviceId)
        {
            Debug.Log("Nakama controller INIT  ");
            var retryConfiguration = new RetryConfiguration(_serverInfo.clientTimeOut * 1000, 1);
            var client = new Client(_serverInfo.scheme, _serverInfo.host, _serverInfo.port, _serverInfo.serverKey,
                UnityWebRequestAdapter.Instance)
            {
                Timeout = _serverInfo.clientTimeOut
            };

            var socket = client.NewSocket(useMainThread: true);

            string authToken = PlayerPrefs.GetString(GameConstants.AuthTokenKey, null);
            bool isAuthToken = !string.IsNullOrEmpty(authToken);

            string refreshToken = PlayerPrefs.GetString(GameConstants.RefreshTokenKey, null);

            ISession session = null;

            // refresh token can be null/empty for initial migration of client to using refresh tokens.
            if (isAuthToken)
            {
                session = Session.Restore(authToken, refreshToken);

                // Check whether a session is close to expiry.
                if (session.HasExpired(DateTime.UtcNow.AddDays(1)))
                {
                    try
                    {
                        // get a new access token
                        session = await client.SessionRefreshAsync(session, retryConfiguration: retryConfiguration);
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            session = await client.AuthenticateDeviceAsync(deviceId,
                                retryConfiguration: retryConfiguration);
                        }
                        catch (ApiResponseException exception)
                        {
                            // Catch and handle ban users
                            if (exception.StatusCode == 403)
                            {
                                // Call function to quit game
                                PlayerPrefs.SetString("AccountIdNakama", session.UserId);
                                TriggerBannedAction(session, client);
                            }
                            else
                            {
                                Debug.Log("An error occurred: " + e.Message);
                            }

                            return false;
                        }

                        // get a new refresh token
                        PlayerPrefs.SetString(GameConstants.RefreshTokenKey, session.RefreshToken);
                    }

                    PlayerPrefs.SetString(GameConstants.AuthTokenKey, session.AuthToken);
                }
            }
            else
            {
                try
                {
                    session = await client.AuthenticateDeviceAsync(deviceId);
                    PlayerPrefs.SetString(GameConstants.AuthTokenKey, session.AuthToken);
                    PlayerPrefs.SetString(GameConstants.RefreshTokenKey, session.RefreshToken);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    return false;
                }
            }

            try
            {
                await socket.ConnectAsync(session);
                NkBug.Log("Socket connected");
                NkBug.Log("Username: " + session.Username);
                _matchMaker.Init(_connection);
            }
            catch (Exception e)
            {
                NkBug.LogWarning("Error connecting socket: " + e.Message);
                return false;
            }

            IApiAccount account = null;

            try
            {
                account = await client.GetAccountAsync(session);
            }
            catch (ApiResponseException e)
            {
                NkBug.LogError("Error getting user account: " + e.Message);
                return false;
            }

            _connection.Init(client, socket, account, session);
            if (session.UserId != null) PlayerPrefs.SetString("AccountIdNakama", session.UserId);
            return true;
            // FindObjectOfType<NakamaManager>().Init(client, session);
        }

        private string GetDeviceId()
        {
            string deviceId = "";

            deviceId = PlayerPrefs.GetString(GameConstants.DeviceIdKey);

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                // Ordinarily, we would use SystemInfo.deviceUniqueIdentifier but for the purposes
                // of this demo we use Guid.NewGuid() so that developers can test against themselves locally.
                // Also note: SystemInfo.deviceUniqueIdentifier is not supported in WebGL.
                deviceId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString(GameConstants.DeviceIdKey, deviceId);
            }

            return deviceId;
        }

        #endregion

        #region Leaderboard

        private int CalculateSubScore()
        {
            int subScore = 1;
            /*//int maxSubscore = PlayerPrefs.GetInt(Save.MaxSubscore.ToString(), 0);
            foreach (GameObject hero in GameObject.FindGameObjectsWithTag("Hero"))
                subScore += (int)Math.Pow(2, hero.transform.parent.GetComponent<HeroSlot>().level - 1);
            if (subScore > maxSubscore)
            {
                PlayerPrefs.SetInt(Save.MaxSubscore.ToString(), subScore);
            }
            else
            {
                subScore = maxSubscore;
            }*/

            return subScore;
        }

        private int CalculateScore()
        {
            int score = GameManager.CurrentLevel;
            /*int maxScore = PlayerPrefs.GetInt(Save.MaxScore.ToString(), 0);
            if (score > maxScore)
            {
                PlayerPrefs.SetInt(Save.MaxScore.ToString(), score);
            }
            else
            {
                score = maxScore;
            }*/

            return score;
        }

        public async Task<IApiLeaderboardRecord> WriteLeaderboardRecordAsync(bool writeInCache = true)
        {
            if (!_connection.Socket.IsConnected) return null;

            cachedLeaderboard = UseCachedLeaderboard();
            if (cachedLeaderboard)
            {
                return cachedOurResult;
            }

            refreshedTime = DateTime.Now;

            IApiLeaderboardRecord ourResult = null;
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(timeout);
                try
                {
                    ourResult = await _connection.Client.WriteLeaderboardRecordAsync(_connection.Session, GameConstants.LeaderboardId, CalculateScore(),
                        CalculateSubScore(), "{\"n\":\"" + GameManager.Username + "\"}", canceller: cts.Token);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
                finally
                {
                    cts.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            if (writeInCache)
            {
                cachedOurResult = ourResult;
            }

            return ourResult;
        }

        public async Task<IApiLeaderboardRecordList> ListLeaderboardRecordsAsync(int limit)
        {
            if (!_connection.Socket.IsConnected) return null;
            if (cachedLeaderboard)
            {
                return cachedListResult;
            }

            IApiLeaderboardRecordList result = null;
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(timeout);
                try
                {
                    result = await _connection.Client.ListLeaderboardRecordsAsync(_connection.Session,
                        GameConstants.LeaderboardId, null, null, limit,
                        canceller: cts.Token);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
                finally
                {
                    cts.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            cachedListResult = result;
            return result;
        }

        private bool UseCachedLeaderboard()
        {
            return cachedOurResult != null &&
                   cachedListResult != null &&
                   cachedOurResult.Score == CalculateScore().ToString() &&
                   cachedOurResult.Subscore == CalculateSubScore().ToString() &&
                   cachedOurResult.Metadata.Replace(" ", "") == "{\"n\":\"" + GameManager.Username + "\"}" &&
                   DateTime.Now - refreshedTime < TimeSpan.FromMinutes(2);
        }

        public async Task DeleteLeaderboardRecordAsync()
        {
            try
            {
                await _connection.Client.DeleteLeaderboardRecordAsync(_connection.Session, GameConstants.LeaderboardId);
                Console.WriteLine("Record deleted successfully from the leaderboard.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error deleting leaderboard record: {e.Message}");
            }
        }

        #endregion

        #region Storage

        // Write payment details on nakama
        /*private async Task WritePaymentDetails(Purchase_Details details)
        {
            try
            {
                // Handling the uniqueness of the key
                System.Random random = new System.Random();
                int randomNumber = random.Next(100, 1000);

                // Convert the struct to a JSON string
                string jsonData = JsonConvert.SerializeObject(details);

                // Prepare the storage object to write
                var storageObject = new WriteStorageObject
                {
                    Collection = nameof(Purchase_Details),          // Collection name is "Payment_Details"
                    Key = details.PaymentSKU + "-" + randomNumber,  // Key is the PaymentSKU
                    Value = jsonData                                // Serialized JSON data
                };

                // Write the object to Nakama Storage
                await _connection.Client.WriteStorageObjectsAsync(_connection.Session, new[] { storageObject });
                Debug.Log("Payment details stored successfully.");
            }
            catch (ApiResponseException e)
            {
                Debug.LogError("Error writing payment details to storage: " + e.Message);
            }
        }
        // This method is written so that it can be used in other scripts
        public async void WritePaymentDetailsToStorage(Purchase_Details payment_Details)
        {
            await WritePaymentDetails(payment_Details);
        }*/

        #endregion

        #region BanManager

        // Log out the user by invalidating the session on the client side
        private void LogoutAndExit(ISession _session)
        {
            try
            {
                if (_session != null)
                {
                    // Invalidate the session by clearing it on the client
                    _session = null;

                    // Show a message to the user and quit the game
                    Debug.LogError("ShowbanPopup  ");
                    //ShowbanPopup();
                }
            }
            catch (ApiResponseException e)
            {
                Debug.LogError("An error occurred during logout: " + e.Message);
            }
        }

        private async void TriggerBannedAction(ISession _session, Client _client)
        {
            await DeleteLeaderboardRecordAsync();
            //PlayerPrefs.SetInt("IsUserBanned", 1);
            LogoutAndExit(_session);
        }

        // This method will call when user should be ban due to cheating conditions.
        //public async Task BanUser()
        //{
        //    await DeleteLeaderboardRecordAsync();
        //    PlayerPrefs.SetInt("IsUserBanned", 1);
        //    ShowbanPopup();
        //}
        // Show ban user popup
        /*public void ShowbanPopup()
        {
            //Debug.Log("nt- user id  is : " + _connection.Session.UserId);
            Instantiate(BanPopup, GameManager.staticUiParent.transform);
        }*/

        #endregion

        private async void OnApplicationQuit()
        {
            if (_connection)
            {
                try
                {
                    await _connection.Socket.CloseAsync();
                    NkBug.LogWarning("Closed connection");
                }
                catch
                {
                    NkBug.LogWarning("Couldn't Close connection");
                }
            }
        }
    }
}