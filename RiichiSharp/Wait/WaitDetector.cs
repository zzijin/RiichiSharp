namespace RiichiSharp.Wait;

using RiichiSharp.Hand;
using RiichiSharp.Tiles;

/// <summary>听牌类型检测 + 平和验证。移植自 agari 的 wait.rs。</summary>
public static class WaitDetector
{
    /// <summary>检测手牌结构的所有听牌类型。</summary>
    public static List<WaitType> Detect(HandStructure structure, Tile winningTile) => structure switch
    {
        Chiitoitsu => new List<WaitType> { WaitType.Tanki },
        Kokushi k => DetectKokushiWait(k, winningTile),
        Standard s => DetectStandardWait(s, winningTile),
        _ => []
    };

    /// <summary>获取计分最优的听牌（符数最高优先）。</summary>
    public static WaitType BestForScoring(List<WaitType> waits) => waits.Count > 0 ? waits.MaxBy(WaitFu.GetFu) : WaitType.Tanki;

    /// <summary>获取平和最优的听牌（两面优先）。</summary>
    public static WaitType BestForPinfu(List<WaitType> waits)
    {
        if (waits.Contains(WaitType.Ryanmen)) return WaitType.Ryanmen;
        if (waits.Contains(WaitType.Shanpon)) return WaitType.Shanpon;
        return waits[0];
    }

    /// <summary>严格平和判定（4条件）：1.门清 2.全顺子 3.非役牌雀头 4.两面听。</summary>
    public static bool IsPinfu(HandStructure structure, Tile winningTile, GameContext context, List<WaitType>? waits = null)
    {
        if (structure is not Standard std) return false;
        if (context.IsOpen) return false;
        if (std.Melds.Any(m => !m.IsShuntsu)) return false;
        if (IsYakuhaiPair(std.Pair, context.RoundWind, context.SeatWind)) return false;
        waits ??= Detect(structure, winningTile);
        return waits.Contains(WaitType.Ryanmen);
    }

    /// <summary>检查雀头是否为役牌（三元牌/场风/自风）。</summary>
    public static bool IsYakuhaiPair(Tile pair, Tile roundWind, Tile seatWind)
        => pair.IsDragon || pair.Id == roundWind.Id || pair.Id == seatWind.Id;

    /// <summary>延单骑（裸单骑）检测：荣和完成的刻子中，和了牌是否也能完成门清顺子。
    /// 示例: 11123m 听 1m/4m，荣和 1m 时 111m 保持暗刻（8符而非4符）。</summary>
    public static bool WinningTileInClosedSequence(Standard structure, Tile winningTile)
    {
        foreach (var m in structure.Melds)
            if (m is Shuntsu s && !s.IsOpen && winningTile.Id >= s.Tile.Id && winningTile.Id <= s.Tile.Id + 2)
                return true;
        return false;
    }

    private static List<WaitType> DetectKokushiWait(Kokushi k, Tile winningTile)
    {
        var w = winningTile.Id == k.Pair.Id ? WaitType.Tanki : WaitType.Kokushi13;
        return [w];
    }

    private static List<WaitType> DetectStandardWait(Standard std, Tile winningTile)
    {
        var waits = new List<WaitType>();
        if (winningTile.Id == std.Pair.Id) waits.Add(WaitType.Tanki);           // 单骑：和了牌=雀头
        foreach (var m in std.Melds)
        {
            if (m is Koutsu k && !k.IsOpen && winningTile.Id == k.Tile.Id)
                waits.Add(WaitType.Shanpon);                                     // 双碰：和了牌完成刻子
        }
        foreach (var m in std.Melds)
            if (m is Shuntsu s) { var w = DetectSequenceWait(s, winningTile); if (w != null) waits.Add(w.Value); }
        return waits;
    }

    private static WaitType? DetectSequenceWait(Shuntsu s, Tile winningTile)
    {
        int start = s.Tile.Number, win = winningTile.Number;
        if (s.Tile.Suit != winningTile.Suit) return null;
        if (win == start) return (start + 2 == 9) ? WaitType.Penchan : WaitType.Ryanmen;   // 低端: 边张或两面
        if (win == start + 1) return WaitType.Kanchan;                                       // 中端: 嵌张
        if (win == start + 2) return (start == 1) ? WaitType.Penchan : WaitType.Ryanmen;     // 高端: 边张或两面
        return null;
    }
}
