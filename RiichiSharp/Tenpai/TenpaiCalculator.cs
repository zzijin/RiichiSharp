namespace RiichiSharp.Tenpai;

using RiichiSharp.Tiles;
using RiichiSharp.Wait;

/// <summary>独立听牌计算器。在单次面子枚举中同时检测听牌和获取待牌列表。
/// 比「向听数+进张」两步法更高效，参考 tempai-core 的 hand/tempai 实现。</summary>
public static class TenpaiCalculator
{
    /// <summary>听牌结果。</summary>
    public record TenpaiResult
    {
        /// <summary>是否听牌。</summary>
        public bool IsTenpai => Waits.Count > 0;
        /// <summary>听牌类型列表（可能有多种和了解读）。</summary>
        public List<TenpaiPattern> Patterns { get; init; } = [];
        /// <summary>全部待牌（去重合并）。</summary>
        public List<Tile> Waits => Patterns.SelectMany(p => p.WaitTiles).Distinct().ToList();
        /// <summary>每种待牌的枚数（4-手牌中已有-可见牌中）。</summary>
        public Dictionary<Tile, int> WaitCounts { get; set; } = [];
        public override string ToString() => IsTenpai ? $"听牌: {string.Join(" ", Waits)}" : "不听";
    }

    /// <summary>单种听牌模式。</summary>
    public record TenpaiPattern
    {
        public List<Tile> WaitTiles { get; init; } = [];
        public List<WaitType> WaitTypes { get; init; } = [];
        public override string ToString() => $"{string.Join("/", WaitTypes)}: {string.Join(" ", WaitTiles)}";
    }

    /// <summary>
    /// 检测一副13枚手牌是否听牌，返回全部待牌（含枚数）。
    /// 含副露面子的处理。
    /// </summary>
    public static TenpaiResult Calculate(TileSet tiles, int calledMelds = 0, TileSet? visibleTiles = null)
    {
        var result = new TenpaiResult();
        int remainingMelds = 4 - calledMelds;
        if (remainingMelds < 0) return result;

        // 在面子枚举中同时检测听牌
        var counts = tiles.ToArray();
        var foundPatterns = new HashSet<string>(); // 去重用

        // 尝试每种可能的雀头
        for (int pairIdx = 0; pairIdx < Tile.Count; pairIdx++)
        {
            if (counts[pairIdx] < 2) continue;
            int[] reduced = (int[])counts.Clone();
            reduced[pairIdx] -= 2;

            // 用面子枚举填充剩余牌，检查留牌是否形成听牌形
            FindTenpaiPatterns(reduced, remainingMelds, new Tile((byte)pairIdx), result.Patterns, foundPatterns);
        }

        // 也尝试无雀头的情况（七对子和国士会被向听数逻辑处理，这里仅标准形）
        FindTenpaiPatterns((int[])counts.Clone(), remainingMelds, null, result.Patterns, foundPatterns);

        // 计算每种待牌的枚数
        result.WaitCounts = CalculateWaitCounts(result.Waits, tiles, visibleTiles);

        return result;
    }

    /// <summary>递归面子枚举 + 听牌形检测。入口，从位置0开始搜索。</summary>
    private static void FindTenpaiPatterns(int[] counts, int needed, Tile? pair,
        List<TenpaiPattern> results, HashSet<string> seen)
        => FindTenpaiPatternsAt(counts, 0, needed, pair, results, seen);

