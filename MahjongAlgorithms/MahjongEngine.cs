using MahjongAlgorithms.Hand;
using MahjongAlgorithms.Parse;
using MahjongAlgorithms.Scoring;
using MahjongAlgorithms.Shanten;
using MahjongAlgorithms.Tiles;
using MahjongAlgorithms.Wait;
using MahjongAlgorithms.Yaku;

namespace MahjongAlgorithms;

/// <summary>顶层便捷 API。提供一行调用的计分、向听、进张分析。</summary>
public static class MahjongEngine
{
    /// <summary>计分：从手牌字符串和游戏上下文计算最优得分。</summary>
    public static ScoreResult Score(string handStr, GameContext? context = null)
    {
        context ??= new GameContext();
        var parsed = TenhouParser.ParseWithMelds(handStr) ?? throw new ArgumentException($"无法解析手牌: {handStr}");
        var tiles = parsed.Tiles;
        var calledMelds = parsed.CalledMelds.Select(cm =>
        {
            Meld? meld = null;
            byte firstId = 0;
            foreach (var (tile, _) in cm.Tiles.Tiles) { firstId = tile.Id; break; }
            if (cm.Type == MeldType.Chi) meld = new Shuntsu(new Tile(firstId), isOpen: !cm.IsClosed);
            else if (cm.IsKan) meld = new Kan(new Tile(firstId), cm.IsClosed ? KanType.Closed : KanType.Open);
            else meld = new Koutsu(new Tile(firstId), isOpen: !cm.IsClosed);
            return meld;
        }).ToList();

        var ctx = context with { IsOpen = calledMelds.Count > 0 || context.IsOpen };
        var structures = calledMelds.Count > 0
            ? Decomposer.DecomposeWithMelds(tiles, calledMelds)
            : Decomposer.Decompose(tiles);

        ScoreResult? best = null;
        foreach (var s in structures)
        {
            var waits = WaitDetector.Detect(s, ctx.WinningTile);
            var yaku = YakuDetector.Detect(s, tiles, ctx, waits);
            var fu = FuCalculator.Calculate(s, ctx, waits);
            var level = ScoreCalculator.DetermineLevel(yaku.TotalHan, fu.Total, yaku.HasYakuman, yaku.YakumanUnits, ctx.ScoreRules.KazoeYakuman, ctx.ScoreRules.ManganRound);
            var basic = ScoreCalculator.CalculateBasicPoints(yaku.TotalHan, fu.Total, level, ctx.ScoreRules.ManganRound);
            var pay = ScoreCalculator.CalculatePayment(basic, ctx.IsDealer, ctx.IsTsumo, ctx.Honba);
            var r = new ScoreResult { Structure = s, Yaku = yaku, Fu = fu, ScoreLevel = level, Payment = pay, HandStr = handStr };
            if (best == null || pay.Total > best.Payment.Total || (pay.Total == best.Payment.Total && yaku.TotalHan > best.Yaku.TotalHan)) best = r;
        }
        return best ?? throw new InvalidOperationException("无法找到有效的手牌分解");
    }

    /// <summary>向听数（-1=和了, 0=听牌, 1=一向听……）。</summary>
    public static ShantenCalculator.ShantenResult Shanten(string handStr, int calledMelds = 0)
    {
        var tiles = TenhouParser.Parse(handStr) ?? throw new ArgumentException($"无法解析手牌: {handStr}");
        return ShantenCalculator.Calculate(tiles, calledMelds);
    }

    /// <summary>进张分析：哪些牌能改良手牌，以及每种牌的剩余枚数。</summary>
    public static UkeireCalculator.UkeireResult Ukeire(string handStr, string? visibleStr = null, int calledMelds = 0)
    {
        var tiles = TenhouParser.Parse(handStr) ?? throw new ArgumentException($"无法解析手牌: {handStr}");
        if (visibleStr != null) { var v = TenhouParser.Parse(visibleStr); if (v != null) return UkeireCalculator.CalculateWithVisible(tiles, (TileSet)v, calledMelds); }
        return UkeireCalculator.Calculate(tiles, calledMelds);
    }

    /// <summary>切牌效率分析：每种切牌后的向听数和进张，按最优切牌排序。</summary>
    public static List<Effective.EffectiveDiscard.DiscardResult> EffectiveDiscards(string handStr, int calledMelds = 0)
    {
        var tiles = TenhouParser.Parse(handStr) ?? throw new ArgumentException($"无法解析手牌: {handStr}");
        return Effective.EffectiveDiscard.Calculate(tiles, calledMelds);
    }
}

/// <summary>完整计分结果。</summary>
public record ScoreResult
{
    public HandStructure Structure { get; init; } = null!;
    public YakuDetector.YakuResult Yaku { get; init; } = null!;
    public FuCalculator.FuResult Fu { get; init; } = null!;
    public ScoreLevel ScoreLevel { get; init; } = null!;
    public Payment Payment { get; init; } = null!;
    public string HandStr { get; init; } = "";
    public int Han => Yaku.TotalHan;
    public int FuTotal => Fu.Total;
    public int Points => Payment.Total;
    public bool IsYakuman => Yaku.HasYakuman;
    public IReadOnlyList<Yaku.Yaku> YakuList => Yaku.YakuHan.Keys.ToList();
    public IReadOnlyList<Yaku.Yakuman> YakumanList => Yaku.Yakumans;
    public override string ToString()
    {
        if (IsYakuman) { var names = string.Join(", ", YakumanList); return $"{HandStr} → {names} ({Points}点)"; }
        var yn = string.Join(", ", YakuList); return $"{HandStr} → {Han}翻{FuTotal}符: {yn} ({Points}点)";
    }
}
