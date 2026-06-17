namespace MahjongAlgorithms.Tiles;

/// <summary>
/// 单张麻将牌。可以是数牌（1m-9s）或字牌（东南西北白发中）。
/// 支持赤宝牌（红五）标记。
/// </summary>
public readonly record struct Tile(byte Id)
{
    // --- 万子: 0-8 (1m-9m) ---
    public static readonly Tile M1 = new(0);
    public static readonly Tile M2 = new(1);
    public static readonly Tile M3 = new(2);
    public static readonly Tile M4 = new(3);
    public static readonly Tile M5 = new(4);
    public static readonly Tile M6 = new(5);
    public static readonly Tile M7 = new(6);
    public static readonly Tile M8 = new(7);
    public static readonly Tile M9 = new(8);

    // --- 筒子: 9-17 (1p-9p) ---
    public static readonly Tile P1 = new(9);
    public static readonly Tile P2 = new(10);
    public static readonly Tile P3 = new(11);
    public static readonly Tile P4 = new(12);
    public static readonly Tile P5 = new(13);
    public static readonly Tile P6 = new(14);
    public static readonly Tile P7 = new(15);
    public static readonly Tile P8 = new(16);
    public static readonly Tile P9 = new(17);

    // --- 索子: 18-26 (1s-9s) ---
    public static readonly Tile S1 = new(18);
    public static readonly Tile S2 = new(19);
    public static readonly Tile S3 = new(20);
    public static readonly Tile S4 = new(21);
    public static readonly Tile S5 = new(22);
    public static readonly Tile S6 = new(23);
    public static readonly Tile S7 = new(24);
    public static readonly Tile S8 = new(25);
    public static readonly Tile S9 = new(26);

    // --- 字牌: 27-33 (东南西北白发中) ---
    public static readonly Tile East = new(27);   // 东
    public static readonly Tile South = new(28);  // 南
    public static readonly Tile West = new(29);   // 西
    public static readonly Tile North = new(30);  // 北
    public static readonly Tile White = new(31);  // 白
    public static readonly Tile Green = new(32);  // 发
    public static readonly Tile Red = new(33);    // 中

    // 迭代用数组
    public static readonly Tile[] Manzu = [M1, M2, M3, M4, M5, M6, M7, M8, M9];
    public static readonly Tile[] Pinzu = [P1, P2, P3, P4, P5, P6, P7, P8, P9];
    public static readonly Tile[] Souzu = [S1, S2, S3, S4, S5, S6, S7, S8, S9];
    public static readonly Tile[] Honors = [East, South, West, North, White, Green, Red];
    public static readonly Tile[] AllTiles = [
        M1, M2, M3, M4, M5, M6, M7, M8, M9,
        P1, P2, P3, P4, P5, P6, P7, P8, P9,
        S1, S2, S3, S4, S5, S6, S7, S8, S9,
        East, South, West, North, White, Green, Red
    ];
    public const int Count = 34;

    // --- 属性 ---
    /// <summary>花色</summary>
    public Suit Suit => Id switch { < 9 => Suit.Manzu, < 18 => Suit.Pinzu, < 27 => Suit.Souzu, _ => Suit.Honor };
    /// <summary>数字（1-9 数牌，1-7 字牌）</summary>
    public int Number => Suit == Suit.Honor ? Id - 27 + 1 : Id % 9 + 1;
    public bool IsHonor => Id >= 27;
    public bool IsSuited => Id < 27;
    /// <summary>是否幺九牌（数牌1/9）</summary>
    public bool IsTerminal => Number == 1 || Number == 9;
    public bool IsWind => Id is >= 27 and <= 30;
    public bool IsDragon => Id is >= 31 and <= 33;
    /// <summary>是否中张牌（2-8数牌）</summary>
    public bool IsSimple => IsSuited && Number is >= 2 and <= 8;
    /// <summary>是否幺九牌（含字牌）</summary>
    public bool IsTerminalOrHonor => !IsSimple || IsHonor;
    /// <summary>是否绿一色可用的牌: 2s,3s,4s,6s,8s,发</summary>
    public bool IsGreen => Id == S2.Id || Id == S3.Id || Id == S4.Id || Id == S6.Id || Id == S8.Id || Id == Green.Id;
    /// <summary>是否国士无双需要的牌（幺九牌+字牌）</summary>
    public bool IsKokushi => IsTerminal || IsHonor;

    // --- 宝牌逻辑 ---
    /// <summary>此指示牌对应的宝牌。</summary>
    public Tile NextDora()
    {
        if (Id == M9.Id) return M1;
        if (Id == P9.Id) return P1;
        if (Id == S9.Id) return S1;
        if (Id == East.Id) return South;
        if (Id == South.Id) return West;
        if (Id == West.Id) return North;
        if (Id == North.Id) return East;
        if (Id == White.Id) return Green;
        if (Id == Green.Id) return Red;
        if (Id == Red.Id) return White;
        return new Tile((byte)(Id + 1));
    }

    // --- 解析 ---
    /// <summary>从字符串解析牌。支持 "1m"、"5p"、"9s"、"1z" 及字母 "e"/"s"/"w"/"n"/"wh"/"g"/"r"。</summary>
    public static Tile? TryParse(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        switch (s.ToLowerInvariant())
        {
            case "e": return East;
            case "s": return South;
            case "w": return West;
            case "n": return North;
            case "wh": return White;
            case "g": return Green;
            case "r": return Red;
        }
        if (s.Length >= 2 && char.IsDigit(s[0]))
        {
            int num = s[0] - '0';
            char suit = char.ToLowerInvariant(s[^1]);
            if (num is < 0 or > 9) return null;
            return suit switch
            {
                'm' => FromSuit(Suit.Manzu, num),
                'p' => FromSuit(Suit.Pinzu, num),
                's' => FromSuit(Suit.Souzu, num),
                'z' => num switch { 1 => East, 2 => South, 3 => West, 4 => North, 5 => White, 6 => Green, 7 => Red, _ => null },
                _ => null
            };
        }
        return null;
    }

    private static Tile? FromSuit(Suit suit, int number)
    {
        if (number is < 1 or > 9) return null;
        int baseId = suit switch { Suit.Manzu => 0, Suit.Pinzu => 9, Suit.Souzu => 18, _ => -1 };
        return baseId >= 0 ? new Tile((byte)(baseId + number - 1)) : null;
    }

    public override string ToString()
    {
        if (IsHonor) return Id switch { 27 => "E", 28 => "S", 29 => "W", 30 => "N", 31 => "Wh", 32 => "G", 33 => "R", _ => $"{Id - 27 + 1}z" };
        char suit = Suit switch { Suit.Manzu => 'm', Suit.Pinzu => 'p', Suit.Souzu => 's', _ => '?' };
        return $"{Number}{suit}";
    }
}
