using UnityEngine;

namespace BaxterMatch3.Animation.Directory.AnimationUI.Demo
{
    public class Main
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialization()
        {
            ScriptSingleton.Initialize();
        }
    }

}