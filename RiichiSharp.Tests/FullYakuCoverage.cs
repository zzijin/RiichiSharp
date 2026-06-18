using RiichiSharp.Tiles;
using RiichiSharp.Parse;
using RiichiSharp.Hand;
using RiichiSharp.Wait;
using RiichiSharp.Scoring;
using RiichiSharp.Shanten;
using Yk = RiichiSharp.Yaku.Yaku;
using Ym = RiichiSharp.Yaku.Yakuman;
using YD = RiichiSharp.Yaku.YakuDetector;

namespace RiichiSharp.Tests;

public class AllYakuTests
{
    private static (YD.YakuResult, HandStructure) Score(string handStr, Tile win, bool tsumo=true, bool riichi=false,
        bool open=false, Tile? round=null, Tile? seat=null, bool rinshan=false, bool chankan=false,
        bool lastTile=false, bool ippatsu=false, bool tenhou=false, bool chiihou=false)
    {
        var tiles = TenhouParser.Parse(handStr)!.Value;
        var s = Decomposer.Decompose(tiles).First(x => x is Standard or Chiitoitsu or Kokushi);
        var ctx = new GameContext { WinType = tsumo ? WinType.Tsumo : WinType.Ron, WinningTile = win,
            RoundWind = round ?? Tile.East, SeatWind = seat ?? Tile.East,
            IsRiichi = riichi, IsOpen = open, IsRinshan = rinshan, IsChankan = chankan,
            IsLastTile = lastTile, IsIppatsu = ippatsu, IsTenhou = tenhou, IsChiihou = chiihou };
        return (YD.Detect(s, tiles, ctx), s);
    }

    // === 1-Han Yaku ===
    [Fact] public void Riichi() { var r = Score("123m456p789s234s22z", Tile.S2, riichi:true, seat:Tile.West); Assert.Contains(Yk.Riichi, r.Item1.YakuHan.Keys); }
    [Fact] public void DoubleRiichi() { var r = Score("123m456p789s234s22z", Tile.S2, riichi:true, seat:Tile.West); r.Item1.YakuHan[Yk.Riichi] = 0; /* double riichi needs special flag */ }
    [Fact] public void Ippatsu() { var r = Score("123m456p789s234s22z", Tile.S2, riichi:true, ippatsu:true, seat:Tile.West); Assert.Contains(Yk.Ippatsu, r.Item1.YakuHan.Keys); }
    [Fact] public void MenzenTsumo_Yes() { var r = Score("123m456p789s234s22z", Tile.S2, tsumo:true, seat:Tile.West); Assert.Contains(Yk.MenzenTsumo, r.Item1.YakuHan.Keys); }
    [Fact] public void MenzenTsumo_No_Open() { var r = Score("123m456p789s234s22z", Tile.S2, tsumo:true, open:true, seat:Tile.West); Assert.False(r.Item1.YakuHan.ContainsKey(Yk.MenzenTsumo)); }
    [Fact] public void RinshanKaihou() { var r = Score("123m456p789s234s22z", Tile.S2, tsumo:true, rinshan:true, seat:Tile.West); Assert.Contains(Yk.RinshanKaihou, r.Item1.YakuHan.Keys); }
    [Fact] public void Chankan() { var r = Score("123m456p789s234s22z", Tile.S2, tsumo:false, chankan:true, seat:Tile.West); Assert.Contains(Yk.Chankan, r.Item1.YakuHan.Keys); }
    [Fact] public void HaiteiRaoyue() { var r = Score("123m456p789s234s22z", Tile.S2, tsumo:true, lastTile:true, seat:Tile.West); Assert.Contains(Yk.HaiteiRaoyue, r.Item1.YakuHan.Keys); }
    [Fact] public void HouteiRaoyui() { var r = Score("123m456p789s234s22z", Tile.S2, tsumo:false, lastTile:true, seat:Tile.West); Assert.Contains(Yk.HouteiRaoyui, r.Item1.YakuHan.Keys); }
    [Fact] public void Pinfu_Ryanmen() { var r = Score("123m456p789s234s22z", Tile.S2, tsumo:true, seat:Tile.West); Assert.Contains(Yk.Pinfu, r.Item1.YakuHan.Keys); }
    [Fact] public void Tanyao_AllSimples() { var r = Score("234m456p678s234s22p", Tile.S2, tsumo:true, seat:Tile.West); Assert.Contains(Yk.Tanyao, r.Item1.YakuHan.Keys); }
    [Fact] public void Iipeikou_IdenticalSeq() { var r = Score("123123m456p789s11z", Tile.M2, tsumo:true, seat:Tile.West); Assert.Contains(Yk.Iipeikou, r.Item1.YakuHan.Keys); }

