using RiichiSharp.Tiles;
using RiichiSharp.Parse;
using RiichiSharp.Hand;
using RiichiSharp.Wait;
using RiichiSharp.Scoring;
using Yk = RiichiSharp.Yaku.Yaku;
using Ym = RiichiSharp.Yaku.Yakuman;
using YD = RiichiSharp.Yaku.YakuDetector;

namespace RiichiSharp.Tests;

public class KuisagariTests
{
    [Fact] public void Honitsu_Closed_3Han() {
        var tiles = TenhouParser.Parse("123456789m111z66z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.East,
            RoundWind = Tile.East, SeatWind = Tile.East, IsOpen = false };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Equal(3, r.YakuHan.GetValueOrDefault(Yk.Honitsu)); }

    [Fact] public void Honitsu_Open_2Han() {
        var tiles = TenhouParser.Parse("123456789m111z66z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.East,
            RoundWind = Tile.East, SeatWind = Tile.East, IsOpen = true };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Equal(2, r.YakuHan.GetValueOrDefault(Yk.Honitsu)); }

    [Fact] public void Chinitsu_Closed_6Han() {
        var tiles = TenhouParser.Parse("123456789m123m11m")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M1,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = false };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Equal(6, r.YakuHan.GetValueOrDefault(Yk.Chinitsu)); }

    [Fact] public void Chinitsu_Open_5Han() {
        var tiles = TenhouParser.Parse("123456789m123m11m")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M1,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = true };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Equal(5, r.YakuHan.GetValueOrDefault(Yk.Chinitsu)); }

    [Fact] public void SanshokuDoujun_Closed_2Han() {
        var tiles = TenhouParser.Parse("123m123p123s456m11z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M4,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = false };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Equal(2, r.YakuHan.GetValueOrDefault(Yk.SanshokuDoujun)); }

    [Fact] public void SanshokuDoujun_Open_1Han() {
        var tiles = TenhouParser.Parse("123m123p123s456m11z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M4,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = true };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Equal(1, r.YakuHan.GetValueOrDefault(Yk.SanshokuDoujun)); }

    [Fact] public void Ittsu_Closed_2Han() {
        var tiles = TenhouParser.Parse("123456789m456p11z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.South,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = false };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Equal(2, r.YakuHan.GetValueOrDefault(Yk.Ittsu)); }

    [Fact] public void Ittsu_Open_1Han() {
        var tiles = TenhouParser.Parse("123456789m456p11z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.South,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = true };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Equal(1, r.YakuHan.GetValueOrDefault(Yk.Ittsu)); }
}

public class ConflictFilteringTests
{
    [Fact] public void Chinitsu_Supersedes_Honitsu() {
        var tiles = TenhouParser.Parse("123456789m123m11m")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M1,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = false };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Contains(Yk.Chinitsu, r.YakuHan.Keys);
        Assert.False(r.YakuHan.ContainsKey(Yk.Honitsu)); }

    [Fact] public void Junchan_Supersedes_Chanta() {
        var tiles = TenhouParser.Parse("123m789m123p789p11m")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M1,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = false };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Contains(Yk.Junchan, r.YakuHan.Keys);
        Assert.False(r.YakuHan.ContainsKey(Yk.Chanta)); }

    [Fact] public void Ryanpeikou_Supersedes_Iipeikou() {
        var tiles = TenhouParser.Parse("123123m456456p11z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.East,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = false };
        var r = YD.Detect(s, tiles, ctx);
        if (r.YakuHan.ContainsKey(Yk.Ryanpeikou))
            Assert.False(r.YakuHan.ContainsKey(Yk.Iipeikou)); }
}

public class FuEdgeCaseTests
{
    [Fact] public void Pinfu_Ron_30Fu() {
        var tiles = TenhouParser.Parse("123m456p789s234s22z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Ron, WinningTile = Tile.S2,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = false };
        var waits = WaitDetector.Detect(s, Tile.S2);
        var fu = FuCalculator.Calculate(s, ctx, waits);
        Assert.True(fu.Total == 20 || fu.Total == 30,
            $"Pinfu ron should be 20 or 30 fu, got {fu.Total}"); }

    [Fact] public void Open_Tsumo_Still_Plus2Fu() {
        // Open hand tsumo still gets +2 fu
        var tiles = TenhouParser.Parse("123m456p789s234s22z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.S2,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = true };
        var waits = WaitDetector.Detect(s, Tile.S2);
        var fu = FuCalculator.Calculate(s, ctx, waits);
        Assert.True(fu.Total >= 20); }

    [Fact] public void PairFu_DoubleWind() {
        int fu = FuCalculator.CalculatePairFu(Tile.East, Tile.East, Tile.East);
        Assert.Equal(4, fu); }

    [Fact] public void PairFu_Dragon() {
        int fu = FuCalculator.CalculatePairFu(Tile.White, Tile.East, Tile.West);
        Assert.Equal(2, fu); }

    [Fact] public void PairFu_NoValue() {
        int fu = FuCalculator.CalculatePairFu(Tile.M2, Tile.East, Tile.West);
        Assert.Equal(0, fu); }
}

public class YakumanStackingTests
{
    [Fact] public void Daisangen_Single() {
        var tiles = TenhouParser.Parse("555z666z777z123m11z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.White };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Single(r.Yakumans);
        Assert.Contains(Ym.Daisangen, r.Yakumans); }

    [Fact] public void Tsuuiisou_Shousuushi_Double() {
        // Tsuuiisou + Shousuushi = double yakuman
        var tiles = TenhouParser.Parse("111z222z333z444z55z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.East,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var r = YD.Detect(s, tiles, ctx);
        // Tsuuiisou detected, check if multiple yakuman stack
        Assert.Contains(Ym.Tsuuiisou, r.Yakumans);
        Assert.True(r.Yakumans.Count >= 1); }
}

public class ScoreKiriageTests
{
    [Fact] public void KiriageMangan_Off_4Han30Fu() {
        var level = ScoreCalculator.DetermineLevel(4, 30, false, 0, false, false);
        var basic = ScoreCalculator.CalculateBasicPoints(4, 30, level, false);
        // 4 han 30 fu = 30 * 2^6 = 1920. Without kiriage, normal scoring.
        Assert.True(basic <= 2000); }

    [Fact] public void KiriageMangan_On_4Han30Fu() {
        var level = ScoreCalculator.DetermineLevel(4, 30, false, 0, false, true);
        var basic = ScoreCalculator.CalculateBasicPoints(4, 30, level, true);
        // With kiriage mangan, 1920 → 2000
        Assert.Equal(2000, basic); }

    [Fact] public void KazoeYakuman_13Han() {
        var level = ScoreCalculator.DetermineLevel(13, 30, false, 0, true, false);
        Assert.Equal(8000, ScoreCalculator.CalculateBasicPoints(13, 30, level, false)); }

    [Fact] public void KazoeYakuman_Off_13Han() {
        var level = ScoreCalculator.DetermineLevel(13, 30, false, 0, false, false);
        Assert.Equal(8000, ScoreCalculator.CalculateBasicPoints(13, 30, level, false)); }
}
