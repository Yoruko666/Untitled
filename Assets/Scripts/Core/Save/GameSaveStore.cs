using System;
using System.IO;
using System.Security;
using UnityEngine;

namespace Untitled
{
    public sealed class GameSaveStore
    {
        public string Path { get; }
        public GameSaveStore(string path) => Path = path;

        public bool TrySave(GameStateSaveData data, out string error)
        {
            error = null;
            if (data == null)
            {
                error = "不能保存空状态。";
                return false;
            }
            try
            {
                string directory = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path));
                Directory.CreateDirectory(directory);
                string temporary = Path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                // Replace only after the complete temporary file has been written.
                if (File.Exists(Path)) File.Replace(temporary, Path, null);
                else File.Move(temporary, Path);
                return true;
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                error = exception.Message;
                return false;
            }
        }

        public bool TryLoad(out GameStateSaveData data, out string error)
        {
            data = null;
            error = null;
            try
            {
                data = JsonUtility.FromJson<GameStateSaveData>(File.ReadAllText(Path));
                if (data != null) return true;
                error = "存档内容为空。";
            }
            catch (Exception exception) when (IsFileError(exception)) { error = exception.Message; }
            return false;
        }

        private static bool IsFileError(Exception exception) => exception is IOException
            || exception is UnauthorizedAccessException || exception is ArgumentException
            || exception is NotSupportedException || exception is SecurityException;
    }
}
