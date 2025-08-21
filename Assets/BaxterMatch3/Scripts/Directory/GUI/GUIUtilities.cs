

using HelperScripts;
using Internal.Scripts.System;
using UnityEngine;

namespace Internal.Scripts.GUI
{
    /// <summary>
    /// Spends a life after game started or offers to buy a life
    /// </summary>
    public class GUIUtilities : MonoBehaviour
    {
        public DebugSettings DebugSettings;

        public static GUIUtilities Instance;

        private void Start()
        {
            DebugSettings = Resources.Load<DebugSettings>("Scriptable/DebugSettings");
            if (!Equals(Instance, this)) Instance = this;
        }

        public void StartGame()
        {
            if (GlobalValue.Life > 0 || DebugSettings.AI)
            {
                if (MainManager.Instance.gameStatus == GameState.PrepareGame) return;
                ResourceManager.LifeAmount--;
                MainManager.Instance.gameStatus = GameState.PrepareGame;
            }
            else
            {
                BuyLifeShop();
            }
        }

        public void BuyLifeShop()
        {
            if (GlobalValue.Life < Initiations.Instance.CapOfLife)
                ReferencerUI.Instance.LiveShop.gameObject.SetActive(true);
        }
    }
}