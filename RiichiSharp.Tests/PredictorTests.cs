using System.Diagnostics;
using RiichiSharp.Tiles;
using RiichiSharp.WaitPredictor;

namespace RiichiSharp.Tests;

public class WaitPredictorTests
{
    [Fact]
    public void Predict_Basic()
    {
        var wall = new TileSet();
        for (int i = 0; i < 34; i++) wall[i] = 3;
        var river = new TileSet();

        var sw = Stopwatch.StartNew();
        var result = Predictor.Predict(wall, river);
        sw.Stop();

        Assert.True(result.TotalCombinations > 0);
        Assert.NotEmpty(result.Ranked);
        Assert.True(sw.Elapsed.TotalSeconds < 60);
    }

    [Fact]
    public void Predict_Furiten_Excluded()
    {
        var wall = new TileSet();
        for (int i = 0; i < 34; i++) wall[i] = 3;
        var river = new TileSet();
        river[Tile.M1.Id] = 2;

        var result = Predictor.Predict(wall, river);
        Assert.False(result.WaitCounts[Tile.M1.Id] > 0);
    }

    [Fact]
    public void Predict_Probability_Sorted()
    {
        var wall = new TileSet();
        for (int i = 0; i < 34; i++) wall[i] = 3;
        var river = new TileSet();

        var result = Predictor.Predict(wall, river);
        var ranked = result.Ranked;
        Assert.NotEmpty(ranked);
        for (int i = 1; i < ranked.Count; i++)
            Assert.True(ranked[i - 1].Prob >= ranked[i].Prob);
    }
}
