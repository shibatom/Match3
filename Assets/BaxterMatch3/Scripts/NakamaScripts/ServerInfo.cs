using System;

namespace NakamaOnline
{
    [Serializable]
    public class ServerInfo
    {
        public string scheme;
        public string host;
        public string serverKey;
        public int port;
        public int clientTimeOut;
    }
}
