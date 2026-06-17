namespace MahjongAlgorithms.Scoring;

using MahjongAlgorithms.Hand;
using MahjongAlgorithms.Tiles;
using MahjongAlgorithms.Wait;

/// <summary>符计算。移植自 agari 的 scoring.rs。</summary>
public static class FuCalculator
{
    public record FuResult { public int Total { get; init; } public int Raw { get; init; } public FuBreakdown Breakdown { get; init; } = new(); }
    public record FuBreakdown { public int Base { get; set; } = 20; public int MenzenRon { get; set; } public int Tsumo { get; set; } public int Melds { get; set; } public int Pair { get; set; } public int Wait { get; set; } public int RawTotal => Base + MenzenRon + Tsumo + Melds + Pair + Wait; }

    /// <summary>计算手牌结构的符数。</summary>
    public static FuResult Calculate(HandStructure structure, GameContext context, List<WaitType>? waitTypes = null)
        => structure switch
        {
            Chiitoitsu => new FuResult { Total = 25, Raw = 25 },
            Kokushi => new FuResult { Total = 30, Raw = 30 },
            Standard s => CalculateStandard(s, context, waitTypes),
            _ => new FuResult { Total = 20, Raw = 20 }
        };

    private static FuResult CalculateStandard(Standard std, GameContext context, List<WaitType>? waitTypes = null)
    {
        var breakdown = new FuBreakdown();
        bool isOpen = context.IsOpen;

        // 平和+自摸 = 恰好20符（不进位）
        bool isPinfu = waitTypes?.Contains(WaitType.Ryanmen) == true && !isOpen
            && std.Melds.All(m => m.IsShuntsu)
            && !WaitDetector.IsYakuhaiPair(std.Pair, context.RoundWind, context.SeatWind);
        if (isPinfu && context.IsTsumo)
            return new FuResult { Total = 20, Raw = 20, Breakdown = new FuBreakdown { Base = 20 } };

        breakdown.Base = 20;                                     // 副底 20符
        if (!isOpen && context.IsRon) breakdown.MenzenRon = 10;  // 门清荣和 +10符
        if (context.IsTsumo && !isPinfu) breakdown.Tsumo = 2;     // 自摸 +2符（平和除外）
        breakdown.Melds = CalculateMeldFu(std, context);          // 面子符
        breakdown.Pair = CalculatePairFu(std.Pair, context.RoundWind, context.SeatWind); // 雀头符
        waitTypes ??= WaitDetector.Detect(std, context.WinningTile);
        breakdown.Wait = WaitFu.GetFu(WaitDetector.BestForScoring(waitTypes)); // 听牌符

        int raw = breakdown.RawTotal;
        int total = raw;
        if (raw % 10 != 0) total = ((raw / 10) + 1) * 10;       // 进位到十位
        if (isOpen && total < 30) total = 30;                     // 副露最低30符
        return new FuResult { Total = total, Raw = raw, Breakdown = breakdown };
    }

    /// <summary>面子符。处理延单骑（裸单骑）边缘情况：荣和在双碰听牌上完成刻子时，若和了牌同时能完成门清顺子，则刻子保持暗刻（高符数）。</summary>
    private static int CalculateMeldFu(Standard std, GameContext context)
    {
        int total = 0;
        foreach (var meld in std.Melds)
            total += meld switch { Shuntsu => 0, Koutsu k => CalculateKoutsuFu(k, std, context), Kan ka => CalculateKanFu(ka), _ => 0 };
        return total;
    }

    private static int CalculateKoutsuFu(Koutsu k, Standard std, GameContext context)
    {
        bool isSimple = k.Tile.IsSimple;
        bool isOpen = k.IsOpen;
        // 荣和双碰：荣和完成的刻子视为"明刻"（低符数），除非和了牌也能完成门清顺子（延单骑例外）
        if (k.IsRon && !WaitDetector.WinningTileInClosedSequence(std, context.WinningTile))
            isOpen = true;
        int baseFu = isSimple ? 2 : 4;    // 中张2符 / 幺九4符
        if (!isOpen) baseFu *= 2;         // 暗刻翻倍
        return baseFu;
    }

    private static int CalculateKanFu(Kan ka)
    {
        bool isSimple = ka.Tile.IsSimple;
        bool isOpen = ka.KanType != KanType.Closed;
        int baseFu = isSimple ? 8 : 16;    // 杠子 = 刻子×4
        if (!isOpen) baseFu *= 2;          // 暗杠翻倍
        return baseFu;
    }

    /// <summary>雀头符：三元牌=2，场风/自风各=2（双重风=4）。</summary>
    public static int CalculatePairFu(Tile pair, Tile roundWind, Tile seatWind)
    {
        int fu = 0;
        if (pair.IsDragon) fu += 2;
        if (pair.Id == roundWind.Id) fu += 2;
        if (pair.Id == seatWind.Id) fu += 2;
        return fu;
    }
}
