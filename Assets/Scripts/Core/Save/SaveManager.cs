using System;
using System.Threading.Tasks;

namespace Untitled
{
    public sealed class SaveManager : ManagerBase<SaveManager>
    {
        private readonly GameSession session;
        private readonly SaveStorage storage;
        private readonly IGamePhaseRestorer phaseRestorer;
        private bool busy;
        public SaveStorage Storage => storage;

        public SaveManager(GameSession session, SaveStorage storage, IGamePhaseRestorer phaseRestorer)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.phaseRestorer = phaseRestorer ?? throw new ArgumentNullException(nameof(phaseRestorer));
        }

        public SaveResult Save()
        {
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && UNTITLED_DEVELOPER_TOOLS)
            if (GameManager.TryGet(out var manager) && ReferenceEquals(manager.GameData, session.GameData) && manager.DeveloperStateModified)
                return SaveResult.Fail(SaveError.NotReady, "Developer-modified sessions cannot overwrite ordinary saves.");
#endif
            if (busy) return SaveResult.Fail(SaveError.Busy, "Restore in progress.");
            if (!session.IsReady) return SaveResult.Fail(SaveError.NotReady, "Session is not ready.");
            return storage.Save(session.Capture());
        }

        public async Task<SaveResult> LoadAsync()
        {
            if (busy) return SaveResult.Fail(SaveError.Busy, "Restore in progress.");
            var result = storage.Load(out var data);
            if (!result.Success) return result;
            return await RestoreAsync(data, result.RecoveredFromBackup);
        }

        /// <summary>Starts runtime state only. Does not overwrite the slot.</summary>
        public Task<SaveResult> NewGameAsync() => RestoreAsync(SaveData.NewGame(), false);

        private async Task<SaveResult> RestoreAsync(SaveData data, bool recovered)
        {
            if (busy) return SaveResult.Fail(SaveError.Busy, "Restore in progress.");
            var validation = SaveValidator.Validate(data);
            if (!validation.Success) return validation;
            var phase = phaseRestorer.Validate(data.Progress.Phase);
            if (!phase.Success) return phase;
            var previous = session.Capture();
            busy = true;
            try
            {
                session.Restore(data);
                await phaseRestorer.EnterAsync(session);
                session.CompleteRestore();
                return SaveResult.Ok(recovered);
            }
            catch (Exception exception)
            {
                session.Restore(previous);
                session.CompleteRestore();
                return SaveResult.Fail(SaveError.PhaseUnavailable, exception.Message);
            }
            finally { busy = false; }
        }
    }
}
