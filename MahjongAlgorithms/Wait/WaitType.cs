namespace MahjongAlgorithms.Wait;

/// <summary>听牌类型（待ちの種類），决定听牌符数和和了资格。</summary>
public enum WaitType
{
    /// <summary>两面待ち（0符）</summary>
    Ryanmen,
    /// <summary>嵌张待ち（2符）</summary>
    Kanchan,
    /// <summary>边张待ち（2符）</summary>
    Penchan,
    /// <summary>双碰待ち（0符）</summary>
    Shanpon,
    /// <summary>单骑待ち（2符）</summary>
    Tanki,
    /// <summary>国士无双13面待ち（0符，役满）</summary>
    Kokushi13,
}

public static class WaitFu
{
    public static int GetFu(WaitType type) => type switch
    {
        WaitType.Ryanmen => 0,
        WaitType.Shanpon => 0,
        WaitType.Kokushi13 => 0,
        WaitType.Kanchan => 2,
        WaitType.Penchan => 2,
        WaitType.Tanki => 2,
        _ => 0
    };
}
