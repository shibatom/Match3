

namespace Internal.Scripts.Items
{
    /// <summary>
    /// Game blocks handler. It lock ups the game on animations
    /// </summary>
    public class In_GameBlocker : UnityEngine.MonoBehaviour
    {
        private void Start()
        {
            MainManager.Instance._stopFall.Add(this);
        }

        private void OnDisable()
        {
            Destroy(this);
        }

        private void OnDestroy()
        {
            MainManager.Instance._stopFall.Remove(this);
        }
    }
}