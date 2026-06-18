namespace RiichiSharp.Shanten;

using RiichiSharp.Tiles;

/// <summary>进张（受け入れ）计算：哪些牌能改良手牌的向听数。</summary>
public static class UkeireCalculator
{
    public record UkeireTile { public Tile Tile { get; init; } public int Available { get; init; } }
    public record UkeireResult { public int Shanten { get; init; } public List<UkeireTile> Tiles { get; set; } = []; public int TotalCount => Tiles.Sum(t => t.Available); }

    /// <summary>理论进张（全136张牌山，仅扣除手牌已有牌）。</summary>
    public static UkeireResult Calculate(TileSet tiles, int calledMelds = 0)
    {
        int s = ShantenCalculator.Calculate(tiles, calledMelds).Shanten;
        return CalculateUkeire(tiles, null, calledMelds, s);
    }

    /// <summary>实际进张（扣除桌上可见牌）。</summary>
    public static UkeireResult CalculateWithVisible(TileSet tiles, TileSet visible, int calledMelds = 0)
    {
        int s = ShantenCalculator.Calculate(tiles, calledMelds).Shanten;
        return CalculateUkeire(tiles, visible, calledMelds, s);
    }

    private static UkeireResult CalculateUkeire(TileSet tiles, TileSet? visible, int calledMelds, int currentShanten)
    {
        var result = new UkeireResult { Shanten = currentShanten };
        if (currentShanten <= -1) return result; // 已和了，无需进张
        var improving = new List<UkeireTile>();
        for (int i = 0; i < Tile.Count; i++)
        {
            if (tiles[i] >= 4) continue;
            int[] counts = tiles.ToArray(); counts[i]++;
            var testHand = TileSet.FromArray(counts);
            if (ShantenCalculator.Calculate(testHand, calledMelds).Shanten < currentShanten)
            {
                int available = 4 - tiles[i];
                if (visible != null) available -= Math.Min(visible.Value[i], available);
                available = Math.Max(0, available);
                if (available > 0) improving.Add(new UkeireTile { Tile = new Tile((byte)i), Available = available });
            }
        }
        result.Tiles = improving;
        return result;
    }
}
