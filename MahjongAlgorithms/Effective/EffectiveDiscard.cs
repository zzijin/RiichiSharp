namespace MahjongAlgorithms.Effective;

using MahjongAlgorithms.Shanten;
using MahjongAlgorithms.Tiles;

/// <summary>切牌效率分析：手牌中的每种牌切出后的向听数和进张，按最优切牌排序。移植自 tempai-core 的 effective.go。</summary>
public static class EffectiveDiscard
{
    public record DiscardResult
    {
        public Tile Discarded { get; init; }
        public int ShantenAfter { get; init; }
        public int UniqueUkeire { get; init; }
        public int TotalUkeire { get; init; }
        public List<UkeireCalculator.UkeireTile> UkeireTiles { get; init; } = [];
    }

    /// <summary>对手牌中每种牌，移除后计算向听数和进张。按最优切牌排序（向听数低→进张多→牌优先级）。</summary>
    public static List<DiscardResult> Calculate(TileSet tiles, int calledMelds = 0)
    {
        var results = new List<DiscardResult>();
        for (int i = 0; i < Tile.Count; i++)
        {
            if (tiles[i] == 0) continue;
            int[] reduced = tiles.ToArray(); reduced[i]--;
            var testHand = TileSet.FromArray(reduced);
            var shanten = ShantenCalculator.Calculate(testHand, calledMelds);
            var ukeire = UkeireCalculator.Calculate(testHand, calledMelds);
            results.Add(new DiscardResult { Discarded = new Tile((byte)i), ShantenAfter = shanten.Shanten, UniqueUkeire = ukeire.Tiles.Count, TotalUkeire = ukeire.TotalCount, UkeireTiles = ukeire.Tiles });
        }
        results.Sort((a, b) =>
        {
            int cmp = a.ShantenAfter.CompareTo(b.ShantenAfter); if (cmp != 0) return cmp;
            cmp = b.UniqueUkeire.CompareTo(a.UniqueUkeire); if (cmp != 0) return cmp;
            cmp = b.TotalUkeire.CompareTo(a.TotalUkeire); if (cmp != 0) return cmp;
            return TilePriority(a.Discarded).CompareTo(TilePriority(b.Discarded));
        });
        return results;
    }

    /// <summary>牌优先级排序：字牌 < 幺九 < 2/8 < 3/7 < 4/6 < 5。数值越高越应保留。</summary>
    private static int TilePriority(Tile tile)
        => tile.IsHonor ? 0 : tile.Number switch { 1 or 9 => 1, 2 or 8 => 2, 3 or 7 => 3, 4 or 6 => 5, 5 => 6, _ => 0 };
}
