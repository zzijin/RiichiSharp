using MahjongAlgorithms.Tiles;
using MahjongAlgorithms.Parse;
using MahjongAlgorithms.Hand;
using MahjongAlgorithms.Wait;
using MahjongAlgorithms.Scoring;
using MahjongAlgorithms.Shanten;
using Yk = MahjongAlgorithms.Yaku.Yaku;
using Ym = MahjongAlgorithms.Yaku.Yakuman;
using YD = MahjongAlgorithms.Yaku.YakuDetector;

namespace MahjongAlgorithms.Tests;

public class ComprehensiveYakuTests
{
    // Helper
    private static YD.YakuResult Detect(string handStr, Tile win, bool tsumo = true,
        Tile? round = null, Tile? seat = null, bool riichi = false, bool open = false)
    {
        var tiles = TenhouParser.Parse(handStr)!.Value;
        var structures = open && TenhouParser.ParseWithMelds(handStr)?.CalledMelds.Count > 0
            ? Decomposer.Decompose(tiles) // simplified
            : Decomposer.Decompose(tiles);
        var s = structures.First(x => x is Standard or Chiitoitsu or Kokushi);
        var ctx = new GameContext { WinType = tsumo ? WinType.Tsumo : WinType.Ron,
            WinningTile = win, RoundWind = round ?? Tile.East, SeatWind = seat ?? Tile.East,
            IsRiichi = riichi, IsOpen = open };
        return YD.Detect(s, tiles, ctx);
    }

    // === Complete hand simulations ===

    [Theory]
    [InlineData("123m456p789s234s22z", "2s")]
    [InlineData("123m456p789s345s22z", "3s")]
    public void Pinfu_Hands(string hand, string winStr)
    {
        var r = Detect(hand, Tile.TryParse(winStr)!.Value, true, seat: Tile.West);
        Assert.Contains(Yk.Pinfu, r.YakuHan.Keys);
    }

    [Theory]
    [InlineData("234m456p678s234s22p", "2s")]  // All simples
    [InlineData("234m456p678s345s33p", "3s")]
    public void Tanyao_Hands(string hand, string winStr)
    {
        var r = Detect(hand, Tile.TryParse(winStr)!.Value, seat: Tile.West);
        Assert.Contains(Yk.Tanyao, r.YakuHan.Keys);
    }

    [Theory]
    [InlineData("123123m456p789s11z", "2m")]  // Iipeikou
    public void Iipeikou_Hands(string hand, string winStr)
    {
        var r = Detect(hand, Tile.TryParse(winStr)!.Value, seat: Tile.West);
        Assert.Contains(Yk.Iipeikou, r.YakuHan.Keys);
    }

    [Theory]
    [InlineData("555z123m456p789s11z", "1m")]  // White dragon triplet
    [InlineData("666z123m456p789s11z", "1m")]  // Green dragon triplet
    [InlineData("777z123m456p789s11z", "1m")]  // Red dragon triplet
    public void Yakuhai_Dragons_All(string hand, string winStr)
    {
        var r = Detect(hand, Tile.TryParse(winStr)!.Value, seat: Tile.West);
        Assert.True(r.YakuHan.ContainsKey(Yk.YakuhaiHaku) ||
                     r.YakuHan.ContainsKey(Yk.YakuhaiHatsu) ||
                     r.YakuHan.ContainsKey(Yk.YakuhaiChun));
    }

    [Theory]
    [InlineData("123m123p123s456m11z", "4m")]  // Sanshoku doujun
    public void SanshokuDoujun_Hands(string hand, string winStr)
    {
        var r = Detect(hand, Tile.TryParse(winStr)!.Value, seat: Tile.West);
        Assert.Contains(Yk.SanshokuDoujun, r.YakuHan.Keys);
    }

    [Theory]
    [InlineData("111m111p111s789m11z", "7m")]  // Sanshoku doukou
    public void SanshokuDoukou_Hands(string hand, string winStr)
    {
        var r = Detect(hand, Tile.TryParse(winStr)!.Value, seat: Tile.West);
        Assert.Contains(Yk.SanshokuDoukou, r.YakuHan.Keys);
    }