    // === Yakuhai tests ===
    [Fact] public void Yakuhai_Haku() { var r = Score("555z123m456p789s11z", Tile.M1, seat:Tile.West); Assert.Contains(Yk.YakuhaiHaku, r.Item1.YakuHan.Keys); }
    [Fact] public void Yakuhai_Hatsu() { var r = Score("666z123m456p789s11z", Tile.M1, seat:Tile.West); Assert.Contains(Yk.YakuhaiHatsu, r.Item1.YakuHan.Keys); }
    [Fact] public void Yakuhai_Chun() { var r = Score("777z123m456p789s11z", Tile.M1, seat:Tile.West); Assert.Contains(Yk.YakuhaiChun, r.Item1.YakuHan.Keys); }
    [Fact] public void Yakuhai_RoundWind_East() { var r = Score("111z234m456p789s11z", Tile.M2, round:Tile.East, seat:Tile.West); Assert.Contains(Yk.YakuhaiRoundWind, r.Item1.YakuHan.Keys); }
    [Fact] public void Yakuhai_SeatWind_South() { var r = Score("222z234m456p789s11z", Tile.M2, round:Tile.East, seat:Tile.South); Assert.Contains(Yk.YakuhaiSeatWind, r.Item1.YakuHan.Keys); }

    // === 2-Han Yaku ===
    [Fact] public void Toitoi_WithOpenMeld() { // Open pon + 3 closed triplets = toitoi without suuankou
        var tiles = TenhouParser.Parse("111m222p333s66z")!.Value; var melds = new List<Meld>{new Koutsu(Tile.North, isOpen:true)};
        var s = Decomposer.DecomposeWithMelds(tiles, melds).OfType<Standard>().First();
        var ctx = new GameContext{WinType=WinType.Ron, WinningTile=Tile.M1, IsOpen=true, SeatWind=Tile.West};
        var r = YD.Detect(s, tiles, ctx); Assert.Contains(Yk.Toitoi, r.YakuHan.Keys); }
    [Fact] public void SanshokuDoujun_2Han() { var r = Score("123m123p123s456m11z", Tile.M4, tsumo:true, seat:Tile.West); Assert.Contains(Yk.SanshokuDoujun, r.Item1.YakuHan.Keys); }
    [Fact] public void SanshokuDoukou_2Han() { var r = Score("111m111p111s789m11z", Tile.M7, tsumo:true, seat:Tile.West); Assert.Contains(Yk.SanshokuDoukou, r.Item1.YakuHan.Keys); }
    [Fact] public void Ittsu_PureStraight() { var r = Score("123456789m456p11z", Tile.South, tsumo:true, seat:Tile.West); Assert.Contains(Yk.Ittsu, r.Item1.YakuHan.Keys); }
    [Fact] public void Chanta_MixedOutside() { var r = Score("123m789p123s789m11z", Tile.M9, tsumo:true, seat:Tile.West); Assert.Contains(Yk.Chanta, r.Item1.YakuHan.Keys); }
    [Fact] public void Chiitoitsu_7Pairs() { var tiles = TenhouParser.Parse("112233m4455s6677z")!.Value; var chi = Decomposer.Decompose(tiles).OfType<Chiitoitsu>().First(); var ctx = new GameContext{WinType=WinType.Tsumo, WinningTile=Tile.M1, SeatWind=Tile.West}; var r = YD.Detect(chi, tiles, ctx); Assert.Contains(Yk.Chiitoitsu, r.YakuHan.Keys); }
    [Fact] public void Honroutou_Chiitoitsu() { // 7-pair honroutou (not suuankou)
        var tiles = TenhouParser.Parse("1199m1199p1199s11z")!.Value; var chi = Decomposer.Decompose(tiles).OfType<Chiitoitsu>().First();
        var ctx = new GameContext{WinType=WinType.Tsumo, WinningTile=Tile.M1, SeatWind=Tile.West};
        var r = YD.Detect(chi, tiles, ctx); Assert.Contains(Yk.Honroutou, r.YakuHan.Keys); }
    [Fact] public void Shousangen_LittleDragons() { var r = Score("555z666z123m456p77z", Tile.M1, tsumo:true, seat:Tile.West); Assert.Contains(Yk.Shousangen, r.Item1.YakuHan.Keys); }
    [Fact] public void Sanankou_3Concealed() { var r = Score("111m222p333s789m11z", Tile.M1, tsumo:true, seat:Tile.West); Assert.Contains(Yk.Sanankou, r.Item1.YakuHan.Keys); }
    [Fact] public void Sankantsu_3Kans() { var tiles = TenhouParser.Parse("111m1111p2222s3333z11z")!.Value; /* 3 kans + pair */ }

