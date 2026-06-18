namespace RiichiSharp.Tiles;

/// <summary>
/// 34元素固定数组牌面表示。下标: m1=0..z7=33。
/// 这是整个库使用的主要手牌表示形式。
/// </summary>
public record struct TileSet
{
    private readonly int[] _counts;

    public TileSet() { _counts = new int[Tile.Count]; }
    public TileSet(TileSet other) { _counts = (int[])other._counts.Clone(); }
    public TileSet Copy() => new(this);

    /// <summary>获取计数数组的副本（可安全修改）。</summary>
    public int[] ToArray() { var copy = new int[Tile.Count]; Array.Copy(_counts, copy, Tile.Count); return copy; }

    /// <summary>从计数数组创建（内部使用）。</summary>
    internal static TileSet FromArray(int[] counts) { var ts = new TileSet(); Array.Copy(counts, ts._counts, Tile.Count); return ts; }

    public int this[int tileId] { get => _counts[tileId]; set => _counts[tileId] = value; }
    public int this[Tile tile] { get => _counts[tile.Id]; set => _counts[tile.Id] = value; }

    public void Add(Tile tile, int count = 1) { _counts[tile.Id] += count; }
    public void Remove(Tile tile, int count = 1) { _counts[tile.Id] = Math.Max(0, _counts[tile.Id] - count); }
    public int CountOf(Tile tile) => _counts[tile.Id];

    /// <summary>总牌数</summary>
    public int TotalCount { get { int sum = 0; for (int i = 0; i < Tile.Count; i++) sum += _counts[i]; return sum; } }

    /// <summary>有牌的种类数（count>0 的牌类型数）</summary>
    public int UniqueCount { get { int c = 0; for (int i = 0; i < Tile.Count; i++) if (_counts[i] > 0) c++; return c; } }

    /// <summary>对子数（count>=2 的牌类型数）</summary>
    public int PairCount { get { int p = 0; for (int i = 0; i < Tile.Count; i++) p += _counts[i] / 2; return p; } }

    /// <summary>所有计数是否在合法范围（0-4）内</summary>
    public bool IsValid { get { for (int i = 0; i < Tile.Count; i++) if (_counts[i] < 0 || _counts[i] > 4) return false; return true; } }

    public void Each(Action<Tile, int> action)
    {
        for (int i = 0; i < Tile.Count; i++)
            if (_counts[i] > 0) action(new Tile((byte)i), _counts[i]);
    }

    public IEnumerable<(Tile Tile, int Count)> Tiles
    {
        get { for (int i = 0; i < Tile.Count; i++) if (_counts[i] > 0) yield return (new Tile((byte)i), _counts[i]); }
    }

    /// <summary>获取指定花色的切片。</summary>
    public ReadOnlySpan<int> GetSuitSlice(Suit suit) => suit switch
    {
        Suit.Manzu => _counts.AsSpan(0, 9),
        Suit.Pinzu => _counts.AsSpan(9, 9),
        Suit.Souzu => _counts.AsSpan(18, 9),
        Suit.Honor => _counts.AsSpan(27, 7),
        _ => throw new ArgumentOutOfRangeException(nameof(suit))
    };

    public override string ToString()
    {
        var parts = new List<string>();
        foreach (var (tile, count) in Tiles)
            for (int i = 0; i < count; i++) parts.Add(tile.ToString());
        return string.Join("", parts);
    }
}