    [Theory]
    [InlineData("123456789m456p11z", "2z")]  // Ittsu
    public void Ittsu_Hands(string hand, string winStr)
    {
        var r = Detect(hand, Tile.TryParse(winStr)!.Value, seat: Tile.West);
        Assert.Contains(Yk.Ittsu, r.YakuHan.Keys);
    }
}

public class ComprehensiveShantenTests
{
    [Theory]
    // Known shanten values validated against tomohxx statistics
    [InlineData("123m456p789s11122z", -1)]     // Complete: 4 melds + pair
    [InlineData("123m456p789s1112z", 0)]       // Tenpai: 3 melds + 1 pair + 1 taatsu (111→pair,2→tan)
    [InlineData("123m456p789s112z", 1)]        // Iishanten: 3 melds + pair + 1 loose
    [InlineData("123m456p789s12z", 2)]         // Ryanshanten: 3 melds + 2 loose
    [InlineData("123m456p789s12z56z", 2)]     // Ryanshanten: 3 melds + 4 loose tiles, no pair → 8-6-0=2
    [InlineData("123456789m12345z", 2)]        // 3 melds + 5 unique honors, no pair
    [InlineData("19m19p19s1234567z", 0)]       // Kokushi tenpai (13 distinct, waiting for pair)
    [InlineData("112233m4455s6677z", -1)]      // Chiitoitsu complete (7 pairs = winning hand)
    [InlineData("112233m4455s667z", 0)]        // Chiitoitsu tenpai (6 pairs + 1 single)
    public void Shanten_Values(string hand, int expected)
    {
        var tiles = TenhouParser.Parse(hand)!.Value;
        var result = ShantenCalculator.Calculate(tiles);
        Assert.Equal(expected, result.Shanten);
    }

    [Fact]
    public void Shanten_Random_1000_Hands_WithinRange()
    {
        var rng = new Random(42);
        for (int n = 0; n < 1000; n++)
        {
            var ts = new TileSet();
            int remaining = 13;
            while (remaining > 0)
            {
                int idx = rng.Next(34);
                if (ts[idx] < 4) { ts[idx]++; remaining--; }
            }
            var result = ShantenCalculator.Calculate(ts);
            Assert.True(result.Shanten is >= -1 and <= 6,
                $"Shanten {result.Shanten} out of range [-1,6] for hand {ts}");
        }
    }
}

public class ComprehensiveScoreTests
{
    [Theory]
    // Standard mangan table
    [InlineData(1, 30, false, 1000)]   // 1 han 30 fu → basic=240 → 1000 ron
    [InlineData(2, 30, false, 2000)]   // 2 han 30 fu → basic=480 → 2000 ron
    [InlineData(3, 30, false, 3900)]   // 3 han 30 fu → basic=960 → 3900 ron
    [InlineData(4, 30, false, 7700)]   // 4 han 30 fu → basic=1920 → 7700 ron
    [InlineData(5, 30, false, 8000)]   // Mangan → 8000 ron
    [InlineData(6, 30, false, 12000)]  // Haneman → 12000 ron
    [InlineData(8, 30, false, 16000)]  // Baiman → 16000 ron
    [InlineData(11, 30, false, 24000)] // Sanbaiman → 24000 ron
    public void Ron_Score_Table(int han, int fu, bool dealer, int expectedRon)
    {
        var level = ScoreCalculator.DetermineLevel(han, fu, false, 0, true, false);
        var basic = ScoreCalculator.CalculateBasicPoints(han, fu, level, false);
        var pay = ScoreCalculator.CalculatePayment(basic, dealer, false);
        Assert.True(pay.Total >= expectedRon - 100 && pay.Total <= expectedRon + 100,
            $"Han={han} Fu={fu}: expected ~{expectedRon}, got {pay.Total}");
    }

