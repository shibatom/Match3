

using System.Collections.Generic;
using Internal.Scripts.System;
using UnityEngine;

namespace Internal.Scripts
{
    public static class DebugLogManager
    {
        public static List<DebugLogElement> logListFalling = new List<DebugLogElement>();
        public static List<DebugLogElement> logListDestroying = new List<DebugLogElement>();
        public static List<DebugLogElement> logListBonus = new List<DebugLogElement>();
        private static DebugSettings _debugSettings;

        public static void Log(string str, LogType type, bool forceStackTrace = false)
        {
#if UNITY_EDITOR
            // StackFrame stackFrame = new System.Diagnostics.StackTrace(1).GetFrame(1);
            // string fileName = stackFrame.GetFileName();
            // string methodName = stackFrame.GetMethod().ToString();
            // int lineNumber = stackFrame.GetFileLineNumber();

            // UnityEngine.Debug.Log(string.Format("{0}({1}:{2})\n{3}", methodName, Path.GetFileName(fileName), lineNumber, ""));
            string extractStackTrace = null;
            switch (type)
            {
                case LogType.Falling:
                    if(_debugSettings.StackTrace || forceStackTrace)
                        extractStackTrace = StackTraceUtility.ExtractStackTrace ();
                    logListFalling.Add(new DebugLogElement(str,extractStackTrace, Time.time));
                    break;
                case LogType.Destroying:
                    if(_debugSettings.StackTrace || forceStackTrace)
                        extractStackTrace = StackTraceUtility.ExtractStackTrace ();
                    logListDestroying.Add(new DebugLogElement(str,extractStackTrace, Time.time));
                    break;
                case LogType.BonusAppearance:
                    if(_debugSettings.StackTrace || forceStackTrace)
                        extractStackTrace = StackTraceUtility.ExtractStackTrace ();
                    logListBonus.Add(new DebugLogElement(str,extractStackTrace, Time.time));
                    break;
            }

//        Debug.Log(UnityEngine.StackTraceUtility.ExtractStackTrace ());
            if (_debugSettings.ShowLogImmediately)
                Debug.Log(str);

#endif
        }

        public static string GetLog(string id, LogType type)
        {
            string txt = "";
            List<DebugLogElement> list = new List<DebugLogElement>();
            switch (type)
            {
                case LogType.Falling:
                    list = logListFalling;
                    break;
                case LogType.Destroying:
                    list = logListDestroying;
                    break;
                case LogType.BonusAppearance:
                    list = logListBonus;
                    break;
            }

            foreach (var item in list)
            {
                if (item.str.Contains(id))
                {
                    string v = item.str + "\n";
                    Debug.Log(item.time + " " + v);
                    v = "Stack trace: " + item.references + "\n";
                    Debug.Log(v);
                    txt += v;
                }
            }

            return txt;
        }

        public enum LogType
        {
            Falling,
            Destroying,
            BonusAppearance
        }

        public class DebugLogElement
        {
            public string str;
            public string references;
            public float time;

            public DebugLogElement(string str, string references, float time)
            {
                this.str = str;
                this.references = references;
                this.time = time;
            }
        }

        public static void Init()
        {
            _debugSettings = Resources.Load("Scriptable/DebugSettings") as DebugSettings;
        }
    }
}