using System.Diagnostics;
using RiichiSharp.Tiles;
using RiichiSharp.Parse;
using RiichiSharp.Shanten;
using RiichiSharp.Hand;

namespace RiichiSharp.Tests;

/// <summary>
/// Performance benchmarks for core algorithms.
/// Not unit tests — run manually to measure throughput.
/// </summary>
public class Benchmarks
{
    // Reference: tempai-core benchmarks
    // Shanten: ~73K ops/sec (13.6 us/op)
    // Tenpai:  ~119K ops/sec (6.0 us/op)
    // Effective: ~8K ops/sec (123.7 us/op)
    // (on i7-1260P, Go implementation)

    [Fact]
    public void Benchmark_Shanten_RandomHands()
    {
        var rng = new Random(123);
        var hands = GenerateRandomHands(1000, rng);
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < hands.Count; i++)
            ShantenCalculator.Calculate(hands[i]);

        sw.Stop();
        double usPerOp = sw.Elapsed.TotalMilliseconds * 1000.0 / hands.Count;
        // .NET should be reasonably fast. Log the result.
        Assert.True(usPerOp < 1000, $"Shanten too slow: {usPerOp:F1} μs/op");
    }

    [Fact]
    public void Benchmark_Decompose_StandardHands()
    {
        var rng = new Random(123);
        var hands = GenerateRandomHands(100, rng);
        var sw = Stopwatch.StartNew();

        int totalStructures = 0;
        for (int i = 0; i < hands.Count; i++)
        {
            var s = Decomposer.Decompose(hands[i]);
            totalStructures += s.Count;
        }

        sw.Stop();
        double usPerOp = sw.Elapsed.TotalMilliseconds * 1000.0 / hands.Count;
        Assert.True(usPerOp < 2000, $"Decompose too slow: {usPerOp:F1} μs/op");
    }

    [Fact]
    public void Benchmark_FullScoring()
    {
        var rng = new Random(123);
        var sw = Stopwatch.StartNew();
        int scored = 0;

        for (int i = 0; i < 50; i++)
        {
            // Generate a valid-looking hand
            var tiles = GenerateValidHand(rng);
            if (tiles == null) continue;

            try
            {
                var result = MahjongEngine.Score(
                    tiles.ToString()!,
                    new GameContext { WinType = WinType.Tsumo, WinningTile = Tile.M1 });
                scored++;
            }
            catch { /* skip invalid hands */ }
        }

        sw.Stop();
        double msPerOp = sw.Elapsed.TotalMilliseconds / Math.Max(1, scored);
        Assert.True(scored > 0, "No hands could be scored");
        Assert.True(msPerOp < 100, $"Full scoring too slow: {msPerOp:F1} ms/op (scored {scored})");
    }

    [Fact]
    public void Benchmark_Ukeire_TenpaiHand()
    {
        var tiles = TenhouParser.Parse("123m456p789s112z")!.Value;
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 1000; i++)
            UkeireCalculator.Calculate(tiles);

        sw.Stop();
        double usPerOp = sw.Elapsed.TotalMilliseconds * 1000.0 / 1000;
        Assert.True(usPerOp < 500, $"Ukeire too slow: {usPerOp:F1} μs/op");
    }

    // Helpers
    private static List<TileSet> GenerateRandomHands(int count, Random rng)
    {
        var hands = new List<TileSet>();
        for (int n = 0; n < count; n++)
        {
            var ts = new TileSet();
            int toGenerate = 13; // standard 13-tile hand for shanten
            while (toGenerate > 0)
            {
                int idx = rng.Next(34);
                if (ts[idx] < 4)
                {
                    ts[idx]++;
                    toGenerate--;
                }
            }
            hands.Add(ts);
        }
        return hands;
    }

    private static TileSet? GenerateValidHand(Random rng)
    {
        // Build a plausible winning hand: 4 melds + 1 pair
        var ts = new TileSet();

        // Add a pair
        int pairIdx = rng.Next(34);
        ts[pairIdx] = 2;

        // Add 4 melds
        for (int m = 0; m < 4; m++)
        {
            if (rng.Next(2) == 0)
            {
                // Koutsu
                int idx = rng.Next(34);
                if (ts[idx] + 3 <= 4) ts[idx] += 3;
                else m--; // retry
            }
            else
            {
                // Shuntsu — suited only, value 1-7
                int suitStart = rng.Next(3) * 9; // 0, 9, or 18
                int num = rng.Next(7); // 0-6 => tiles 1-7
                int i0 = suitStart + num;
                if (ts[i0] < 4 && ts[i0 + 1] < 4 && ts[i0 + 2] < 4)
                {
                    ts[i0]++;
                    ts[i0 + 1]++;
                    ts[i0 + 2]++;
                }
                else m--; // retry
            }
        }

        if (ts.TotalCount == 14) return ts;
        return null;
    }
}
