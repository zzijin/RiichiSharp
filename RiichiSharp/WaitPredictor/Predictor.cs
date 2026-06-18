namespace RiichiSharp.WaitPredictor;

using RiichiSharp.Tiles;

/// <summary>对手待牌预测器。枚举所有可能的听牌手牌，用组合计数估算对手每种待牌的概率。
/// 参考 tomohxx waits-predictor 的组合枚举算法。</summary>
public static class Predictor
{
    // 组合数 C(n,k)，n,k ∈ [0,4]
    private static readonly long[,] Combin = new long[5, 5];
    static Predictor()
    {
        for (int n = 0; n <= 4; n++) { Combin[n, 0] = 1; Combin[n, n] = 1; }
        for (int n = 2; n <= 4; n++)
            for (int k = 1; k < n; k++)
                Combin[n, k] = Combin[n - 1, k - 1] + Combin[n - 1, k];
        SuitedStates = PrecomputeSuitedStates();
        HonorStates = PrecomputeHonorStates();
    }

    // === 状态表 ===
    private static readonly List<HandState> SuitedStates;  // 花色牌（9位置）
    private static readonly List<HandState> HonorStates;   // 字牌（7位置）

    /// <summary>手牌状态：手牌排列 + 待牌位掩码。</summary>
    private record HandState(byte[] Counts, uint WaitMask) // WaitMask: bit i = 牌 i 是待牌
    {
        public int TotalTiles => Counts.Sum(c => (int)c);
    }

    /// <summary>枚举所有花色牌（9位）的合法手牌排列并分类。</summary>
    private static List<HandState> PrecomputeSuitedStates()
    {
        var states = new List<HandState>();
        var hand = new byte[9];
        EnumerateSuited(hand, 0, 0, states);
        return states;
    }

    private static void EnumerateSuited(byte[] hand, int pos, int total, List<HandState> states)
    {
        if (pos == 9)
        {
            if (total is >= 0 and <= 14)
            {
                uint mask = GetWaitMask(hand, 9);
                if (mask != 0)
                    states.Add(new HandState((byte[])hand.Clone(), mask));
            }
            return;
        }
        int maxCount = Math.Min(4, 14 - total);
        for (byte c = 0; c <= maxCount; c++)
        {
            hand[pos] = c;
            int remaining = 14 - total - c;
            int minForRest = 0; // could be lower if we skip some
            int maxForRest = Math.Min((9 - pos - 1) * 4, remaining);
            if (minForRest <= maxForRest)
                EnumerateSuited(hand, pos + 1, total + c, states);
        }
    }

    /// <summary>枚举字牌（7位）的合法手牌排列并分类。</summary>
    private static List<HandState> PrecomputeHonorStates()
    {
        var states = new List<HandState>();
        var hand = new byte[7];
        EnumerateHonors(hand, 0, 0, states);
        return states;
    }

    private static void EnumerateHonors(byte[] hand, int pos, int total, List<HandState> states)
    {
        if (pos == 7)
        {
            if (total is >= 0 and <= 14)
            {
                uint mask = GetHonorWaitMask(hand);
                if (mask != 0)
                    states.Add(new HandState((byte[])hand.Clone(), mask));
            }
            return;
        }
        int maxCount = Math.Min(4, 14 - total);
        for (byte c = 0; c <= maxCount; c++)
        {
            hand[pos] = c;
            EnumerateHonors(hand, pos + 1, total + c, states);
        }
    }

    // === 手牌分类 ===

    /// <summary>检测花色牌手牌的待牌位掩码（标准形）。</summary>
    private static uint GetWaitMask(byte[] hand, int len)
    {
        uint mask = 0;
        // 尝试每种牌作为待牌（+1），检查是否能形成完整手牌
        for (int i = 0; i < len; i++)
        {
            if (hand[i] >= 4) continue;
            hand[i]++;
            if (IsWinningHandSuited(hand, len)) mask |= 1u << i;
            hand[i]--;
        }
        return mask;
    }

    /// <summary>检测字牌手牌的待牌位掩码。</summary>
    private static uint GetHonorWaitMask(byte[] hand)
    {
        uint mask = 0;
        for (int i = 0; i < 7; i++)
        {
            if (hand[i] >= 4) continue;
            hand[i]++;
            if (IsWinningHonor(hand)) mask |= 1u << i;
            hand[i]--;
        }
        return mask;
    }

    /// <summary>花色牌是否能分解为 3N+2（含雀头的完整手牌）？</summary>
    private static bool IsWinningHandSuited(byte[] hand, int len)
    {
        int total = hand.Sum(c => (int)c);
        if (total % 3 != 2) return false;
        // 尝试每种可能的雀头
        for (int p = 0; p < len; p++)
        {
            if (hand[p] < 2) continue;
            var h = (byte[])hand.Clone(); h[p] -= 2;
            if (CanFormMelds(h, len)) return true;
        }
        return false;
    }

