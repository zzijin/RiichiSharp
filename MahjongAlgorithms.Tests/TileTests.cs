using MahjongAlgorithms.Tiles;
using MahjongAlgorithms.Parse;

namespace MahjongAlgorithms.Tests;

public class TileTests
{
    [Fact]
    public void Tile_Properties_Correct()
    {
        Assert.True(Tile.M1.IsSuited);
        Assert.False(Tile.M1.IsHonor);
        Assert.Equal(1, Tile.M1.Number);
        Assert.Equal(Suit.Manzu, Tile.M1.Suit);

        Assert.True(Tile.East.IsHonor);
        Assert.True(Tile.East.IsWind);
        Assert.False(Tile.East.IsDragon);

        Assert.True(Tile.White.IsDragon);
        Assert.False(Tile.White.IsWind);

        Assert.True(Tile.M1.IsTerminal);
        Assert.True(Tile.M9.IsTerminal);
        Assert.False(Tile.M5.IsTerminal);
        Assert.True(Tile.M5.IsSimple);
    }

    [Fact]
    public void Tile_Dora_Next_Correct()
    {
        Assert.Equal(Tile.M2.Id, Tile.M1.NextDora().Id);
        Assert.Equal(Tile.M1.Id, Tile.M9.NextDora().Id); // wraps
        Assert.Equal(Tile.South.Id, Tile.East.NextDora().Id);
        Assert.Equal(Tile.West.Id, Tile.South.NextDora().Id);
        Assert.Equal(Tile.North.Id, Tile.West.NextDora().Id);
        Assert.Equal(Tile.East.Id, Tile.North.NextDora().Id);
        Assert.Equal(Tile.Green.Id, Tile.White.NextDora().Id);
        Assert.Equal(Tile.Red.Id, Tile.Green.NextDora().Id);
        Assert.Equal(Tile.White.Id, Tile.Red.NextDora().Id);
    }

    [Fact]
    public void Tile_Parse_Standard()
    {
        Assert.Equal(Tile.M1, Tile.TryParse("1m"));
        Assert.Equal(Tile.M9, Tile.TryParse("9m"));
        Assert.Equal(Tile.P5, Tile.TryParse("5p"));
        Assert.Equal(Tile.S1, Tile.TryParse("1s"));
        Assert.Equal(Tile.East, Tile.TryParse("1z"));
        Assert.Equal(Tile.South, Tile.TryParse("2z"));
        Assert.Equal(Tile.White, Tile.TryParse("5z"));
    }

    [Fact]
    public void Tile_Parse_Letters()
    {
        Assert.Equal(Tile.East, Tile.TryParse("e"));
        Assert.Equal(Tile.South, Tile.TryParse("s"));
        Assert.Equal(Tile.West, Tile.TryParse("w"));
        Assert.Equal(Tile.North, Tile.TryParse("n"));
        Assert.Equal(Tile.White, Tile.TryParse("wh"));
        Assert.Equal(Tile.Green, Tile.TryParse("g"));
        Assert.Equal(Tile.Red, Tile.TryParse("r"));
    }
}

public class TileSetTests
{
    [Fact]
    public void TileSet_Basic()
    {
        var ts = new TileSet();
        ts.Add(Tile.M1);
        Assert.Equal(1, ts[Tile.M1.Id]);
        Assert.Equal(1, ts.TotalCount);

        ts.Add(Tile.M1, 2);
        Assert.Equal(3, ts[Tile.M1.Id]);
    }

    [Fact]
    public void TileSet_PairCount()
    {
        var ts = new TileSet();
        ts[Tile.M1.Id] = 2;
        ts[Tile.M2.Id] = 2;
        ts[Tile.M3.Id] = 1;
        Assert.Equal(2, ts.PairCount);
        Assert.Equal(3, ts.UniqueCount);
    }
}

public class TenhouParserTests
{
    [Theory]
    [InlineData("123m456p789s11122z", 14)]
    [InlineData("1112345678999m", 13)]
    [InlineData("19m19p19s1234567z", 13)]
    public void Parse_Valid_Hand(string hand, int expectedCount)
    {
        var result = TenhouParser.Parse(hand);
        Assert.NotNull(result);
        Assert.Equal(expectedCount, result!.Value.TotalCount);
    }

    [Fact]
    public void Parse_With_Called_Melds()
    {
        var parsed = TenhouParser.ParseWithMelds("(123m)456m789p123s11z");
        Assert.NotNull(parsed);
        Assert.Single(parsed!.CalledMelds);
        Assert.Equal(MeldType.Chi, parsed.CalledMelds[0].Type);
    }

    [Fact]
    public void Parse_Roundtrip()
    {
        string hand = "123m456p789s11z";
        var result = TenhouParser.Parse(hand);
        Assert.NotNull(result);
        // Verify individual tiles
        Assert.Equal(1, result!.Value[Tile.M1.Id]);
        Assert.Equal(1, result.Value[Tile.M2.Id]);
        Assert.Equal(1, result.Value[Tile.M3.Id]);
        Assert.Equal(2, result.Value[Tile.East.Id]); // 11z = East pair
    }
}