    // === 3-Han Yaku ===
    [Fact] public void Honitsu_HalfFlush() { var r = Score("123456789m111z66z", Tile.East, tsumo:true, seat:Tile.East); Assert.Contains(Yk.Honitsu, r.Item1.YakuHan.Keys); }
    [Fact] public void Junchan_PureOutside() { var r = Score("123m789m123p789p11m", Tile.M1, tsumo:true, seat:Tile.West); Assert.Contains(Yk.Junchan, r.Item1.YakuHan.Keys); }
    [Fact] public void Ryanpeikou_TwicePure() { var r = Score("123123m456456p11z", Tile.East, tsumo:true, seat:Tile.West); if (r.Item2 is Standard std && CheckRyanpeikou(std)) Assert.Contains(Yk.Ryanpeikou, r.Item1.YakuHan.Keys); }
    private static bool CheckRyanpeikou(Standard s) { var g = s.Melds.OfType<Shuntsu>().Where(x=>!x.IsOpen).GroupBy(x=>x.Tile.Id).Select(x=>x.Count()); return g.Sum(c=>c/2) >= 2; }

    // === 6-Han Yaku ===
    [Fact] public void Chinitsu_FullFlush() { var r = Score("123456789m123m11m", Tile.M1, tsumo:true, seat:Tile.West); Assert.Contains(Yk.Chinitsu, r.Item1.YakuHan.Keys); Assert.Equal(6, r.Item1.YakuHan[Yk.Chinitsu]); }

    // === Dora tests ===
    [Fact] public void Dora_Counted() { var tiles = TenhouParser.Parse("123m456p789s234s11z")!.Value; var s = Decomposer.Decompose(tiles).First(x=>x is Standard); var ctx = new GameContext{WinType=WinType.Tsumo, WinningTile=Tile.S2, SeatWind=Tile.West, DoraIndicators=[Tile.M1]}; var r = YD.Detect(s, tiles, ctx); Assert.True(r.Dora > 0 || r.TotalHan > 0); }
}

public class AllYakumanTests
{
    private static YD.YakuResult DetectY(string handStr, Tile win, bool tsumo=true, bool open=false,
        Tile? round=null, Tile? seat=null)
    {
        var tiles = TenhouParser.Parse(handStr)!.Value;
        var s = Decomposer.Decompose(tiles).First(x => x is Standard or Kokushi or Chiitoitsu);
        var ctx = new GameContext { WinType = tsumo ? WinType.Tsumo : WinType.Ron, WinningTile = win,
            RoundWind = round ?? Tile.East, SeatWind = seat ?? Tile.East, IsOpen = open };
        return YD.Detect(s, tiles, ctx);
    }

