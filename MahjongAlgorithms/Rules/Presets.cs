namespace MahjongAlgorithms.Rules;

using MahjongAlgorithms.Tiles;
using MahjongAlgorithms.Yaku;

/// <summary>EMA（欧洲麻将协会）标准规则。</summary>
public class RulesEMA : IYakuRules, IScoreRules
{
    public bool OpenTanyao => true;
    public bool IsAkaDora(Tile tile) => false;
    public ScoreLimit Renhou => ScoreLimit.Mangan;
    public bool HaiteiFromLiveOnly => false;
    public bool Ura => true;
    public bool Ippatsu => true;
    public bool GreenRequired => false;
    public bool RinshanFu => true;

    public bool ManganRound => false;
    public bool KazoeYakuman => false;
    public bool YakumanSum => true;
    public bool IsDoubleYakuman(Yakuman yakuman) => DefaultDoubleYakumans.Contains(yakuman);
    public int Honba => 100;

    public static readonly HashSet<Yakuman> DefaultDoubleYakumans = [
        Yakuman.KokushiMusou13Wait,
        Yakuman.SuuankouTanki,
        Yakuman.Daisuushi,
        Yakuman.JunseiChuurenPoutou
    ];
}

/// <summary>天凤规则（含赤宝牌）。</summary>
public class RulesTenhou : IYakuRules, IScoreRules
{
    public bool OpenTanyao => true;
    public bool IsAkaDora(Tile tile) =>
        (tile.Id == Tile.M5.Id || tile.Id == Tile.P5.Id || tile.Id == Tile.S5.Id);
    public ScoreLimit Renhou => ScoreLimit.None;
    public bool HaiteiFromLiveOnly => false;
    public bool Ura => true;
    public bool Ippatsu => true;
    public bool GreenRequired => false;
    public bool RinshanFu => false;

    public bool ManganRound => false;
    public bool KazoeYakuman => true;
    public bool YakumanSum => true;
    public bool IsDoubleYakuman(Yakuman yakuman) => RulesEMA.DefaultDoubleYakumans.Contains(yakuman);
    public int Honba => 300;
}

/// <summary>JPML-A（日本职业麻将联盟·A规则）。较严格：无一发、无里宝牌、绿一色必须全绿。</summary>
public class RulesJPMLA : IYakuRules, IScoreRules
{
    public bool OpenTanyao => true;
    public bool IsAkaDora(Tile tile) => false;
    public ScoreLimit Renhou => ScoreLimit.Mangan;
    public bool HaiteiFromLiveOnly => false;
    public bool Ura => false;
    public bool Ippatsu => false;
    public bool GreenRequired => true;
    public bool RinshanFu => false;

    public bool ManganRound => false;
    public bool KazoeYakuman => false;
    public bool YakumanSum => true;
    public bool IsDoubleYakuman(Yakuman yakuman) => RulesEMA.DefaultDoubleYakumans.Contains(yakuman);
    public int Honba => 100;
}

/// <summary>JPML-B 规则。计分上与 EMA 相同，但启用切上满贯。</summary>
public class RulesJPMLB : RulesEMA
{
    public new bool ManganRound => true;
}
