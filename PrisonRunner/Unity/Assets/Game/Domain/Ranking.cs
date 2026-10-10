using System;
using System.Collections.Generic;

namespace Muhanok.Domain
{
    public enum PlayStyle { FullBody, UpperBody, Keyboard }

    // One exhibition visitor: kept across "다시 하기", cleared by "다음 관람객".
    public sealed class VisitorProfile
    {
        public string Name = "";
        public string Weight = "";
        public PlayStyle Style = PlayStyle.FullBody;
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "관람객" : Name.Trim();
    }

    public static class PlayStyleInfo
    {
        public static string Label(PlayStyle style) => style == PlayStyle.FullBody ? "전신" : style == PlayStyle.UpperBody ? "상체" : "키보드";
        // 2024 Adult Compendium: 22240 total-body active game 4 MET; upper-body active game about 2.5 MET; seated play 1.3 MET.
        public static double Met(PlayStyle style) => style == PlayStyle.FullBody ? 4 : style == PlayStyle.UpperBody ? 2.5 : 1.3;
        public static bool UsesCamera(PlayStyle style) => style != PlayStyle.Keyboard;
    }

    [Serializable]
    public sealed class RankingEntry
    {
        public string Name; public int Score; public float Distance; public int Coins; public string Style; public long Ticks;
    }

    // Exhibition ranking: only the given day's records count.
    public sealed class DailyRanking
    {
        private readonly List<RankingEntry> entries = new List<RankingEntry>();
        public string Day { get; private set; }
        public IReadOnlyList<RankingEntry> Entries => entries;
        public DailyRanking(string day) { Day = day; }
        public static string DayKey(DateTime time) => time.ToString("yyyyMMdd");
        public int Add(RankingEntry entry)
        {
            entries.Add(entry); Sort();
            return entries.IndexOf(entry) + 1;
        }
        public void Load(IEnumerable<RankingEntry> saved) { entries.Clear(); if (saved != null) entries.AddRange(saved); Sort(); }
        public void Clear() => entries.Clear();
        private void Sort() => entries.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : a.Ticks.CompareTo(b.Ticks));
    }
}
