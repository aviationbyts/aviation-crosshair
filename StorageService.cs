using System;
using System.IO;
using System.Text.Json;
using AviationCrosshair.Models;

namespace AviationCrosshair.Services
{
    /// <summary>
    /// Handles all local persistence. Data lives in
    /// %AppData%\AviationCrosshair\appdata.json - survives restarts, and is never
    /// allowed to crash the app: any read/parse failure falls back to defaults.
    /// </summary>
    public class StorageService
    {
        private readonly string _folder;
        private readonly string _dataFile;

        public StorageService()
        {
            _folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AviationCrosshair");
            Directory.CreateDirectory(_folder);
            _dataFile = Path.Combine(_folder, "appdata.json");
        }

        public AppData Load()
        {
            try
            {
                if (!File.Exists(_dataFile))
                    return new AppData();

                var json = File.ReadAllText(_dataFile);
                var data = JsonSerializer.Deserialize<AppData>(json, JsonOptions());
                return data ?? new AppData();
            }
            catch
            {
                // Corrupt or unreadable file: never crash, just start fresh.
                return new AppData();
            }
        }

        public bool Save(AppData data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, JsonOptions());
                var tmp = _dataFile + ".tmp";
                File.WriteAllText(tmp, json);
                File.Copy(tmp, _dataFile, overwrite: true);
                File.Delete(tmp);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string DataFolder => _folder;

        public static JsonSerializerOptions JsonOptions() => new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = false
        };

        /// <summary>
        /// Export a single crosshair to a chosen file path. Returns false on failure
        /// instead of throwing, per the app's "never crash on I/O" requirement.
        /// </summary>
        public bool ExportCrosshair(CrosshairSettings crosshair, string path)
        {
            try
            {
                var json = JsonSerializer.Serialize(crosshair, JsonOptions());
                File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryImportCrosshair(string path, out CrosshairSettings? crosshair, out string error)
        {
            crosshair = null;
            error = "";
            try
            {
                var json = File.ReadAllText(path);
                var imported = JsonSerializer.Deserialize<CrosshairSettings>(json, JsonOptions());
                if (imported == null || string.IsNullOrWhiteSpace(imported.Name))
                {
                    error = "Could not import crosshair. The selected file is invalid.";
                    return false;
                }
                imported.Id = Guid.NewGuid().ToString("N");
                imported.IsBuiltIn = false;
                crosshair = imported;
                return true;
            }
            catch
            {
                error = "Could not import crosshair. The selected file is invalid.";
                return false;
            }
        }

        public bool ExportProfile(Profile profile, string path)
        {
            try
            {
                var json = JsonSerializer.Serialize(profile, JsonOptions());
                File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryImportProfile(string path, out Profile? profile, out string error)
        {
            profile = null;
            error = "";
            try
            {
                var json = File.ReadAllText(path);
                var imported = JsonSerializer.Deserialize<Profile>(json, JsonOptions());
                if (imported == null || string.IsNullOrWhiteSpace(imported.Name))
                {
                    error = "Could not import profile. The selected file is invalid.";
                    return false;
                }
                imported.Id = Guid.NewGuid().ToString("N");
                profile = imported;
                return true;
            }
            catch
            {
                error = "Could not import profile. The selected file is invalid.";
                return false;
            }
        }
    }
}
