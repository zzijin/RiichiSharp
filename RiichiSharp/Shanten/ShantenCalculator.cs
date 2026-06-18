namespace RiichiSharp.Shanten;

using RiichiSharp.Tiles;

/// <summary>向听数计算器。使用递归回溯（移植自 agari 的 shanten.rs）。支持标准形、七对子、国士无双三种和了形。</summary>
public static class ShantenCalculator
{
    public enum ShantenType { Standard, Chiitoitsu, Kokushi }
    public record ShantenResult { public int Shanten { get; init; } public ShantenType BestType { get; init; } public int StandardShanten { get; init; } public int ChiitoitsuShanten { get; init; } public int KokushiShanten { get; init; } }

    public static ShantenResult Calculate(TileSet tiles, int calledMelds = 0)
    {
        int s = CalculateStandard(tiles, calledMelds);
        int c = calledMelds > 0 ? 99 : CalculateChiitoitsu(tiles);
        int k = calledMelds > 0 ? 99 : CalculateKokushi(tiles);
        int best = Math.Min(s, Math.Min(c, k));
        return new ShantenResult { Shanten = best, BestType = best == s ? ShantenType.Standard : best == c ? ShantenType.Chiitoitsu : ShantenType.Kokushi, StandardShanten = s, ChiitoitsuShanten = c, KokushiShanten = k };
    }

    // ============ 标准形向听 ============

    /// <summary>公式: 8 - 2×面子 - 搭子 - 雀头。含搭子上限、超额搭子惩罚、牌数不足处理。</summary>
    private static int CalculateStandard(TileSet tiles, int calledMelds)
    {
        int[] counts = tiles.ToArray();
        int total = tiles.TotalCount;
        int minTenpai = calledMelds >= 4 ? 1 : Math.Max(0, 13 - 3 * calledMelds);
        int deficit = Math.Max(0, minTenpai - total);
        int clampedCalled = Math.Min(calledMelds, 4);
        int best = 99;

        for (int i = 0; i < Tile.Count; i++)
        {
            if (counts[i] < 2) continue;
            int[] reduced = (int[])counts.Clone(); reduced[i] -= 2;
            var (hm, ta) = CountMeldsAndTaatsu(reduced);
            int tm = Math.Min(hm + clampedCalled, 4);
            int maxUseful = 4 - tm, useful = Math.Min(ta, maxUseful);
            int s = 8 - 2 * tm - useful - 1;
            if (tm + useful > 4) s += (tm + useful - 4);
            if (s >= 0) s = Math.Max(s, deficit);
            best = Math.Min(best, s);
        }

        {
            var (hm, ta) = CountMeldsAndTaatsu(counts);
            int tm = Math.Min(hm + clampedCalled, 4);
            int useful = Math.Min(ta, 4 - tm);
            int s = 8 - 2 * tm - useful - 0;
            if (tm + useful > 4) s += (tm + useful - 4);
            if (s >= 0) s = Math.Max(s, deficit);
            best = Math.Min(best, s);
        }
        return Math.Max(-1, best);
    }

    private static (int, int) CountMeldsAndTaatsu(int[] counts)
    {
        (int m0, int t0) = CountSuitMeldsAndTaatsu(counts, 0);
        (int m1, int t1) = CountSuitMeldsAndTaatsu(counts, 9);
        (int m2, int t2) = CountSuitMeldsAndTaatsu(counts, 18);
        int hm = 0, ht = 0;
        for (int i = 27; i < 34; i++) { int c = counts[i]; if (c >= 3) { hm++; c -= 3; } if (c >= 2) ht++; }
        return (m0 + m1 + m2 + hm, t0 + t1 + t2 + ht);
    }

    /// <summary>使用递归回溯找出一门花色中的最优（面子数, 搭子数）组合。</summary>
    private static (int, int) CountSuitMeldsAndTaatsu(int[] counts, int start)
    {
        int[] suit = new int[9]; Array.Copy(counts, start, suit, 0, 9);
        int maxMelds = FindMaxMelds((int[])suit.Clone(), 0);
        int bestTaatsu = FindBestTaatsuForMelds((int[])suit.Clone(), 0, maxMelds);
        return (maxMelds, bestTaatsu);
    }

