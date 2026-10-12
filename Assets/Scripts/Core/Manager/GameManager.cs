using UnityEngine;

namespace Untitled
{
    public class GameManager : ManagerBase<GameManager>
    {
        public GameData GameData;

        public bool IsPaused { get; private set; }

        public override void Initialize()
        {
            base.Initialize();
            GameData = new GameData();
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }
    }
}
