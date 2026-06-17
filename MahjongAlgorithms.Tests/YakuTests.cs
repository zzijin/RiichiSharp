using MahjongAlgorithms.Tiles;
using MahjongAlgorithms.Parse;
using MahjongAlgorithms.Hand;
using MahjongAlgorithms.Wait;
using MahjongAlgorithms.Scoring;
using Yk = MahjongAlgorithms.Yaku.Yaku;
using Ym = MahjongAlgorithms.Yaku.Yakuman;
using YD = MahjongAlgorithms.Yaku.YakuDetector;

namespace MahjongAlgorithms.Tests;

public class YakuDetectionTests
{
    private static YD.YakuResult Detect(string handStr, Tile winTile, bool tsumo = true,
        bool riichi = false, Tile? round = null, Tile? seat = null)
    {
        var tiles = TenhouParser.Parse(handStr)!.Value;
        var structures = Decomposer.Decompose(tiles);
        var s = structures.First(x => x is Standard);
        var ctx = new GameContext
        {
            WinType = tsumo ? WinType.Tsumo : WinType.Ron,
            WinningTile = winTile,
            RoundWind = round ?? Tile.East,
            SeatWind = seat ?? Tile.East,
            IsRiichi = riichi,
            IsOpen = false
        };
        return YD.Detect(s, tiles, ctx);
    }

    // --- 1 Han ---
    [Fact] public void Riichi_Only() {
        var r = Detect("123m456p789s345s11z", Tile.S5, tsumo: true, riichi: true, seat: Tile.West);
        Assert.Contains(Yk.Riichi, r.YakuHan.Keys); }

    [Fact] public void MenzenTsumo() {
        var r = Detect("123m456p789s345s11z", Tile.S5, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.MenzenTsumo, r.YakuHan.Keys); }

    [Fact] public void Pinfu() {
        // Need ryanmen wait + non-yakuhai pair. Use South pair (seat=West, round=East)
        var r = Detect("123m456p789s234s22z", Tile.S2, tsumo: true, round: Tile.East, seat: Tile.West);
        Assert.Contains(Yk.Pinfu, r.YakuHan.Keys); }

    [Fact] public void Tanyao() {
        // All 2-8 suited tiles
        var r = Detect("234m456p678s234s22p", Tile.S2, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.Tanyao, r.YakuHan.Keys); }

    [Fact] public void Iipeikou() {
        var r = Detect("123123m456p789s11z", Tile.M2, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.Iipeikou, r.YakuHan.Keys); }

    [Fact] public void Yakuhai_Dragon() {
        var r = Detect("123m456p789s555z11z", Tile.M1, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.YakuhaiHaku, r.YakuHan.Keys); }

    [Fact] public void Yakuhai_RoundWind() {
        var r = Detect("123m456p789s111z66z", Tile.East, tsumo: true, round: Tile.East);
        Assert.Contains(Yk.YakuhaiRoundWind, r.YakuHan.Keys); }

    [Fact] public void Yakuhai_DoubleWind() {
        // Triplet of East when both round AND seat wind are East → 2 han
        var r = Detect("123m456p789s111z66z", Tile.East, tsumo: true, round: Tile.East, seat: Tile.East);
        Assert.Contains(Yk.YakuhaiRoundWind, r.YakuHan.Keys);
        Assert.Contains(Yk.YakuhaiSeatWind, r.YakuHan.Keys); }

