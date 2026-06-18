namespace RiichiSharp;

using RiichiSharp.Tiles;
using RiichiSharp.Rules;

/// <summary>
/// 对局上下文：计分所需的全部局面信息。
/// 使用 Builder 模式通过 with 表达式链式构建。
/// </summary>
public record GameContext
{
    /// <summary>和了方式（荣和/自摸）</summary>
    public WinType WinType { get; init; } = WinType.Ron;

    /// <summary>场风（东=庄家）</summary>
    public Tile RoundWind { get; init; } = Tile.East;

    /// <summary>自风</summary>
    public Tile SeatWind { get; init; } = Tile.East;

    /// <summary>和了牌</summary>
    public Tile WinningTile { get; init; }

    /// <summary>是否副露（有鸣牌）</summary>
    public bool IsOpen { get; init; }

    /// <summary>是否立直</summary>
    public bool IsRiichi { get; init; }

    /// <summary>是否两立直（第一巡立直）</summary>
    public bool IsDoubleRiichi { get; init; }

    /// <summary>是否一发（立直后一巡内和了）</summary>
    public bool IsIppatsu { get; init; }

    /// <summary>是否岭上开花（杠后摸牌和了）</summary>
    public bool IsRinshan { get; init; }

    /// <summary>是否抢杠（抢他人加杠和了）</summary>
    public bool IsChankan { get; init; }

    /// <summary>是否海底/河底（最后一张牌和了）</summary>
    public bool IsLastTile { get; init; }

    /// <summary>是否天和（庄家第一巡自摸和了）</summary>
    public bool IsTenhou { get; init; }

    /// <summary>是否地和（闲家第一巡自摸和了）</summary>
    public bool IsChiihou { get; init; }

    /// <summary>宝牌指示牌</summary>
    public List<Tile> DoraIndicators { get; init; } = [];

    /// <summary>里宝牌指示牌（仅立直时有效）</summary>
    public List<Tile> UraDoraIndicators { get; init; } = [];

    /// <summary>手牌中赤宝牌（红五）的数量</summary>
    public int AkaCount { get; init; }

    /// <summary>本场数（连庄计数器）</summary>
    public int Honba { get; init; }

    /// <summary>役种判定规则</summary>
    public IYakuRules YakuRules { get; init; } = new RulesEMA();

    /// <summary>点数计算规则</summary>
    public IScoreRules ScoreRules { get; init; } = new RulesEMA();

    /// <summary>桌上可见牌（用于实际进张计算）</summary>
    public TileSet? VisibleTiles { get; init; }

    // Builder 方法
    public GameContext WithWinType(WinType type) => this with { WinType = type };
    public GameContext WithWinds(Tile round, Tile seat) => this with { RoundWind = round, SeatWind = seat };
    public GameContext WithWinningTile(Tile tile) => this with { WinningTile = tile };
    public GameContext Open() => this with { IsOpen = true };
    public GameContext Riichi() => this with { IsRiichi = true };
    public GameContext DoubleRiichi() => this with { IsDoubleRiichi = true };
    public GameContext Ippatsu() => this with { IsIppatsu = true };
    public GameContext Rinshan() => this with { IsRinshan = true };
    public GameContext Chankan() => this with { IsChankan = true };
    public GameContext LastTile() => this with { IsLastTile = true };
    public GameContext Tenhou() => this with { IsTenhou = true };
    public GameContext Chiihou() => this with { IsChiihou = true };
    public GameContext WithDora(List<Tile> indicators) => this with { DoraIndicators = indicators };
    public GameContext WithUraDora(List<Tile> indicators) => this with { UraDoraIndicators = indicators };
    public GameContext WithAka(int count) => this with { AkaCount = count };
    public GameContext WithHonba(int count) => this with { Honba = count };
    public GameContext WithYakuRules(IYakuRules rules) => this with { YakuRules = rules };
    public GameContext WithScoreRules(IScoreRules rules) => this with { ScoreRules = rules };
    public GameContext WithVisibleTiles(TileSet visible) => this with { VisibleTiles = visible };

    /// <summary>是否庄家（东家）</summary>
    public bool IsDealer => SeatWind.Id == Tile.East.Id;

    /// <summary>是否自摸和了</summary>
    public bool IsTsumo => WinType == WinType.Tsumo;

    /// <summary>是否荣和</summary>
    public bool IsRon => WinType == WinType.Ron;
}

public enum WinType
{
    Ron,    // 荣和
    Tsumo   // 自摸
}
