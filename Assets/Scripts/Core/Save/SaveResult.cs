namespace Untitled
{
    public enum SaveError { None, NotFound, Corrupt, UnsupportedVersion, InvalidData, Io, Busy, NotReady, PhaseUnavailable }

    public sealed class SaveResult
    {
        public bool Success => Error == SaveError.None;
        public SaveError Error { get; }
        public string Message { get; }
        public bool RecoveredFromBackup { get; }
        private SaveResult(SaveError error, string message, bool recovered)
        { Error = error; Message = message; RecoveredFromBackup = recovered; }
        public static SaveResult Ok(bool recovered = false) => new SaveResult(SaveError.None, "", recovered);
        public static SaveResult Fail(SaveError error, string message) => new SaveResult(error, message, false);
    }
}