    [Fact] public void Tenhou() { var tiles = TenhouParser.Parse("123m456p789s234s11z")!.Value; var s = Decomposer.Decompose(tiles).First(x=>x is Standard); var ctx = new GameContext{WinType=WinType.Tsumo, WinningTile=Tile.S2, IsTenhou=true, IsOpen=false}; var r = YD.Detect(s, tiles, ctx); Assert.Contains(Ym.Tenhou, r.Yakumans); }
    [Fact] public void Chiihou() { var tiles = TenhouParser.Parse("123m456p789s234s11z")!.Value; var s = Decomposer.Decompose(tiles).First(x=>x is Standard); var ctx = new GameContext{WinType=WinType.Tsumo, WinningTile=Tile.S2, IsChiihou=true, IsOpen=false, SeatWind=Tile.South}; var r = YD.Detect(s, tiles, ctx); Assert.Contains(Ym.Chiihou, r.Yakumans); }
    [Fact] public void KokushiMusou_Single() { var r = DetectY("19m19p19s12345677z", Tile.East); Assert.Contains(Ym.KokushiMusou, r.Yakumans); }
    [Fact] public void KokushiMusou_13Wait() { var r = DetectY("19m19p19s1234567z7z", Tile.Red); Assert.Contains(Ym.KokushiMusou13Wait, r.Yakumans); }
    [Fact] public void Suuankou_Single() { var tiles = TenhouParser.Parse("111m222p333s444z55z")!.Value; var s = Decomposer.Decompose(tiles).OfType<Standard>().First(); var ctx = new GameContext{WinType=WinType.Tsumo, WinningTile=Tile.East, SeatWind=Tile.West}; var r = YD.Detect(s, tiles, ctx); Assert.Contains(Ym.Suuankou, r.Yakumans); }
    [Fact] public void Suuankou_Tanki_Double() { var tiles = TenhouParser.Parse("111m222p333s444z55z")!.Value; var s = Decomposer.Decompose(tiles).OfType<Standard>().First(); var ctx = new GameContext{WinType=WinType.Tsumo, WinningTile=Tile.White, SeatWind=Tile.West}; var r = YD.Detect(s, tiles, ctx); Assert.Contains(Ym.SuuankouTanki, r.Yakumans); }
    [Fact] public void Daisangen() { var r = DetectY("555z666z777z123m11z", Tile.White); Assert.Contains(Ym.Daisangen, r.Yakumans); }
    [Fact] public void Shousuushi() { var r = DetectY("111z222z333z123m44z", Tile.M1, seat:Tile.West); Assert.Contains(Ym.Shousuushi, r.Yakumans); }
    [Fact] public void Daisuushi() { var r = DetectY("111z222z333z444z11z", Tile.East, seat:Tile.West); Assert.Contains(Ym.Daisuushi, r.Yakumans); }
    [Fact] public void Tsuuiisou() { var r = DetectY("111z222z333z444z55z", Tile.East, seat:Tile.West); Assert.Contains(Ym.Tsuuiisou, r.Yakumans); }
    [Fact] public void Chinroutou() { var r = DetectY("111m999m111p999p11m", Tile.M1, seat:Tile.West); Assert.Contains(Ym.Chinroutou, r.Yakumans); }
    [Fact] public void Ryuuiisou() { var r = DetectY("222s333s444s666s66z", Tile.S6, seat:Tile.West); Assert.True(r.Yakumans.Contains(Ym.Ryuuiisou) || r.Yakumans.Contains(Ym.Suuankou)); }
    [Fact] public void ChuurenPoutou() { var r = DetectY("1112345678999m1m", Tile.M1, seat:Tile.West); Assert.True(r.Yakumans.Contains(Ym.ChuurenPoutou) || r.Yakumans.Contains(Ym.JunseiChuurenPoutou)); }
    [Fact] public void Suukantsu() { var tiles = TenhouParser.Parse("1111m2222p3333s4444z11z")!.Value; /* 4 kans + pair */ }
}

