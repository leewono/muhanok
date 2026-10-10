using System;
using System.Collections.Generic;

namespace Muhanok.Domain
{
    public enum ItemKind { Magnet, DoubleScore, Shield }

    public sealed class ItemDefinition
    {
        public readonly ItemKind Kind; public readonly string Name, Description; public readonly int UnlockCost; public readonly float Seconds;
        public ItemDefinition(ItemKind kind, string name, string description, int cost, float seconds)
        { Kind = kind; Name = name; Description = description; UnlockCost = cost; Seconds = seconds; }
    }

    // Item types are data so new ones only need an entry here and an effect hook.
    public static class ItemCatalog
    {
        public static readonly ItemDefinition[] All =
        {
            new ItemDefinition(ItemKind.Magnet, "자석", "옆 레인의 코인까지 끌어당깁니다.", 30, 8f),
            new ItemDefinition(ItemKind.DoubleScore, "점수 2배", "잠시 동안 모든 점수가 두 배입니다.", 60, 10f),
            new ItemDefinition(ItemKind.Shield, "보호막", "장애물 충돌을 한 번 막아 줍니다.", 100, 15f),
        };
        public static ItemDefinition Get(ItemKind kind) { foreach (var item in All) if (item.Kind == kind) return item; return All[0]; }
    }

    public sealed class PowerUpState
    {
        private readonly float[] remaining = new float[Enum.GetValues(typeof(ItemKind)).Length];
        public int Collected { get; private set; }
        public bool IsActive(ItemKind kind) => remaining[(int)kind] > 0f;
        public float Remaining(ItemKind kind) => remaining[(int)kind];
        public void Activate(ItemKind kind) { remaining[(int)kind] = ItemCatalog.Get(kind).Seconds; Collected++; }
        public bool ConsumeShield()
        {
            if (!IsActive(ItemKind.Shield)) return false;
            remaining[(int)ItemKind.Shield] = 0f; return true;
        }
        public void Tick(float dt) { for (int i = 0; i < remaining.Length; i++) remaining[i] = Math.Max(0f, remaining[i] - Math.Max(0f, dt)); }
        public void Reset() { Array.Clear(remaining, 0, remaining.Length); Collected = 0; }
    }

    // Coins from every visitor go into one exhibition pool that unlocks items for everybody.
    public sealed class ExhibitionProgress
    {
        public int CoinPool { get; private set; }
        public bool UnlockAll { get; set; }
        private readonly HashSet<ItemKind> unlocked = new HashSet<ItemKind>();
        public IEnumerable<ItemKind> Unlocked => unlocked;
        public bool IsUnlocked(ItemKind kind) => UnlockAll || unlocked.Contains(kind);
        public void Deposit(int coins) { CoinPool += Math.Max(0, coins); }
        public bool TryUnlock(ItemKind kind)
        {
            if (IsUnlocked(kind)) return false;
            int cost = ItemCatalog.Get(kind).UnlockCost;
            if (CoinPool < cost) return false;
            CoinPool -= cost; unlocked.Add(kind); return true;
        }
        public void Restore(int pool, IEnumerable<ItemKind> kinds)
        {
            CoinPool = Math.Max(0, pool); unlocked.Clear();
            if (kinds != null) foreach (var kind in kinds) unlocked.Add(kind);
        }
        public List<ItemKind> Available()
        {
            var list = new List<ItemKind>();
            foreach (var item in ItemCatalog.All) if (IsUnlocked(item.Kind)) list.Add(item.Kind);
            return list;
        }
    }
}
