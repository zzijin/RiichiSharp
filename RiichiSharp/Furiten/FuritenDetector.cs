namespace RiichiSharp.Furiten;

using RiichiSharp.Tiles;
using RiichiSharp.Hand;
using RiichiSharp.Shanten;

/// <summary>振听检测：舍牌振听（现物）、同巡振听、立直永久振听三级。</summary>
public static class FuritenDetector
{
    /// <summary>振听检查结果。</summary>
    public record FuritenStatus
    {
        /// <summary>是否处于振听状态（无法荣和）。</summary>
        public bool IsFuriten => Type != FuritenType.None;
        public FuritenType Type { get; set; }
        /// <summary>造成振听的具体舍牌。（仅舍牌振听/立直振听时有值）</summary>
        public List<Tile> FuritenTiles { get; init; } = [];
        /// <summary>仍可荣和的待牌（排除振听牌后）。</summary>
        public List<Tile> SafeRonTiles { get; init; } = [];
        /// <summary>不能荣和的待牌（振听牌）。</summary>
        public List<Tile> UnsafeRonTiles { get; init; } = [];
        /// <summary>可自摸的待牌（振听不影响自摸）。</summary>
        public List<Tile> TsumoTiles { get; init; } = [];
        public override string ToString() => IsFuriten ? $"{Type}: 不可荣和 {string.Join(" ", UnsafeRonTiles)}" : "非振听";
    }

    public enum FuritenType { None, Discard, Temporary, Riichi }

    /// <summary>
    /// 全面振听检测。
    /// </summary>
    /// <param name="hand">手牌（13枚）</param>
    /// <param name="calledMelds">副露面子数</param>
    /// <param name="discards">该玩家的舍牌牌河</param>
    /// <param name="isRiichi">是否已立直</param>
    /// <param name="hasPassedRon">本巡是否已放弃过荣和（同巡振听）</param>
    public static FuritenStatus Check(TileSet hand, int calledMelds, List<Tile> discards,
        bool isRiichi = false, bool hasPassedRon = false)
    {
        var result = new FuritenStatus();

        // 1. 找出全部听牌（所有能和了的牌）
        var allWaits = FindAllWinningTiles(hand, calledMelds);

        // 2. 遍历每种待牌，检查是否在舍牌中
        var furitenTiles = new HashSet<byte>();
        var discardSet = new HashSet<byte>(discards.Select(d => d.Id));

        foreach (var wait in allWaits)
        {
            if (discardSet.Contains(wait.Id))
                furitenTiles.Add(wait.Id);
        }

        // 3. 分类
        foreach (var wait in allWaits)
        {
            if (furitenTiles.Contains(wait.Id))
                result.UnsafeRonTiles.Add(wait);
            else
                result.SafeRonTiles.Add(wait);
            result.TsumoTiles.Add(wait); // 振听不影响自摸
        }

        // 4. 确定振听类型
        if (furitenTiles.Count > 0)
        {
            if (isRiichi)
                result.Type = FuritenType.Riichi; // 立直后永久振听
            else
                result.Type = FuritenType.Discard; // 舍牌振听

            foreach (var id in furitenTiles)
                result.FuritenTiles.Add(new Tile(id));
        }
        else if (hasPassedRon && allWaits.Count > 0)
        {
            // 同巡振听：本巡放过荣和，所有听牌均不可荣和
            result.Type = FuritenType.Temporary;
            result.UnsafeRonTiles.AddRange(result.SafeRonTiles);
            result.SafeRonTiles.Clear();
        }

        return result;
    }

    /// <summary>找出一副手牌的全部和了牌（待牌）。</summary>
    private static List<Tile> FindAllWinningTiles(TileSet hand, int calledMelds)
    {
        var result = new List<Tile>();
        var shanten = ShantenCalculator.Calculate(hand, calledMelds);
        if (shanten.Shanten != 0) return result; // 非听牌

        // 进张分析获取改良牌（对于听牌手牌 = 和了牌）
        var ukeire = UkeireCalculator.Calculate(hand, calledMelds);
        foreach (var ut in ukeire.Tiles)
            if (ut.Available > 0) result.Add(ut.Tile);
        return result;
    }

    /// <summary>快速检测：此牌是否对该玩家是现物（安全牌）？</summary>
    public static bool IsGenbutsu(Tile tile, List<Tile> discards, List<Meld>? opponentMelds = null)
    {
        // 1. 舍牌现物：对手自己打过的牌
        if (discards.Any(d => d.Id == tile.Id)) return true;

        // 2. 副露现物：对手碰/吃时拿到的牌旁边的同花色相邻牌可能是安全的
        // （简化处理，仅检查舍牌现物）
        return false;
    }
}