    [Fact] public void RinshanKaihou() {
        var tiles = TenhouParser.Parse("123m456p789s234s11z")!.Value;
        var s = Decomposer.Decompose(tiles).First(x => x is Standard);
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.S2,
            RoundWind = Tile.East, SeatWind = Tile.West, IsRinshan = true };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Contains(Yk.RinshanKaihou, r.YakuHan.Keys); }

    [Fact] public void RinshanKaihou_NoRon() {
        // Rinshan must be tsumo
        var r = Detect("123m456p789s234s11z", Tile.S2, tsumo: false, seat: Tile.West);
        var tiles2 = TenhouParser.Parse("123m456p789s234s11z")!.Value;
        var s2 = Decomposer.Decompose(tiles2).First(x => x is Standard);
        var ctx2 = new GameContext { WinType = WinType.Ron, WinningTile = Tile.S2,
            RoundWind = Tile.East, SeatWind = Tile.West, IsRinshan = true };
        var r2 = YD.Detect(s2, tiles2, ctx2);
        Assert.False(r2.YakuHan.ContainsKey(Yk.RinshanKaihou)); }

    // --- 2 Han ---
    [Fact] public void Toitoi_WithOpenMeld() {
        // Open triplet + 3 closed triplets + pair = Toitoi without Suuankou
        // Hand: open pon of 1m, closed: 222p 333s 444z, pair 66z
        var tiles = TenhouParser.Parse("222p333s444z66z")!.Value;
        var calledMelds = new List<Meld> { new Koutsu(Tile.M1, isOpen: true) };
        var structures = Decomposer.DecomposeWithMelds(tiles, calledMelds);
        var s = structures.OfType<Standard>().FirstOrDefault();
        Assert.NotNull(s);
        var ctx = new GameContext { WinType = WinType.Ron, WinningTile = Tile.M1,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = true };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Contains(Yk.Toitoi, r.YakuHan.Keys); }

    [Fact] public void Toitoi_ClosedButNonYakuman() {
        // 2 triplets + 2 sequences = NOT toitoi. Use a clear toitoi hand with sequence
        // Actually: 3 triplets + 1 sequence + pair is not toitoi. Use all triplets.
        // 111m 222p 333s 444z 55z → But 55z is White pair. 444z is North.
        // This is Suuankou. Need a hand that's toitoi without being suuankou.
        // Solution: use an open meld or a hand with 4 triplets where one is open.
        // For closed: 111m 222p 333s 55z + 789s (one sequence) is not toitoi.
        // Let's test: 111m 222p 333s 55z + open pon of 444z via decomposeWithMelds
        var tiles = TenhouParser.Parse("111m222p333s55z")!.Value;
        var melds = new List<Meld> { new Koutsu(Tile.North, isOpen: true) }; // Open 444z
        var structures = Decomposer.DecomposeWithMelds(tiles, melds);
        var s = structures.OfType<Standard>().FirstOrDefault();
        Assert.NotNull(s);
        var ctx = new GameContext { WinType = WinType.Ron, WinningTile = Tile.North,
            RoundWind = Tile.East, SeatWind = Tile.West, IsOpen = true };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Contains(Yk.Toitoi, r.YakuHan.Keys); }

    [Fact] public void Sanankou() {
        // 3 concealed triplets + sequence + pair (tsumo on a triplet tile)
        var r = Detect("111m222p333s789m11z", Tile.M1, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.Sanankou, r.YakuHan.Keys); }

    [Fact] public void SanshokuDoujun() {
        var r = Detect("123m123p123s456m11z", Tile.M4, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.SanshokuDoujun, r.YakuHan.Keys); }

    [Fact] public void SanshokuDoukou() {
        var r = Detect("111m111p111s789m11z", Tile.M7, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.SanshokuDoukou, r.YakuHan.Keys); }

    [Fact] public void Ittsu() {
        var r = Detect("123456789m456p11z", Tile.South, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.Ittsu, r.YakuHan.Keys); }

    [Fact] public void Chanta() {
        var r = Detect("123m789p123s789m11z", Tile.M9, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.Chanta, r.YakuHan.Keys); }

    [Fact] public void Honroutou_Chiitoitsu() {
        // Chiitoitsu with all terminals/honors
        var tiles = TenhouParser.Parse("1199m1199p1199s11z")!.Value;
        var chi = Decomposer.Decompose(tiles).OfType<Chiitoitsu>().FirstOrDefault();
        if (chi == null) return;
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M1,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var r = YD.Detect(chi, tiles, ctx);
        Assert.Contains(Yk.Honroutou, r.YakuHan.Keys); }

    [Fact] public void Shousangen() {
        // 2 dragon triplets + dragon pair
        var r = Detect("555z666z123m456p77z", Tile.M1, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.Shousangen, r.YakuHan.Keys); }

    // --- 3-6 Han ---
    [Fact] public void Honitsu() {
        var r = Detect("123456789m111z66z", Tile.East, tsumo: true, seat: Tile.East);
        Assert.Contains(Yk.Honitsu, r.YakuHan.Keys); }

    [Fact] public void Chinitsu() {
        var r = Detect("123456789m123m11m", Tile.M1, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.Chinitsu, r.YakuHan.Keys);
        Assert.Equal(6, r.YakuHan[Yk.Chinitsu]); }

    [Fact] public void Junchan() {
        var r = Detect("123m789m123p789p11m", Tile.M1, tsumo: true, seat: Tile.West);
        Assert.Contains(Yk.Junchan, r.YakuHan.Keys); }

    [Fact] public void Chiitoitsu_Yaku() {
        var tiles = TenhouParser.Parse("112233m4455s6677z")!.Value;
        var chi = Decomposer.Decompose(tiles).OfType<Chiitoitsu>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M1,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var r = YD.Detect(chi, tiles, ctx);
        Assert.Contains(Yk.Chiitoitsu, r.YakuHan.Keys); }

    // --- Situational ---
    [Fact] public void Haitei() {
        var tiles = TenhouParser.Parse("123m456p789s234s11z")!.Value;
        var s = Decomposer.Decompose(tiles).First(x => x is Standard);
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.S2,
            RoundWind = Tile.East, SeatWind = Tile.West, IsLastTile = true };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Contains(Yk.HaiteiRaoyue, r.YakuHan.Keys); }

    [Fact] public void Chankan_MustBeRon() {
        var r = Detect("123m456p789s234s11z", Tile.S2, tsumo: true, seat: Tile.West);
        var tiles = TenhouParser.Parse("123m456p789s234s11z")!.Value;
        var s = Decomposer.Decompose(tiles).First(x => x is Standard);
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.S2,
            RoundWind = Tile.East, SeatWind = Tile.West, IsChankan = true };
        var r2 = YD.Detect(s, tiles, ctx);
        Assert.False(r2.YakuHan.ContainsKey(Yk.Chankan)); }
}

