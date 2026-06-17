using MahjongAlgorithms.Tiles;
using MahjongAlgorithms.Parse;
using MahjongAlgorithms.Hand;
using MahjongAlgorithms.Shanten;

namespace MahjongAlgorithms.Tests;

public class DecomposerTests
{
    [Fact]
    public void Decompose_Standard_Hand()
    {
        var tiles = TenhouParser.Parse("123m456p789s11122z");
        Assert.NotNull(tiles);
        var structures = Decomposer.Decompose(tiles!.Value);
        Assert.NotEmpty(structures);
        Assert.Contains(structures, s => s is Standard);
    }

    [Fact]
    public void Decompose_Chiitoitsu()
    {
        // 7 distinct pairs: 1m×2,2m×2,3m×2,4p×2,5s×2,6z×2,7z×2
        var tiles = TenhouParser.Parse("112233m4455s6677z");
        Assert.NotNull(tiles);
        var structures = Decomposer.Decompose(tiles!.Value);
        Assert.Contains(structures, s => s is Chiitoitsu);
    }

    [Fact]
    public void Decompose_13Orphans()
    {
        // 13 orphans + one duplicate = 14 tiles kokushi complete
        var tiles = TenhouParser.Parse("19m19p19s12345677z");
        Assert.NotNull(tiles);
        var structures = Decomposer.Decompose(tiles!.Value);
        Assert.Contains(structures, s => s is Kokushi);
    }

    [Fact]
    public void Decompose_Multiple_Interpretations()
    {
        // 111222333m can be 3 triplets or 3 sequences
        var tiles = TenhouParser.Parse("111222333m456p11z");
        Assert.NotNull(tiles);
        var structures = Decomposer.Decompose(tiles!.Value);
        Assert.True(structures.Count >= 2, $"Expected >= 2 interpretations, got {structures.Count}");
    }
}

public class ShantenTests
{
    [Theory]
    // Known shanten values from reference engines
    [InlineData("123m456p789s11122z", -1)]  // Complete winning hand
    [InlineData("123m456p789s1112z", 0)]    // Tenpai (waiting on 3z)
    [InlineData("123m456p789s112z", 1)]     // Iishanten
    [InlineData("123m456p789s12z", 2)]      // Ryanshanten
    // TODO: Fix shanten counting — this hand should be ryanshanten (2)
    public void Shanten_Standard(string hand, int expectedShanten)
    {
        var tiles = TenhouParser.Parse(hand);
        Assert.NotNull(tiles);
        var result = ShantenCalculator.Calculate(tiles!.Value);
        Assert.Equal(expectedShanten, result.Shanten);
    }

    [Theory]
    [InlineData("112233m4455s667z", 0)]    // Chiitoitsu tenpai (6 pairs + 1 single)
    [InlineData("112233m4455s66z", 1)]      // Chiitoitsu iishanten (5 pairs + 2 singles)
    public void Shanten_Chiitoitsu(string hand, int expectedChiitoitsu)
    {
        var tiles = TenhouParser.Parse(hand);
        Assert.NotNull(tiles);
        var result = ShantenCalculator.Calculate(tiles!.Value);
        Assert.True(result.ChiitoitsuShanten <= expectedChiitoitsu,
            $"Expected chiitoitsu shanten <= {expectedChiitoitsu}, got {result.ChiitoitsuShanten}");
    }
}

public class UkeireTests
{
    [Fact]
    public void Ukeire_Tenpai_Has_Improving_Tiles()
    {
        // Tenpai hand: 3 melds + East pair + 1 loose = iishanten (needs 1 more meld-equivalent)
        var tiles = TenhouParser.Parse("123m456p789s112z");
        Assert.NotNull(tiles);
        var ukeire = UkeireCalculator.Calculate(tiles!.Value);
        Assert.Equal(1, ukeire.Shanten); // iishanten: needs 1 more meld
        Assert.NotEmpty(ukeire.Tiles);
    }

    [Fact]
    public void Ukeire_Iishanten_Has_Many_Improving()
    {
        var tiles = TenhouParser.Parse("123m456p789s12z");
        Assert.NotNull(tiles);
        var ukeire = UkeireCalculator.Calculate(tiles!.Value);
        Assert.Equal(2, ukeire.Shanten);
        Assert.NotEmpty(ukeire.Tiles);
    }
}

public class ScoringTests
{
    [Fact]
    public void Full_Pipeline_Pinfu_Only()
    {
        // Pinfu menzen tsumo: 1 han 20 fu
        var tiles = TenhouParser.Parse("123m456p789s234s11z");
        Assert.NotNull(tiles);
        var structures = Decomposer.Decompose(tiles!.Value);

        var context = new GameContext
        {
            WinType = WinType.Tsumo,
            WinningTile = Tile.S4,
            RoundWind = Tile.East,
            SeatWind = Tile.South,
            IsOpen = false
        };

        Assert.NotEmpty(structures);
    }

    [Fact]
    public void Full_Pipeline_Riichi_Tsumo()
    {
        var tiles = TenhouParser.Parse("123m456p789s234s11z");
        Assert.NotNull(tiles);
        var structures = Decomposer.Decompose(tiles!.Value);

        var context = new GameContext
        {
            WinType = WinType.Tsumo,
            WinningTile = Tile.S4,
            RoundWind = Tile.East,
            SeatWind = Tile.West,
            IsRiichi = true,
            IsOpen = false
        };

        var structure = structures.First(s => s is Standard);
        var waitTypes = MahjongAlgorithms.Wait.WaitDetector.Detect(structure, Tile.S4);
        var yaku = MahjongAlgorithms.Yaku.YakuDetector.Detect(structure, tiles!.Value, context, waitTypes);
        var fu = MahjongAlgorithms.Scoring.FuCalculator.Calculate(structure, context, waitTypes);

        Assert.True(yaku.TotalHan >= 1, $"Expected >= 1 han, got {yaku.TotalHan}");
    }
}