    /// <summary>从指定位置开始搜索面子（跳过 empty 位置）。</summary>
    private static void FindTenpaiPatternsAt(int[] counts, int startPos, int needed, Tile? pair,
        List<TenpaiPattern> results, HashSet<string> seen)
    {
        int pos = startPos;
        while (pos < 34 && counts[pos] == 0) pos++;
        if (pos >= 34) { DetectWaitFromRemaining(counts, pair, results, seen); return; }

        var tile = new Tile((byte)pos);
        bool canKoutsu = counts[pos] >= 3;
        bool canShuntsu = tile.IsSuited && tile.Number <= 7 && counts[pos + 1] > 0 && counts[pos + 2] > 0;

        // 孤立牌：跳过
        if (!canKoutsu && !canShuntsu) { FindTenpaiPatternsAt(counts, pos + 1, needed, pair, results, seen); return; }

        // 尝试刻子
        if (canKoutsu) { counts[pos] -= 3; FindTenpaiPatternsAt(counts, pos, needed - 1, pair, results, seen); counts[pos] += 3; }
        // 尝试顺子
        if (canShuntsu) { counts[pos]--; counts[pos + 1]--; counts[pos + 2]--; FindTenpaiPatternsAt(counts, pos, needed - 1, pair, results, seen); counts[pos]++; counts[pos + 1]++; counts[pos + 2]++; }
        // 跳过
        FindTenpaiPatternsAt(counts, pos + 1, needed, pair, results, seen);
    }

    /// <summary>从剩余牌中检测听牌形（1-2张牌）。</summary>
    private static void DetectWaitFromRemaining(int[] counts, Tile? pair,
        List<TenpaiPattern> results, HashSet<string> seen)
    {
        int total = counts.Sum();
        if (total == 0) return; // 已经和了（4面子+雀头完整），不是听牌

        // 收集非零位置
        var positions = new List<int>();
        for (int i = 0; i < 34; i++)
            if (counts[i] > 0) positions.Add(i);

        if (positions.Count == 1)
        {
            // 单张牌：待牌 = 和这张牌相同的牌形成对子 → 单骑 (tanki)
            int idx = positions[0];
            if (pair == null)
            {
                var tile = new Tile((byte)idx);
                AddPattern(results, seen, [tile], WaitType.Tanki, pair);
            }
        }
        else if (positions.Count == 2)
        {
            int a = positions[0], b = positions[1];
            var ta = new Tile((byte)a); var tb = new Tile((byte)b);

            if (a == b)
            {
                // 两张相同：待牌 = 第三张相同牌形成刻子 → 双碰 (shanpon)
                // 前提：已有雀头或这两张就是雀头
                AddPattern(results, seen, [ta], WaitType.Shanpon, pair);
            }
            else if (ta.IsSuited && tb.IsSuited && ta.Suit == tb.Suit)
            {
                int diff = tb.Number - ta.Number;
                if (diff == 1)
                {
                    // 连续两张：①低端听 3→ 两面；②高端听 → 两面；③边张 (12→3, 89→7)
                    var low = new Tile((byte)(ta.Id - 1));
                    var high = new Tile((byte)(tb.Id + 1));
                    var waits = new List<(Tile, WaitType)>();
                    if (ta.Number > 1) waits.Add((low, ta.Number == 2 ? WaitType.Penchan : WaitType.Ryanmen));
                    if (tb.Number < 9) waits.Add((high, tb.Number == 8 ? WaitType.Penchan : WaitType.Ryanmen));
                    foreach (var (w, wt) in waits) AddPattern(results, seen, [w], wt, pair);
                }
                else if (diff == 2)
                {
                    // 隔一张：待牌 = 中间那张 → 嵌张 (kanchan)
                    var mid = new Tile((byte)(ta.Id + 1));
                    AddPattern(results, seen, [mid], WaitType.Kanchan, pair);
                }
            }
        }
    }

    private static void AddPattern(List<TenpaiPattern> results, HashSet<string> seen,
        List<Tile> waits, WaitType type, Tile? pair)
    {
        var key = $"{string.Join(",", waits.Select(w => w.Id).OrderBy(x => x))}|{type}";
        if (seen.Add(key))
            results.Add(new TenpaiPattern { WaitTiles = waits, WaitTypes = [type] });
    }

    /// <summary>计算每种待牌在牌山中剩余的枚数。</summary>
    private static Dictionary<Tile, int> CalculateWaitCounts(List<Tile> waits, TileSet hand, TileSet? visible)
    {
        var counts = new Dictionary<Tile, int>();
        foreach (var w in waits.Distinct())
        {
            int avail = 4 - hand[w.Id];
            if (visible != null) avail -= Math.Min(visible.Value[w.Id], avail);
            counts[w] = Math.Max(0, avail);
        }
        return counts;
    }
}
