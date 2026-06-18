namespace RiichiSharp.Rules;

/// <summary>可配置的役种判定规则。参考 tempai-core 的 yaku.Rules 接口。</summary>
public interface IYakuRules
{
    /// <summary>是否承认副露断幺九。</summary>
    bool OpenTanyao { get; }

    /// <summary>检查某张牌是否为赤宝牌（红五）。</summary>
    bool IsAkaDora(Tiles.Tile tile);

    /// <summary>人和的翻数限制：None（不承认）、Mangan、Haneman……或 Yakuman。</summary>
    ScoreLimit Renhou { get; }

    /// <summary>海底捞月是否要求牌山仍有存活牌。</summary>
    bool HaiteiFromLiveOnly { get; }

    /// <summary>是否启用里宝牌。</summary>
    bool Ura { get; }

    /// <summary>是否启用一发。</summary>
    bool Ippatsu { get; }

    /// <summary>绿一色是否要求全部牌均为绿色。</summary>
    bool GreenRequired { get; }

    /// <summary>岭上开花是否计算自摸符（+2符）。</summary>
    bool RinshanFu { get; }
}

/// <summary>可配置的点数计算规则。参考 tempai-core 的 score.Rules 接口。</summary>
public interface IScoreRules
{
    /// <summary>是否启用切上满贯（4翻30符→满贯）。</summary>
    bool ManganRound { get; }

    /// <summary>是否启用数え役满（13翻以上计为役满）。</summary>
    bool KazoeYakuman { get; }

    /// <summary>是否允许多种役满叠加。</summary>
    bool YakumanSum { get; }

    /// <summary>指定的役满类型是否计为双倍役满。</summary>
    bool IsDoubleYakuman(Yaku.Yakuman yakuman);

    /// <summary>每本场的点数（通常100或300）。</summary>
    int Honba { get; }
}

/// <summary>分数等级/役满级别。</summary>
public enum ScoreLimit
{
    None = 0,
    Mangan = 1,      // 满贯
    Haneman = 2,     // 跳满
    Baiman = 3,      // 倍满
    Sanbaiman = 4,   // 三倍满
    Yakuman = 5      // 役满
}