    private static int FindMaxMelds(int[] suit, int pos)
    {
        while (pos < 9 && suit[pos] == 0) pos++;
        if (pos >= 9) return 0;
        int best = 0;
        if (suit[pos] >= 3) { suit[pos] -= 3; best = Math.Max(best, 1 + FindMaxMelds(suit, pos)); suit[pos] += 3; }
        if (pos <= 6 && suit[pos] >= 1 && suit[pos + 1] >= 1 && suit[pos + 2] >= 1) { suit[pos]--; suit[pos + 1]--; suit[pos + 2]--; best = Math.Max(best, 1 + FindMaxMelds(suit, pos)); suit[pos]++; suit[pos + 1]++; suit[pos + 2]++; }
        best = Math.Max(best, FindMaxMelds(suit, pos + 1));
        return best;
    }

    private static int FindBestTaatsuForMelds(int[] suit, int pos, int remaining)
    {
        while (pos < 9 && suit[pos] == 0) pos++;
        if (remaining == 0) return CountTaatsuRecursive((int[])suit.Clone(), 0);
        if (pos >= 9) return -99;
        int best = -99;
        if (suit[pos] >= 3) { suit[pos] -= 3; best = Math.Max(best, FindBestTaatsuForMelds(suit, pos, remaining - 1)); suit[pos] += 3; }
        if (pos <= 6 && suit[pos] >= 1 && suit[pos + 1] >= 1 && suit[pos + 2] >= 1) { suit[pos]--; suit[pos + 1]--; suit[pos + 2]--; best = Math.Max(best, FindBestTaatsuForMelds(suit, pos, remaining - 1)); suit[pos]++; suit[pos + 1]++; suit[pos + 2]++; }
        best = Math.Max(best, FindBestTaatsuForMelds(suit, pos + 1, remaining));
        return best;
    }

    /// <summary>递归搭子计数：尝试对子、两面/边张、嵌张，正确消耗牌张。</summary>
    private static int CountTaatsuRecursive(int[] suit, int pos)
    {
        while (pos < 9 && suit[pos] == 0) pos++;
        if (pos >= 9) return 0;
        int best = 0;
        if (suit[pos] >= 2) { suit[pos] -= 2; best = Math.Max(best, 1 + CountTaatsuRecursive(suit, pos)); suit[pos] += 2; }
        if (pos <= 7 && suit[pos] >= 1 && suit[pos + 1] >= 1) { suit[pos]--; suit[pos + 1]--; best = Math.Max(best, 1 + CountTaatsuRecursive(suit, pos)); suit[pos]++; suit[pos + 1]++; }
        if (pos <= 6 && suit[pos] >= 1 && suit[pos + 2] >= 1) { suit[pos]--; suit[pos + 2]--; best = Math.Max(best, 1 + CountTaatsuRecursive(suit, pos)); suit[pos]++; suit[pos + 2]++; }
        best = Math.Max(best, CountTaatsuRecursive(suit, pos + 1));
        return best;
    }

    // ============ 七对子向听 ============
    /// <summary>公式: 6 - 对子数 + max(0, 7 - 牌种类数)。四张相同牌在七对子中只算1对。</summary>
    private static int CalculateChiitoitsu(TileSet tiles)
    {
        int pairs = 0, unique = 0;
        for (int i = 0; i < Tile.Count; i++) { int c = tiles[i]; if (c > 0) unique++; if (c >= 2) pairs++; }
        return 6 - pairs + Math.Max(0, 7 - unique);
    }

    // ============ 国士无双向听 ============
    private static readonly int[] KokushiIndices = [0, 8, 9, 17, 18, 26, 27, 28, 29, 30, 31, 32, 33];
    /// <summary>公式: 13 - 幺九牌种类数 - (有对子 ? 1 : 0)。</summary>
    private static int CalculateKokushi(TileSet tiles)
    {
        int unique = 0; bool hasPair = false;
        foreach (int i in KokushiIndices) { if (tiles[i] > 0) unique++; if (tiles[i] >= 2) hasPair = true; }
        return 13 - unique - (hasPair ? 1 : 0);
    }
}
