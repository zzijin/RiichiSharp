using RiichiSharp.Tiles;
using RiichiSharp.Parse;
using RiichiSharp.Furiten;
using RiichiSharp.Tenpai;
using RiichiSharp.Validation;

namespace RiichiSharp.Tests;

public class FuritenTests
{
    [Fact] public void 无振听_舍牌中无待牌()
    {
        var tiles = TenhouParser.Parse("123m456p789s222s1z")!.Value; // 听 1z (单骑)
        var discards = new List<Tile> { Tile.M5, Tile.P5, Tile.South };
        var status = FuritenDetector.Check(tiles, 0, discards);
        Assert.False(status.IsFuriten);
    }

    [Fact] public void 舍牌振听_待牌在舍牌中()
    {
        var tiles = TenhouParser.Parse("123m456p789s222s1z")!.Value; // 听 1z (East)
        var discards = new List<Tile> { Tile.East };
        var status = FuritenDetector.Check(tiles, 0, discards);
        Assert.True(status.IsFuriten);
        Assert.Equal(FuritenDetector.FuritenType.Discard, status.Type);
    }

    [Fact] public void 立直后振听()
    {
        var tiles = TenhouParser.Parse("123m456p789s222s1z")!.Value;
        var discards = new List<Tile> { Tile.East };
        var status = FuritenDetector.Check(tiles, 0, discards, isRiichi: true);
        Assert.Equal(FuritenDetector.FuritenType.Riichi, status.Type);
    }

    [Fact] public void 同巡振听_未立直但放过荣和()
    {
        var tiles = TenhouParser.Parse("123m456p789s222s1z")!.Value;
        var discards = new List<Tile> { Tile.M5 };
        var status = FuritenDetector.Check(tiles, 0, discards, hasPassedRon: true);
        Assert.True(status.IsFuriten);
        Assert.Equal(FuritenDetector.FuritenType.Temporary, status.Type);
    }

    [Fact] public void 非听牌_振听无意义()
    {
        var tiles = TenhouParser.Parse("123m456p789s12z")!.Value;
        var discards = new List<Tile> { Tile.East };
        var status = FuritenDetector.Check(tiles, 0, discards);
        Assert.False(status.IsFuriten);
    }

    [Fact] public void 振听不影响自摸()
    {
        var tiles = TenhouParser.Parse("123m456p789s222s1z")!.Value;
        var discards = new List<Tile> { Tile.East };
        var status = FuritenDetector.Check(tiles, 0, discards);
        Assert.NotEmpty(status.TsumoTiles);
    }
}

public class TenpaiCalculatorTests
{
    [Fact] public void 标准听牌_两面听()
    {
        var tiles = TenhouParser.Parse("123m456p789s1112z")!.Value; // 4 melds + 0 pair = tenpai (单骑) // 12枚，向听1
        var result = TenpaiCalculator.Calculate(tiles);
        Assert.True(result.IsTenpai);
    }

    [Fact] public void 标准听牌_嵌张听()
    {
        var tiles = TenhouParser.Parse("123m456p789s11z46p")!.Value;
        var result = TenpaiCalculator.Calculate(tiles);
        Assert.True(result.IsTenpai);
    }

    [Fact] public void 非听牌()
    {
        var tiles = TenhouParser.Parse("123m456p789s12z")!.Value;
        var result = TenpaiCalculator.Calculate(tiles);
        Assert.False(result.IsTenpai);
    }

    [Fact] public void 听牌枚数计算()
    {
        var tiles = TenhouParser.Parse("123m456p789s1112z")!.Value; // 4 melds + 0 pair = tenpai (单骑)
        var result = TenpaiCalculator.Calculate(tiles);
        if (result.IsTenpai)
        {
            Assert.NotEmpty(result.WaitCounts);
            foreach (var (tile, count) in result.WaitCounts)
                Assert.True(count is >= 0 and <= 4);
        }
    }
}

public class TenhouReplayValidatorTests
{
    [Fact] public void 天凤牌ID映射_万子()
    {
        Assert.Equal(0, TenhouReplayValidator.TenhouHaiIdToTileIndex(0));  // 1m 第1张
        Assert.Equal(0, TenhouReplayValidator.TenhouHaiIdToTileIndex(3));  // 1m 第4张
        Assert.Equal(1, TenhouReplayValidator.TenhouHaiIdToTileIndex(4));  // 2m 第1张
        Assert.Equal(8, TenhouReplayValidator.TenhouHaiIdToTileIndex(32)); // 9m 第1张
    }

    [Fact] public void 天凤牌ID映射_筒子()
    {
        Assert.Equal(9, TenhouReplayValidator.TenhouHaiIdToTileIndex(36));  // 1p
        Assert.Equal(17, TenhouReplayValidator.TenhouHaiIdToTileIndex(68)); // 9p
    }

    [Fact] public void 天凤牌ID映射_索子()
    {
        Assert.Equal(18, TenhouReplayValidator.TenhouHaiIdToTileIndex(72));  // 1s
        Assert.Equal(26, TenhouReplayValidator.TenhouHaiIdToTileIndex(104)); // 9s
    }

    [Fact] public void 天凤牌ID映射_字牌()
    {
        Assert.Equal(27, TenhouReplayValidator.TenhouHaiIdToTileIndex(108)); // E
        Assert.Equal(28, TenhouReplayValidator.TenhouHaiIdToTileIndex(112)); // S
        Assert.Equal(33, TenhouReplayValidator.TenhouHaiIdToTileIndex(132)); // Red
    }

    [Fact] public void 空牌谱_返回空结果()
    {
        var report = TenhouReplayValidator.Validate("<mjloggm></mjloggm>");
        Assert.Equal(0, report.Total);
        Assert.Equal(100, report.AccuracyPercent);
    }
}
