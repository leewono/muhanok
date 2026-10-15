using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuhanokOverlay
{
    [Serializable]
    public class LeaderboardRecord
    {
        public string name;
        public float meters;
        public string date;
        public long id;
    }

    public static class LeaderboardStore
    {
        const string Key = "overlay_leaderboard";
        const int MaxRecords = 100;

        [Serializable]
        class Wrapper { public List<LeaderboardRecord> items = new List<LeaderboardRecord>(); }

        public static string Today { get { return DateTime.Now.ToString("yyyy-MM-dd"); } }

        public static List<LeaderboardRecord> LoadAll()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new List<LeaderboardRecord>();
            var w = JsonUtility.FromJson<Wrapper>(json);
            return w != null && w.items != null ? w.items : new List<LeaderboardRecord>();
        }

        static void SaveAll(List<LeaderboardRecord> list)
        {
            list.Sort((a, b) => b.meters.CompareTo(a.meters));
            if (list.Count > MaxRecords) list.RemoveRange(MaxRecords, list.Count - MaxRecords);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(new Wrapper { items = list }));
            PlayerPrefs.Save();
        }

        public static LeaderboardRecord Add(float meters, string name = null)
        {
            var list = LoadAll();
            var rec = new LeaderboardRecord
            {
                name = string.IsNullOrEmpty(name) ? "Player " + (list.Count + 1) : name,
                meters = meters,
                date = Today,
                id = DateTime.Now.Ticks
            };
            list.Add(rec);
            SaveAll(list);
            return rec;
        }

        public static List<LeaderboardRecord> Sorted(bool todayOnly)
        {
            var list = LoadAll();
            if (todayOnly) list = list.FindAll(r => r.date == Today);
            list.Sort((a, b) => b.meters.CompareTo(a.meters));
            return list;
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
