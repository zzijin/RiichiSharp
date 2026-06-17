namespace MahjongAlgorithms.Hand;

using MahjongAlgorithms.Tiles;

/// <summary>手牌分解器：枚举所有有效的面子+雀头排列。使用递归回溯（移植自 agari 的 hand.rs）。</summary>
public static class Decomposer
{
    /// <summary>分解门清手牌。支持标准形（4面子+1雀头）、七对子、国士无双。</summary>
    public static List<HandStructure> Decompose(TileSet tiles)
    {
        var results = new List<HandStructure>();
        int total = tiles.TotalCount;
        if (total is not (13 or 14)) return results;
        var kokushi = TryKokushi(tiles, total); if (kokushi != null) results.Add(kokushi);
        if (total == 14) { var chiitoi = TryChiitoitsu(tiles); if (chiitoi != null) results.Add(chiitoi); }
        DecomposeStandard(tiles, results);
        Deduplicate(results);
        return results;
    }

    /// <summary>分解带副露的手牌（仅标准形）。</summary>
    public static List<HandStructure> DecomposeWithMelds(TileSet tiles, IReadOnlyList<Meld> calledMelds)
    {
        var results = new List<HandStructure>();
        int remainingMelds = 4 - calledMelds.Count;
        if (remainingMelds < 0 || remainingMelds > 4) return results;
        DecomposeStandardWithMelds(tiles, calledMelds, remainingMelds, results);
        Deduplicate(results);
        return results;
    }

    // --- 国士无双 ---
    private static readonly int[] KokushiTileIds = [0, 8, 9, 17, 18, 26, 27, 28, 29, 30, 31, 32, 33];
    private static Kokushi? TryKokushi(TileSet tiles, int totalCount)
    {
        if (totalCount != 14) return null;
        for (int i = 0; i < Tile.Count; i++) { if (tiles[i] > 0 && !new Tile((byte)i).IsKokushi) return null; }
        var present = new List<Tile>(); Tile? pair = null; int pairCount = 0;
        foreach (int id in KokushiTileIds)
        { int c = tiles[id]; if (c == 0 || c > 2) return null; if (c == 2) { pair = new Tile((byte)id); pairCount++; } present.Add(new Tile((byte)id)); }
        return (pair != null && pairCount == 1) ? new Kokushi(present, pair.Value) : null;
    }

    // --- 七对子 ---
    /// <summary>七对子要求恰好7种牌各2张（4张相同牌在七对子中无效）。</summary>
    private static Chiitoitsu? TryChiitoitsu(TileSet tiles)
    {
        var pairs = new List<Tile>(); int distinct = 0;
        for (int i = 0; i < Tile.Count; i++) { int c = tiles[i]; if (c == 0) continue; if (c != 2) return null; distinct++; pairs.Add(new Tile((byte)i)); }
        return distinct == 7 ? new Chiitoitsu(pairs) : null;
    }

    // --- 标准形分解 ---
    private static void DecomposeStandard(TileSet tiles, List<HandStructure> results)
    {
        for (int i = 0; i < Tile.Count; i++)
        {
            if (tiles[i] < 2) continue;
            var remaining = tiles.Copy(); remaining[i] -= 2;
            var combos = new List<List<Meld>>();
            FindMeldCombinations(remaining, 4, [], combos);
            foreach (var melds in combos) if (melds.Count == 4) results.Add(new Standard(melds, new Tile((byte)i)));
        }
    }

    private static void DecomposeStandardWithMelds(TileSet tiles, IReadOnlyList<Meld> calledMelds, int remainingMelds, List<HandStructure> results)
    {
        for (int i = 0; i < Tile.Count; i++)
        {
            if (tiles[i] < 2) continue;
            var remaining = tiles.Copy(); remaining[i] -= 2;
            var combos = new List<List<Meld>>();
            FindMeldCombinations(remaining, remainingMelds, [], combos);
            foreach (var hm in combos) { if (hm.Count == remainingMelds) { var all = new List<Meld>(calledMelds); all.AddRange(hm); results.Add(new Standard(all, new Tile((byte)i))); } }
        }
    }

    /// <summary>递归回溯：找到所有从牌中形成 need 个面子的方式。选择最低牌，尝试刻子/顺子，孤立牌（count=1且无法形成任何面子）正确拒绝。</summary>
    private static void FindMeldCombinations(TileSet tiles, int needed, List<Meld> current, List<List<Meld>> results)
    {
        if (needed == 0) { if (tiles.TotalCount == 0) results.Add([..current]); return; }
        int first = -1; for (int i = 0; i < Tile.Count; i++) if (tiles[i] > 0) { first = i; break; }
        if (first < 0) return;
        var ft = new Tile((byte)first);
        bool canKoutsu = tiles[first] >= 3;
        bool canShuntsu = ft.IsSuited && ft.Number <= 7 && tiles[first + 1] > 0 && tiles[first + 2] > 0;
        if (!canKoutsu && !canShuntsu) return; // 孤立牌，此雀头选择无效
        if (canKoutsu) { tiles[first] -= 3; current.Add(new Koutsu(ft)); FindMeldCombinations(tiles, needed - 1, current, results); current.RemoveAt(current.Count - 1); tiles[first] += 3; }
        if (canShuntsu) { tiles[first]--; tiles[first + 1]--; tiles[first + 2]--; current.Add(new Shuntsu(ft)); FindMeldCombinations(tiles, needed - 1, current, results); current.RemoveAt(current.Count - 1); tiles[first]++; tiles[first + 1]++; tiles[first + 2]++; }
    }

    private static void Deduplicate(List<HandStructure> results)
    {
        var seen = new HashSet<string>();
        for (int i = results.Count - 1; i >= 0; i--) if (!seen.Add(StructureKey(results[i]))) results.RemoveAt(i);
    }

    private static string StructureKey(HandStructure s) => s switch
    {
        Standard std => $"S:{std.Pair.Id}:{string.Join(",", std.Melds.Select(MeldKey).OrderBy(x => x))}",
        Chiitoitsu ch => $"C:{string.Join(",", ch.Pairs.Select(p => p.Id).OrderBy(x => x))}",
        Kokushi ko => $"K:{ko.Pair.Id}", _ => "?"
    };
    private static string MeldKey(Meld m) => m switch
    {
        Shuntsu s => $"S{s.Tile.Id}:{(s.IsOpen ? "O" : "C")}",
        Koutsu k => $"K{k.Tile.Id}:{(k.IsOpen ? "O" : "C")}",
        Kan ka => $"Ka{ka.Tile.Id}:{(int)ka.KanType}", _ => "?"
    };
}
