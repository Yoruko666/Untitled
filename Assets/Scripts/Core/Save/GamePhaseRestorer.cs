using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Untitled
{
    public interface IGamePhaseRestorer
    {
        SaveResult Validate(GamePhase phase);
        Task EnterAsync(GameSession session);
    }

    /// <summary>Optional scene host hook, called after restore and before UI publication.</summary>
    public interface ISessionSceneSystem { void RestoreSession(GameSession session); }

    public sealed class GamePhaseRestorer : IGamePhaseRestorer
    {
        private readonly string dayScene;
        private readonly string nightScene;
        public GamePhaseRestorer(string dayScene = "Bussiness", string nightScene = "Adventure")
        { this.dayScene = dayScene; this.nightScene = nightScene; }
        private string SceneFor(GamePhase phase) => phase == GamePhase.Day ? dayScene : nightScene;

        public SaveResult Validate(GamePhase phase)
        {
            if (phase != GamePhase.Day && phase != GamePhase.Night)
                return SaveResult.Fail(SaveError.PhaseUnavailable, "No scene flow is available for this phase.");
            string scene = SceneFor(phase);
            if (SceneManager.GetActiveScene().name == scene || Application.CanStreamedLevelBeLoaded(scene)) return SaveResult.Ok();
            return SaveResult.Fail(SaveError.PhaseUnavailable, "Add the phase scene to Build Settings: " + scene);
        }

        public async Task EnterAsync(GameSession session)
        {
            var validation = Validate(session.Phase);
            if (!validation.Success) throw new InvalidOperationException(validation.Message);
            string scene = SceneFor(session.Phase);
            if (SceneManager.GetActiveScene().name != scene)
            {
                var operation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
                if (operation == null) throw new InvalidOperationException("Scene load did not start: " + scene);
                while (!operation.isDone) await Task.Yield();
            }
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component is ISessionSceneSystem system) system.RestoreSession(session);
        }
    }
}
