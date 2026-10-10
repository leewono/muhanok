using System;
using System.Collections.Generic;
using System.IO;
using Muhanok.Domain;
using UnityEngine;

namespace Muhanok.Infrastructure
{
    // Exhibition data on this PC: the shared coin pool, unlocked items and one ranking file per day.
    public sealed class ExhibitionStore
    {
        [Serializable] private sealed class RankingFile { public List<RankingEntry> Entries = new List<RankingEntry>(); }
        [Serializable] private sealed class ProgressFile { public int CoinPool; public List<int> Unlocked = new List<int>(); }
        private readonly string folder;
        public readonly ExhibitionProgress Progress = new ExhibitionProgress();
        public DailyRanking Today { get; private set; }
        public ExhibitionStore(string root = null, bool unlockAll = false)
        {
            folder = Path.Combine(root ?? UnityEngine.Application.persistentDataPath, "Exhibition");
            Progress.UnlockAll = unlockAll;
            LoadProgress(); LoadToday();
        }
        private string RankingPath(string day) => Path.Combine(folder, "ranking-" + day + ".json");
        private string ProgressPath => Path.Combine(folder, "progress.json");
        public void LoadToday()
        {
            Today = new DailyRanking(DailyRanking.DayKey(DateTime.Now));
            var file = Read<RankingFile>(RankingPath(Today.Day));
            Today.Load(file?.Entries);
        }
        // Returns the 1-based rank for today.
        public int Record(RankingEntry entry)
        {
            if (Today.Day != DailyRanking.DayKey(DateTime.Now)) LoadToday();
            int rank = Today.Add(entry);
            Write(RankingPath(Today.Day), new RankingFile { Entries = new List<RankingEntry>(Today.Entries) });
            return rank;
        }
        public void ClearToday() { Today.Clear(); Write(RankingPath(Today.Day), new RankingFile()); }
        public void DepositCoins(int coins) { Progress.Deposit(coins); SaveProgress(); }
        public bool Unlock(ItemKind kind) { bool done = Progress.TryUnlock(kind); if (done) SaveProgress(); return done; }
        private void LoadProgress()
        {
            var file = Read<ProgressFile>(ProgressPath);
            if (file == null) return;
            var kinds = new List<ItemKind>(); foreach (int id in file.Unlocked) if (Enum.IsDefined(typeof(ItemKind), id)) kinds.Add((ItemKind)id);
            Progress.Restore(file.CoinPool, kinds);
        }
        private void SaveProgress()
        {
            var file = new ProgressFile { CoinPool = Progress.CoinPool };
            foreach (var kind in Progress.Unlocked) file.Unlocked.Add((int)kind);
            Write(ProgressPath, file);
        }
        private static T Read<T>(string path) where T : class
        {
            try { return File.Exists(path) ? JsonUtility.FromJson<T>(File.ReadAllText(path)) : null; }
            catch (Exception error) { Debug.LogWarning("Exhibition data unreadable: " + path + " " + error.Message); return null; }
        }
        private void Write(string path, object data)
        {
            try { Directory.CreateDirectory(folder); File.WriteAllText(path, JsonUtility.ToJson(data, true)); }
            catch (Exception error) { Debug.LogWarning("Exhibition data not saved: " + error.Message); }
        }
    }
}
