using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Infrastructure
{
    public static class UserSettingsStore
    {
        private const string Key = "Muhanok.UserSettings";
        public static UserSettings Load(UserSettings defaults)
        {
            var settings = defaults?.Clone() ?? new UserSettings();
            string json = PlayerPrefs.GetString(Key, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { JsonUtility.FromJsonOverwrite(json, settings); }
                catch (System.ArgumentException) { Debug.LogWarning("Muhanok: saved settings were unreadable; using defaults."); }
            }
            settings.Clamp(); return settings;
        }
        public static void Save(UserSettings settings) { PlayerPrefs.SetString(Key, JsonUtility.ToJson(settings)); PlayerPrefs.Save(); }
    }
}
