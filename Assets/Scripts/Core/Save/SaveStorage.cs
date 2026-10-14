using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Untitled
{
    /// <summary>One slot. Backup recovery is read-only until the next explicit save.</summary>
    public sealed class SaveStorage
    {
        [Serializable]
        private sealed class Envelope { public int FormatVersion; public string Payload; public string Sha256; }
        private const long MaximumBytes = 4 * 1024 * 1024;
        private readonly object gate = new object();
        public string MainPath { get; }
        public string BackupPath => MainPath + ".bak";

        public SaveStorage(string directory) { MainPath = Path.Combine(directory, "slot_0.json"); }
        public static SaveStorage Default() => new SaveStorage(Path.Combine(Application.persistentDataPath, "Saves"));

        public SaveResult Load(out SaveData data)
        {
            lock (gate)
            {
                var main = Read(MainPath, out data);
                if (main.Success || main.Error == SaveError.UnsupportedVersion || main.Error == SaveError.Io) return main;
                var backup = Read(BackupPath, out var recovered);
                if (backup.Success) { data = recovered; return SaveResult.Ok(true); }
                if (backup.Error == SaveError.UnsupportedVersion || backup.Error == SaveError.Io) return backup;
                return main.Error == SaveError.NotFound ? backup : main;
            }
        }

        public SaveResult Save(SaveData data)
        {
            lock (gate)
            {
                var validation = SaveValidator.Validate(data);
                if (!validation.Success) return validation;
                string temporary = MainPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    var main = Read(MainPath, out _);
                    // Never downgrade an unsupported save, or replace the last unreadable copy.
                    if (main.Error == SaveError.UnsupportedVersion || main.Error == SaveError.Io) return main;
                    if (!main.Success && main.Error != SaveError.NotFound)
                    {
                        var backup = Read(BackupPath, out _);
                        if (!backup.Success) return backup.Error == SaveError.NotFound ? main : backup;
                    }
                    else if (main.Error == SaveError.NotFound)
                    {
                        var backup = Read(BackupPath, out _);
                        if (!backup.Success && backup.Error != SaveError.NotFound) return backup;
                    }
                    string payload = JsonUtility.ToJson(data);
                    var envelope = new Envelope { FormatVersion = 1, Payload = payload, Sha256 = Hash(payload) };
                    byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(envelope, true) + "\n");
                    if (bytes.Length > MaximumBytes) return SaveResult.Fail(SaveError.InvalidData, "Save exceeds size limit.");
                    Directory.CreateDirectory(Path.GetDirectoryName(MainPath));
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                    var verified = Read(temporary, out _);
                    if (!verified.Success) return verified;
                    if (File.Exists(MainPath))
                        File.Replace(temporary, MainPath, main.Success ? BackupPath : null);
                    else File.Move(temporary, MainPath);
                    return SaveResult.Ok();
                }
                catch (Exception exception) when (IsStorageException(exception))
                { return SaveResult.Fail(SaveError.Io, exception.Message); }
                finally
                {
                    // Only our uncommitted temporary file is disposable.
                    try { if (File.Exists(temporary)) File.Delete(temporary); }
                    catch (Exception exception) when (IsStorageException(exception)) { }
                }
            }
        }

        private SaveResult Read(string path, out SaveData data)
        {
            data = null;
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length == 0 || stream.Length > MaximumBytes) return SaveResult.Fail(SaveError.Corrupt, "Invalid file size.");
                    using (var reader = new StreamReader(stream, new UTF8Encoding(false, true)))
                    {
                        var envelope = JsonUtility.FromJson<Envelope>(reader.ReadToEnd());
                        if (envelope == null || envelope.Payload == null || envelope.Sha256 != Hash(envelope.Payload))
                            return SaveResult.Fail(SaveError.Corrupt, "Save checksum mismatch.");
                        if (envelope.FormatVersion != 1) return SaveResult.Fail(SaveError.UnsupportedVersion, "Unsupported file format.");
                        var candidate = JsonUtility.FromJson<SaveData>(envelope.Payload);
                        var result = SaveValidator.Validate(candidate);
                        if (result.Success) data = candidate;
                        return result;
                    }
                }
            }
            catch (FileNotFoundException) { return SaveResult.Fail(SaveError.NotFound, "Save not found."); }
            catch (DirectoryNotFoundException) { return SaveResult.Fail(SaveError.NotFound, "Save directory not found."); }
            catch (ArgumentException exception) { return SaveResult.Fail(SaveError.Corrupt, exception.Message); }
            catch (Exception exception) when (IsStorageException(exception)) { return SaveResult.Fail(SaveError.Io, exception.Message); }
        }

        private static bool IsStorageException(Exception exception) => exception is IOException
            || exception is UnauthorizedAccessException || exception is System.Security.SecurityException || exception is NotSupportedException;

        private static string Hash(string payload)
        {
            using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }
    }
}
