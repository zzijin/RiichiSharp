namespace RiichiSharp.Hand;

using RiichiSharp.Tiles;

/// <summary>面子（メンツ）。手牌中的一组3或4张牌。</summary>
public abstract record Meld
{
    public bool IsOpen { get; init; }
    public bool IsRon { get; init; }
    public abstract IReadOnlyList<Tile> Tiles { get; }
    public abstract Tile Tile { get; }
    public bool IsShuntsu => this is Shuntsu;
    public bool IsKoutsu => this is Koutsu;
    public bool IsKan => this is Kan;
    public abstract Meld WithOpen();
    public abstract Meld WithRon();
}

/// <summary>顺子（シュンツ）。同花色连续三张牌。</summary>
public sealed record Shuntsu : Meld
{
    public override Tile Tile { get; }
    public override IReadOnlyList<Tile> Tiles { get; }
    public Shuntsu(Tile startTile, bool isOpen = false, bool isRon = false)
    {
        if (!startTile.IsSuited || startTile.Number > 7) throw new ArgumentException($"无效顺子起始牌: {startTile}");
        Tile = startTile;
        Tiles = [startTile, new Tile((byte)(startTile.Id + 1)), new Tile((byte)(startTile.Id + 2))];
        IsOpen = isOpen; IsRon = isRon;
    }
    public override Meld WithOpen() => this with { IsOpen = true };
    public override Meld WithRon() => this with { IsRon = true };
}

/// <summary>刻子（コーツ）。三张相同牌。</summary>
public sealed record Koutsu : Meld
{
    public override Tile Tile { get; }
    public override IReadOnlyList<Tile> Tiles { get; }
    public Koutsu(Tile tile, bool isOpen = false, bool isRon = false)
    {
        Tile = tile; Tiles = [tile, tile, tile]; IsOpen = isOpen; IsRon = isRon;
    }
    public override Meld WithOpen() => this with { IsOpen = true };
    public override Meld WithRon() => this with { IsRon = true };
}

/// <summary>杠子（カンツ）。四张相同牌。</summary>
public sealed record Kan : Meld
{
    public KanType KanType { get; init; } = KanType.Closed;
    public override Tile Tile { get; }
    public override IReadOnlyList<Tile> Tiles { get; }
    public Kan(Tile tile, KanType kanType = KanType.Closed, bool isOpen = false, bool isRon = false)
    {
        Tile = tile; Tiles = [tile, tile, tile, tile]; KanType = kanType;
        IsOpen = kanType != KanType.Closed; IsRon = isRon;
    }
    public override Meld WithOpen() => this with { IsOpen = true };
    public override Meld WithRon() => this with { IsRon = true };
}

/// <summary>杠的类型</summary>
public enum KanType
{
    Closed,  // 暗杠（アンカン）
    Open,    // 明杠（ダイミンカン）
    Added    // 加杠（ショウミンカン）
}
