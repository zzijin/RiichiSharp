namespace RiichiSharp.Hand;

using RiichiSharp.Tiles;

/// <summary>完整手牌分解（面子和雀头）。</summary>
public abstract record HandStructure
{
    public abstract IReadOnlyList<Meld> Melds { get; }
    public abstract Tile Pair { get; }
    public bool IsStandard => this is Standard;
    public bool IsChiitoitsu => this is Chiitoitsu;
    public bool IsKokushi => this is Kokushi;
    /// <summary>是否门清（全部面子未副露）</summary>
    public bool IsMenzen => Melds.All(m => !m.IsOpen);
    public int ClosedTripletCount => Melds.Count(m => m is Koutsu && !m.IsOpen);
    public int ClosedKanCount => Melds.Count(m => m is Kan k && k.KanType == KanType.Closed);
    public int OpenTripletCount => Melds.Count(m => m is Koutsu && m.IsOpen);
    public int OpenKanCount => Melds.Count(m => m is Kan k && k.KanType != KanType.Closed);
}

/// <summary>标准形：4面子+1雀头。</summary>
public sealed record Standard : HandStructure
{
    public override IReadOnlyList<Meld> Melds { get; }
    public override Tile Pair { get; }
    public Standard(IEnumerable<Meld> melds, Tile pair) { Melds = melds.ToList().AsReadOnly(); Pair = pair; }
}

/// <summary>七对子（チートイツ）：7个对子。</summary>
public sealed record Chiitoitsu : HandStructure
{
    public override IReadOnlyList<Meld> Melds { get; }
    public override Tile Pair { get; }
    public IReadOnlyList<Tile> Pairs { get; }
    public Chiitoitsu(IEnumerable<Tile> pairs) { var list = pairs.ToList(); Pairs = list.AsReadOnly(); Pair = list[0]; Melds = []; }
}

/// <summary>国士无双（コクシムソウ）：13种幺九牌+1对。</summary>
public sealed record Kokushi : HandStructure
{
    public override IReadOnlyList<Meld> Melds => [];
    public override Tile Pair { get; }
    public IReadOnlyList<Tile> Tiles { get; }
    public Kokushi(IEnumerable<Tile> tiles, Tile pair) { Tiles = tiles.ToList().AsReadOnly(); Pair = pair; }
}