public class YakumanTests
{
    [Fact] public void Kokushi_Single() {
        var tiles = TenhouParser.Parse("19m19p19s12345677z")!.Value;
        var k = Decomposer.Decompose(tiles).OfType<Kokushi>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.East,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var r = YD.Detect(k, tiles, ctx);
        Assert.Contains(Ym.KokushiMusou, r.Yakumans); }

    [Fact] public void Kokushi_13Wait() {
        var tiles = TenhouParser.Parse("19m19p19s1234567z7z")!.Value;
        var k = Decomposer.Decompose(tiles).OfType<Kokushi>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.Red,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var r = YD.Detect(k, tiles, ctx);
        Assert.Contains(Ym.KokushiMusou13Wait, r.Yakumans); }

    [Fact] public void Daisangen() {
        var tiles = TenhouParser.Parse("555z666z777z123m11z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.White };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Contains(Ym.Daisangen, r.Yakumans); }

    [Fact] public void Suuankou() {
        var tiles = TenhouParser.Parse("111m222p333s444z55z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.East,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Contains(Ym.Suuankou, r.Yakumans); }

    [Fact] public void Tsuuiisou() {
        var tiles = TenhouParser.Parse("111z222z333z444z55z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.East,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Contains(Ym.Tsuuiisou, r.Yakumans); }

    [Fact] public void Chinroutou() {
        var tiles = TenhouParser.Parse("111m999m111p999p11m")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M1,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Contains(Ym.Chinroutou, r.Yakumans); }

    [Fact] public void Shousuushi() {
        // 3 wind triplets + pair in 4th wind
        var tiles = TenhouParser.Parse("111z222z333z123m44z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M1,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Contains(Ym.Shousuushi, r.Yakumans); }
}

public class FuScoreTests
{
    [Fact] public void Pinfu_Tsumo_20Fu() {
        // Non-yakuhai pair (South), seat=West, round=East
        var tiles = TenhouParser.Parse("123m456p789s234s22z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.S2,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var waits = WaitDetector.Detect(s, Tile.S2);
        var fu = FuCalculator.Calculate(s, ctx, waits);
        Assert.Equal(20, fu.Total); }

    [Fact] public void Menzen_Ron_30Fu_Min() {
        var tiles = TenhouParser.Parse("123m456p789s234s22z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Ron, WinningTile = Tile.S2,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var waits = WaitDetector.Detect(s, Tile.S2);
        var fu = FuCalculator.Calculate(s, ctx, waits);
        Assert.True(fu.Total >= 30, $"Expected >= 30 fu, got {fu.Total}"); }

    [Fact] public void Chiitoitsu_25Fu() {
        var tiles = TenhouParser.Parse("112233m4455s6677z")!.Value;
        var chi = Decomposer.Decompose(tiles).OfType<Chiitoitsu>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M1 };
        var fu = FuCalculator.Calculate(chi, ctx);
        Assert.Equal(25, fu.Total); }

    [Fact] public void Koutsu_Closed_Terminal_8Fu() {
        // Closed triplet of terminal (1m) = 8 fu. Base 20 + tsumo 2 + triplet 8 = 30 → 30.
        var tiles = TenhouParser.Parse("111m456p789s234s22z")!.Value;
        var s = Decomposer.Decompose(tiles).OfType<Standard>().First();
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M1,
            RoundWind = Tile.East, SeatWind = Tile.West };
        var waits = WaitDetector.Detect(s, Tile.M1);
        var fu = FuCalculator.Calculate(s, ctx, waits);
        Assert.Equal(30, fu.Total); }

    [Fact] public void Mangan_5Han() {
        var level = ScoreCalculator.DetermineLevel(5, 30, false, 0, false, false);
        Assert.Equal(2000, ScoreCalculator.CalculateBasicPoints(5, 30, level, false)); }

    [Fact] public void Haneman_6Han() {
        var level = ScoreCalculator.DetermineLevel(6, 30, false, 0, false, false);
        Assert.Equal(3000, ScoreCalculator.CalculateBasicPoints(6, 30, level, false)); }

    [Fact] public void Dealer_Ron() {
        var pay = ScoreCalculator.CalculatePayment(2000, true, false);
        Assert.True(pay.Total >= 12000); }

    [Fact] public void NonDealer_Ron() {
        var pay = ScoreCalculator.CalculatePayment(2000, false, false);
        Assert.True(pay.Total >= 7700); }
}