    /// <summary>字牌是否能分解为完整手牌？每种字牌必须是0或3（刻子），恰好一种为2（雀头）。
    /// 总牌数必须是 3K+2。</summary>
    private static bool IsWinningHonor(byte[] hand)
    {
        int pairs = 0;
        for (int i = 0; i < 7; i++)
        {
            if (hand[i] == 2) pairs++;
            else if (hand[i] != 0 && hand[i] != 3) return false;
            else if (hand[i] >= 5) return false;
        }
        return pairs == 1;
    }

    /// <summary>花色牌剩余牌能全部形成面子（不含雀头）？贪心左到右扫描。</summary>
    private static bool CanFormMelds(byte[] hand, int len)
    {
        var h = (byte[])hand.Clone();
        for (int i = 0; i < len; i++)
        {
            int c = h[i] % 3;
            if (c == 0) continue;
            if (i + 2 >= len) return false; // 需要顺子但位置不够
            if (h[i + 1] < c || h[i + 2] < c) return false;
            h[i + 1] -= (byte)c;
            h[i + 2] -= (byte)c;
        }
        return true;
    }

    // === 主预测逻辑 ===

    /// <summary>待牌预测结果。</summary>
    public record PredictionResult
    {
        /// <summary>每种牌是对手待牌的组合数。</summary>
        public long[] WaitCounts { get; init; } = new long[34];
        /// <summary>总组合数。</summary>
        public long TotalCombinations { get; init; }
        /// <summary>每种牌是对手待牌的概率（0-1）。</summary>
        public double[] Probabilities => WaitCounts.Select(c => TotalCombinations > 0 ? (double)c / TotalCombinations : 0).ToArray();
        /// <summary>按概率降序排列的待牌。</summary>
        public List<(Tile Tile, double Prob)> Ranked => Enumerable.Range(0, 34)
            .Where(i => WaitCounts[i] > 0)
            .Select(i => (new Tile((byte)i), Probabilities[i]))
            .OrderByDescending(x => x.Item2).ToList();
        public override string ToString() => $"预测 {TotalCombinations} 组合, 最高: {string.Join(" ", Ranked.Take(5).Select(r => $"{r.Tile}:{r.Prob:P1}"))}";
    }

    /// <summary判断牌山和牌河，预测对手的待牌概率。</summary>
    /// <param name="wall">牌山剩余牌（每张牌的剩余枚数，0-4）</param>
    /// <param name="river">对手的牌河（弃牌）。对手不会听已在牌河中的牌（振听）。</param>
    public static PredictionResult Predict(TileSet wall, TileSet river)
    {
        var result = new PredictionResult();
        var waitCounts = new long[34];

        // 分区处理：万/筒/索/字
        var wallArr = wall.ToArray();
        var riverArr = river.ToArray();

        // 枚举花色手牌状态
        for (int suit = 0; suit < 3; suit++)
        {
            int start = suit * 9;
            foreach (var state in SuitedStates)
            {
                // 计算此手牌能从牌山中形成的组合数
                long combos = CountCombinations(state.Counts, wallArr, start, 9);
                if (combos == 0) continue;

                // 传播待牌
                uint mask = state.WaitMask;
                for (int i = 0; i < 9; i++)
                    if ((mask & (1u << i)) != 0 && riverArr[start + i] == 0) // 不在对手牌河中
                        waitCounts[start + i] += combos;
            }
        }

        // 枚举字牌手牌状态
        foreach (var state in HonorStates)
        {
            long combos = CountCombinations(state.Counts, wallArr, 27, 7);
            if (combos == 0) continue;

            uint mask = state.WaitMask;
            for (int i = 0; i < 7; i++)
                if ((mask & (1u << i)) != 0 && riverArr[27 + i] == 0)
                    waitCounts[27 + i] += combos;
        }

        long total = waitCounts.Sum();
        return new PredictionResult { WaitCounts = waitCounts, TotalCombinations = total };
    }

    /// <summary>计算从牌山剩余牌中形成手牌的组合数 = ∏ C(wall[t], hand[t])。</summary>
    private static long CountCombinations(byte[] hand, int[] wall, int start, int len)
    {
        long result = 1;
        for (int i = 0; i < len; i++)
        {
            int h = hand[i];
            if (h == 0) continue;
            int w = wall[start + i];
            if (w < h) return 0; // 牌山不够
            result *= Combin[w, h];
        }
        return result;
    }
}