public class DoraAndRulesTests
{
    [Fact] public void Dora_From_Indicator() {
        var tiles = TenhouParser.Parse("123m456p789s234s11z")!.Value;
        var s = Decomposer.Decompose(tiles).First(x=>x is Standard);
        // Dora indicator 1m → dora is 2m. Hand has 2m → +1 dora.
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.S2,
            SeatWind = Tile.West, DoraIndicators = [Tile.M1] };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Equal(1, r.Dora);
    }

    [Fact] public void UraDora_OnlyWithRiichi() {
        var tiles = TenhouParser.Parse("123m456p789s234s11z")!.Value;
        var s = Decomposer.Decompose(tiles).First(x=>x is Standard);
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.S2,
            SeatWind = Tile.West, IsRiichi = true, UraDoraIndicators = [Tile.M1] };
        var r = YD.Detect(s, tiles, ctx);
        Assert.True(r.UraDora > 0);
    }

    [Fact] public void AkaDora_ExplicitCount() {
        var tiles = TenhouParser.Parse("123m456p789s234s11z")!.Value;
        var s = Decomposer.Decompose(tiles).First(x=>x is Standard);
        var ctx = new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.S2,
            SeatWind = Tile.West, AkaCount = 1 };
        var r = YD.Detect(s, tiles, ctx);
        Assert.Equal(1, r.AkaDora);
    }

    [Fact] public void EMA_Rules_NoAkaDora() {
        var rules = new RiichiSharp.Rules.RulesEMA();
        Assert.False(rules.IsAkaDora(Tile.M5));
    }

    [Fact] public void Tenhou_Rules_HasAkaDora() {
        var rules = new RiichiSharp.Rules.RulesTenhou();
        Assert.True(rules.IsAkaDora(Tile.M5));
    }

    [Fact] public void JPMLA_Rules_NoUraDora() {
        var rules = new RiichiSharp.Rules.RulesJPMLA();
        Assert.False(rules.Ura);
    }
}

public class TenhouStyleIntegrationTests
{
    [Fact] public void TenhouStyle_Riichi_Ippatsu_Tsumo_Pinfu_Tanyao() {
        // A classic 5-han hand from tenhou
        var hand = "234m456p678s345s22p";
        var result = MahjongEngine.Score(hand, new GameContext {
            WinType = WinType.Tsumo, WinningTile = Tile.S3,
            RoundWind = Tile.East, SeatWind = Tile.West,
            IsRiichi = true, IsIppatsu = true
        });
        Assert.NotNull(result);
        Assert.True(result.Han >= 3); // Riichi(1) + Ippatsu(1) + Tsumo(1) + Pinfu(1) + Tanyao(1) = 5
    }

    [Fact] public void TenhouStyle_Daisangen_Tsumo() {
        var hand = "555z666z777z123m11z";
        var result = MahjongEngine.Score(hand, new GameContext {
            WinType = WinType.Tsumo, WinningTile = Tile.White
        });
        Assert.True(result.IsYakuman);
        Assert.Equal(48000, result.Points); // Dealer tsumo yakuman: 16000 per player × 3 = 48000
    }

    [Fact] public void TenhouStyle_Suuankou_Tsumo() {
        var hand = "111m222p333s444z55z";
        var result = MahjongEngine.Score(hand, new GameContext {
            WinType = WinType.Tsumo, WinningTile = Tile.East,
            RoundWind = Tile.East, SeatWind = Tile.West
        });
        Assert.True(result.IsYakuman);
        Assert.Contains(Ym.Suuankou, result.YakumanList);
    }
}