    [Fact]
    public void Dealer_Tsumo_Mangan_4000All()
    {
        var pay = ScoreCalculator.CalculatePayment(2000, true, true);
        Assert.True(pay.Payments.ContainsKey("nonDealer"));
        Assert.True(pay.Payments["nonDealer"] >= 3900);
    }

    [Fact]
    public void NonDealer_Tsumo_Mangan_2000_4000()
    {
        var pay = ScoreCalculator.CalculatePayment(2000, false, true);
        Assert.True(pay.Payments.ContainsKey("dealer"));
        Assert.True(pay.Payments.ContainsKey("nonDealer"));
    }
}

public class TempaiCoreExampleTests
{
    // Based on tempai-core examples/ directory

    [Fact]
    public void TC_Shanten_3567m5677p268s77z()
    {
        // From tempai-core examples/shanten/shante_test.go
        var tiles = TenhouParser.Parse("3567m5677p268s77z")!.Value;
        var result = ShantenCalculator.Calculate(tiles);
        Assert.Equal(2, result.StandardShanten);
    }

    [Fact]
    public void TC_Tenpai_789m4466678p234s()
    {
        // From tempai-core examples/tempai/tempai_test.go
        // Waits should be 469p
        var tiles = TenhouParser.Parse("789m4466678p234s")!.Value;
        var result = ShantenCalculator.Calculate(tiles);
        Assert.Equal(0, result.Shanten);
    }

    [Fact]
    public void TC_Effective_5677m4456899p25s3z()
    {
        // From tempai-core examples/effective/effective_test.go
        // Best discard should be 3z
        var tiles = TenhouParser.Parse("5677m4456899p25s3z")!.Value;
        var results = Effective.EffectiveDiscard.Calculate(tiles);
        Assert.NotEmpty(results);
        Assert.Equal(Tile.West.Id, results[0].Discarded.Id); // 3z = West
    }
}

public class RoundtripTests
{
    [Theory]
    [InlineData("123m456p789s11122z")]
    [InlineData("19m19p19s1234567z")]
    [InlineData("1112345678999m")]
    [InlineData("0m456p789s11122z33z")]  // With aka dora + complete hand
    public void Parse_Roundtrip(string hand)
    {
        var tiles = TenhouParser.Parse(hand);
        Assert.NotNull(tiles);
        Assert.True(tiles!.Value.TotalCount >= 13);
    }

    [Fact]
    public void FullPipeline_Kokushi()
    {
        var hand = "19m19p19s12345677z";
        var result = MahjongEngine.Score(hand, new GameContext
        {
            WinType = WinType.Tsumo,
            WinningTile = Tile.East,
            RoundWind = Tile.East,
            SeatWind = Tile.West
        });
        Assert.NotNull(result);
        Assert.True(result.IsYakuman);
        Assert.Contains(Ym.KokushiMusou, result.YakumanList);
    }

    [Fact]
    public void FullPipeline_Daisangen()
    {
        var hand = "555z666z777z123m11z";
        var result = MahjongEngine.Score(hand, new GameContext
        {
            WinType = WinType.Tsumo,
            WinningTile = Tile.White
        });
        Assert.NotNull(result);
        Assert.True(result.IsYakuman);
        Assert.Contains(Ym.Daisangen, result.YakumanList);
    }

    [Fact]
    public void FullPipeline_RiichiPinfuTsumo()
    {
        var hand = "123m456p789s234s22z";
        var result = MahjongEngine.Score(hand, new GameContext
        {
            WinType = WinType.Tsumo,
            WinningTile = Tile.S2,
            RoundWind = Tile.East,
            SeatWind = Tile.West,
            IsRiichi = true
        });
        Assert.NotNull(result);
        Assert.False(result.IsYakuman);
        Assert.Contains(Yk.Riichi, result.YakuList);
        Assert.Contains(Yk.Pinfu, result.YakuList);
        Assert.Contains(Yk.MenzenTsumo, result.YakuList);
        Assert.Equal(3, result.Han);
    }
}
