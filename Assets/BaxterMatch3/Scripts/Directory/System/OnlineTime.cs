

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Internal.Scripts.System
{
    public class OnlineTime : MonoBehaviour
    {
        public static OnlineTime THIS;
        public DateTime serverTime;
        public bool dateReceived;
        public delegate void DateReceived();
        public static event DateReceived OnDateReceived;
        [Header("Test date example: 2019-08-27 09:12:29")]
        public string TestDate; 
        private void Awake()
        {
            if (THIS == null)
                THIS = this;
            else if(THIS != this)
                Destroy(gameObject);
            GetServerTime();
        }

        private void OnEnable()
        {
            GetServerTime();
        }

        void GetServerTime ()
        {
            StartCoroutine(getTime());
        }

        #if UNITY_ANDROID
        void OnApplicationFocus ( bool focus )
        {
            if (focus)
                GetServerTime();
            else
                dateReceived = false;
        }
        #endif

        #if UNITY_EDITOR || UNITY_IOS
        void OnApplicationPause ( bool pause )
        {
            if ( !pause )	                
                GetServerTime();
            else
                dateReceived = false;
        }
        #endif
        
        IEnumerator getTime()
        {
#if UNITY_WEBGL
            serverTime = DateTime.Now;
#else
            UnityWebRequest www = UnityWebRequest.Get("https://timeserver12536721345.000webhostapp.com/gettime.php");
            yield return www.SendWebRequest();
            yield return new WaitUntil(() => www.downloadHandler.isDone);
            if(www.downloadHandler.text != "")
                serverTime = DateTime.Parse(www.downloadHandler.text);
            else
                serverTime = DateTime.Now;
            if(TestDate!="" && (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor))
                serverTime = DateTime.Parse(TestDate);
#endif
            //Debug.Log(serverTime);
            yield return  null;
            dateReceived = true;
            OnDateReceived?.Invoke();
        }
    }
}