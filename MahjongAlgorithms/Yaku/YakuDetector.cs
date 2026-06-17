namespace MahjongAlgorithms.Yaku;

using MahjongAlgorithms.Hand;
using MahjongAlgorithms.Tiles;
using MahjongAlgorithms.Wait;

/// <summary>役种检测器。先判役满，再判常规役种，含食下和冲突过滤。移植自 agari 的 yaku.rs。</summary>
public static class YakuDetector
{
    public record YakuResult { public Dictionary<Yaku, int> YakuHan { get; init; } = []; public List<Yakuman> Yakumans { get; init; } = []; public int TotalYakuHan { get; set; } public int Dora { get; set; } public int UraDora { get; set; } public int AkaDora { get; set; } public int TotalHan => TotalYakuHan + Dora + UraDora + AkaDora; public bool HasYakuman => Yakumans.Count > 0; public int YakumanUnits => Yakumans.Sum(y => y.GetYakumanUnits(false)); }

    public static YakuResult Detect(HandStructure structure, TileSet tiles, GameContext context, List<WaitType>? waitTypes = null)
    {
        var result = new YakuResult(); bool isOpen = context.IsOpen;
        waitTypes ??= WaitDetector.Detect(structure, context.WinningTile);
        DetectYakuman(structure, tiles, context, result);
        if (result.HasYakuman) return result;
        // 情况役
        if (context.IsRiichi && !isOpen) AddYaku(result, Yaku.Riichi, isOpen);
        if (context.IsDoubleRiichi && !isOpen) AddYaku(result, Yaku.DoubleRiichi, isOpen);
        if (context.IsIppatsu && !isOpen) AddYaku(result, Yaku.Ippatsu, isOpen);
        if (context.IsTsumo && !isOpen) AddYaku(result, Yaku.MenzenTsumo, isOpen);
        if (context.IsRinshan && context.IsTsumo) AddYaku(result, Yaku.RinshanKaihou, isOpen);
        if (context.IsChankan && context.IsRon) AddYaku(result, Yaku.Chankan, isOpen);
        if (context.IsLastTile) { if (context.IsTsumo) AddYaku(result, Yaku.HaiteiRaoyue, isOpen); else AddYaku(result, Yaku.HouteiRaoyui, isOpen); }
        // 人和（可配置）
        if (context.IsChankan && context.IsRon && !isOpen && !context.IsDealer && context.YakuRules.Renhou != Rules.ScoreLimit.None)
        { if (context.YakuRules.Renhou == Rules.ScoreLimit.Yakuman) result.Yakumans.Add(Yakuman.Renhou); }
        // 结构役
        switch (structure) { case Standard std: DetectStandardYaku(std, context, waitTypes, result); break; case Chiitoitsu chi: DetectChiitoitsuYaku(chi, result, isOpen); break; }
        ApplyConflictFiltering(result);
        CountDora(structure, context, result);
        return result;
    }

    // ============ 役满检测 ============
    private static void DetectYakuman(HandStructure structure, TileSet tiles, GameContext context, YakuResult result)
    {
        if (context.IsTenhou && context.IsTsumo && !context.IsOpen && context.IsDealer) result.Yakumans.Add(Yakuman.Tenhou);
        if (context.IsChiihou && context.IsTsumo && !context.IsOpen && !context.IsDealer) result.Yakumans.Add(Yakuman.Chiihou);
        switch (structure)
        {
            case Kokushi k: result.Yakumans.Add(k.Pair.Id == context.WinningTile.Id ? Yakuman.KokushiMusou13Wait : Yakuman.KokushiMusou); return;
        }
        if (structure is not Standard std) return;
        if (CheckSuuankou(std, context, out bool isTanki)) result.Yakumans.Add(isTanki ? Yakuman.SuuankouTanki : Yakuman.Suuankou);
        if (CheckDaisangen(std)) result.Yakumans.Add(Yakuman.Daisangen);
        int winds = CheckFourWinds(std);
        if (winds == 4) result.Yakumans.Add(Yakuman.Daisuushi);
        else if (winds == 3 && IsPairFourthWind(std)) result.Yakumans.Add(Yakuman.Shousuushi);
        if (CheckTsuuiisou(std)) result.Yakumans.Add(Yakuman.Tsuuiisou);
        if (CheckChinroutou(std)) result.Yakumans.Add(Yakuman.Chinroutou);
        if (CheckRyuuiisou(std, context.YakuRules)) result.Yakumans.Add(Yakuman.Ryuuiisou);
        if (CheckChuurenPoutou(std, tiles, context.WinningTile, out bool j)) result.Yakumans.Add(j ? Yakuman.JunseiChuurenPoutou : Yakuman.ChuurenPoutou);
        if (CheckSuukantsu(std)) result.Yakumans.Add(Yakuman.Suukantsu);
    }

    // ============ 常规役种 ============
    private static void DetectStandardYaku(Standard std, GameContext context, List<WaitType> waitTypes, YakuResult result)
    {
        bool isOpen = context.IsOpen;
        if (WaitDetector.IsPinfu(std, context.WinningTile, context, waitTypes)) AddYaku(result, Yaku.Pinfu, isOpen);
        if (CheckTanyao(std)) AddYaku(result, Yaku.Tanyao, isOpen);
        int peikou = CheckPeikou(std);
        if (peikou == 2 && !isOpen) AddYaku(result, Yaku.Ryanpeikou, isOpen);
        else if (peikou == 1 && !isOpen) AddYaku(result, Yaku.Iipeikou, isOpen);
        CheckYakuhai(std, context.RoundWind, context.SeatWind, result, isOpen);
        if (CheckToitoi(std)) AddYaku(result, Yaku.Toitoi, isOpen);
        if (CheckSanshokuDoujun(std)) AddYaku(result, Yaku.SanshokuDoujun, isOpen);
        if (CheckSanshokuDoukou(std)) AddYaku(result, Yaku.SanshokuDoukou, isOpen);
        if (CheckIttsu(std)) AddYaku(result, Yaku.Ittsu, isOpen);
        if (CheckChanta(std)) AddYaku(result, Yaku.Chanta, isOpen);
        if (CheckJunchan(std)) AddYaku(result, Yaku.Junchan, isOpen);
        if (CountConcealedTriplets(std, context) == 3) AddYaku(result, Yaku.Sanankou, isOpen);
        if (std.Melds.Count(m => m is Kan) == 3) AddYaku(result, Yaku.Sankantsu, isOpen);
        if (CheckHonroutou(std)) AddYaku(result, Yaku.Honroutou, isOpen);
        if (CheckShousangen(std)) AddYaku(result, Yaku.Shousangen, isOpen);
        int flush = CheckFlush(std);
        if (flush == 2) AddYaku(result, Yaku.Chinitsu, isOpen);
        else if (flush == 1) AddYaku(result, Yaku.Honitsu, isOpen);
    }

    private static void DetectChiitoitsuYaku(Chiitoitsu chi, YakuResult result, bool isOpen)
    {
        if (isOpen) return;
        AddYaku(result, Yaku.Chiitoitsu, isOpen);
        if (chi.Pairs.All(p => p.IsSimple)) AddYaku(result, Yaku.Tanyao, isOpen);
        if (chi.Pairs.All(p => p.IsTerminalOrHonor)) AddYaku(result, Yaku.Honroutou, isOpen);
        if (chi.Pairs.All(p => p.IsSuited && p.IsTerminal)) result.Yakumans.Add(Yakuman.Chinroutou);
        if (chi.Pairs.All(p => p.IsHonor)) result.Yakumans.Add(Yakuman.Tsuuiisou);
        var suits = chi.Pairs.Select(p => p.Suit).Distinct().ToList();
        if (suits.Count == 1 && suits[0] != Suit.Honor) AddYaku(result, Yaku.Chinitsu, isOpen);
        else if (suits.Count == 2 && suits.Contains(Suit.Honor)) AddYaku(result, Yaku.Honitsu, isOpen);
    }

    // ============ 辅助方法 ============
    private static void AddYaku(YakuResult r, Yaku y, bool isOpen) { int? h = y.GetHan(isOpen); if (h.HasValue) { r.YakuHan[y] = h.Value; r.TotalYakuHan += h.Value; } }

    private static bool CheckTanyao(Standard std) => std.Pair.IsSimple && std.Melds.All(m => m.Tiles.All(t => t.IsSimple));
    private static int CheckPeikou(Standard std) => std.Melds.OfType<Shuntsu>().Where(s => !s.IsOpen).GroupBy(s => s.Tile.Id).Select(g => g.Count()).Sum(c => c / 2);
    private static void CheckYakuhai(Standard std, Tile rw, Tile sw, YakuResult r, bool o) { foreach (var m in std.Melds) { if (m is not (Koutsu or Kan)) continue; if (m.Tile.Id == Tile.White.Id) AddYaku(r, Yaku.YakuhaiHaku, o); if (m.Tile.Id == Tile.Green.Id) AddYaku(r, Yaku.YakuhaiHatsu, o); if (m.Tile.Id == Tile.Red.Id) AddYaku(r, Yaku.YakuhaiChun, o); if (m.Tile.Id == rw.Id) AddYaku(r, Yaku.YakuhaiRoundWind, o); if (m.Tile.Id == sw.Id) AddYaku(r, Yaku.YakuhaiSeatWind, o); } }
    private static bool CheckToitoi(Standard std) => std.Melds.All(m => m is Koutsu or Kan);

    private static bool CheckSanshokuDoujun(Standard std) { var sh = std.Melds.OfType<Shuntsu>().ToList(); if (sh.Count < 3) return false; for (int n = 1; n <= 7; n++) if (sh.Any(s => s.Tile.Suit == Suit.Manzu && s.Tile.Number == n) && sh.Any(s => s.Tile.Suit == Suit.Pinzu && s.Tile.Number == n) && sh.Any(s => s.Tile.Suit == Suit.Souzu && s.Tile.Number == n)) return true; return false; }
    private static bool CheckSanshokuDoukou(Standard std) { var tr = std.Melds.Where(m => m is Koutsu or Kan).Select(m => m.Tile).ToList(); if (tr.Count < 3) return false; for (int n = 1; n <= 9; n++) if (tr.Any(t => t.Suit == Suit.Manzu && t.Number == n) && tr.Any(t => t.Suit == Suit.Pinzu && t.Number == n) && tr.Any(t => t.Suit == Suit.Souzu && t.Number == n)) return true; return false; }
    private static bool CheckIttsu(Standard std) { var sh = std.Melds.OfType<Shuntsu>().ToList(); foreach (Suit s in Enum.GetValues<Suit>()) { if (s == Suit.Honor) continue; if (sh.Any(x => x.Tile.Suit == s && x.Tile.Number == 1) && sh.Any(x => x.Tile.Suit == s && x.Tile.Number == 4) && sh.Any(x => x.Tile.Suit == s && x.Tile.Number == 7)) return true; } return false; }
    private static bool CheckChanta(Standard std) { if (!std.Pair.IsTerminalOrHonor || !std.Melds.Any(m => m is Shuntsu)) return false; foreach (var m in std.Melds) { if (m is Shuntsu s && !s.Tiles.Any(t => t.IsTerminal)) return false; if (m is (Koutsu or Kan) && !m.Tile.IsTerminalOrHonor) return false; } return true; }
    private static bool CheckJunchan(Standard std) { if (!std.Pair.IsSuited || !std.Pair.IsTerminal || !std.Melds.Any(m => m is Shuntsu)) return false; foreach (var m in std.Melds) { if (m is Shuntsu s && !s.Tiles.Any(t => t.IsTerminal)) return false; if (m is (Koutsu or Kan) && (!m.Tile.IsSuited || !m.Tile.IsTerminal)) return false; } return true; }

    /// <summary>计数暗刻（含暗杠和延单骑处理）。</summary>
    private static int CountConcealedTriplets(Standard std, GameContext? ctx = null)
    {
        int c = 0;
        foreach (var m in std.Melds)
        {
            if (m is Koutsu k) { if (k.IsOpen) continue; if (k.IsRon && ctx != null && !WaitDetector.WinningTileInClosedSequence(std, ctx.WinningTile)) continue; if (!k.IsRon || ctx != null) c++; }
            else if (m is Kan ka && ka.KanType == KanType.Closed) c++;
        }
        return c;
    }

    private static bool CheckHonroutou(Standard std) => std.Pair.IsTerminalOrHonor && std.Melds.All(m => m is not Shuntsu && m.Tile.IsTerminalOrHonor);
    private static bool CheckShousangen(Standard std) => std.Melds.Count(m => m is Koutsu or Kan && (m.Tile.Id == Tile.White.Id || m.Tile.Id == Tile.Green.Id || m.Tile.Id == Tile.Red.Id)) == 2 && (std.Pair.Id == Tile.White.Id || std.Pair.Id == Tile.Green.Id || std.Pair.Id == Tile.Red.Id);
    private static int CheckFlush(Standard std) { var suits = new HashSet<Suit>(); foreach (var m in std.Melds) suits.Add(m.Tile.Suit); suits.Add(std.Pair.Suit); if (suits.Count == 1 && !suits.Contains(Suit.Honor)) return 2; if (suits.Count == 2 && suits.Contains(Suit.Honor)) return 1; return 0; }

    // ============ 役满辅助 ============
    private static bool CheckSuuankou(Standard std, GameContext ctx, out bool isTanki) { isTanki = false; int c = CountConcealedTriplets(std, ctx); if (c != 4) return false; if (ctx.IsTsumo && ctx.WinningTile.Id == std.Pair.Id) isTanki = true; else if (ctx.IsRon) isTanki = true; return true; }
    private static bool CheckDaisangen(Standard std) => std.Melds.Count(m => m is Koutsu or Kan && (m.Tile.Id == Tile.White.Id || m.Tile.Id == Tile.Green.Id || m.Tile.Id == Tile.Red.Id)) == 3;
    private static int CheckFourWinds(Standard std) => std.Melds.Count(m => m is Koutsu or Kan && m.Tile.IsWind);
    private static bool IsPairFourthWind(Standard std) => std.Pair.IsWind && !std.Melds.Any(m => m is Koutsu or Kan && m.Tile.Id == std.Pair.Id);
    private static bool CheckTsuuiisou(Standard std) => std.Pair.IsHonor && std.Melds.All(m => m is not Shuntsu && m.Tile.IsHonor);
    private static bool CheckChinroutou(Standard std) => std.Pair.IsSuited && std.Pair.IsTerminal && std.Melds.All(m => m is not Shuntsu && m.Tile.IsSuited && m.Tile.IsTerminal);
    private static bool CheckRyuuiisou(Standard std, Rules.IYakuRules rules) => std.Pair.IsGreen && std.Melds.All(m => m is Shuntsu s ? (s.Tile.Suit == Suit.Souzu && s.Tile.Number == 2) : m.Tile.IsGreen);
    private static bool CheckChuurenPoutou(Standard std, TileSet tiles, Tile win, out bool isJunsei) { isJunsei = false; if (std.Melds.Any(m => m.IsOpen)) return false; Suit s = std.Pair.Suit; if (s == Suit.Honor || std.Melds.Any(m => m.Tile.Suit != s)) return false; var cnt = new int[10]; foreach (var m in std.Melds) foreach (var t in m.Tiles) if (t.Suit == s) cnt[t.Number]++; cnt[std.Pair.Number] += 2; if (cnt[1] < 3 || cnt[9] < 3) return false; for (int n = 2; n <= 8; n++) if (cnt[n] < 1) return false; isJunsei = true; return true; }
    private static bool CheckSuukantsu(Standard std) => std.Melds.Count(m => m is Kan) == 4;

    // ============ 冲突过滤 ============
    private static void ApplyConflictFiltering(YakuResult r)
    {
        if (r.YakuHan.ContainsKey(Yaku.Chinitsu)) r.YakuHan.Remove(Yaku.Honitsu);
        if (r.YakuHan.ContainsKey(Yaku.Ryanpeikou)) r.YakuHan.Remove(Yaku.Iipeikou);
        if (r.YakuHan.ContainsKey(Yaku.Junchan)) r.YakuHan.Remove(Yaku.Chanta);
        if (r.YakuHan.ContainsKey(Yaku.Toitoi)) { r.YakuHan.Remove(Yaku.Pinfu); r.YakuHan.Remove(Yaku.Iipeikou); r.YakuHan.Remove(Yaku.Ryanpeikou); r.YakuHan.Remove(Yaku.SanshokuDoujun); r.YakuHan.Remove(Yaku.Ittsu); }
        if (r.YakuHan.ContainsKey(Yaku.Tanyao)) { r.YakuHan.Remove(Yaku.Chanta); r.YakuHan.Remove(Yaku.Junchan); }
        r.TotalYakuHan = r.YakuHan.Values.Sum();
    }

    // ============ 宝牌计数 ============
    public static void CountDora(HandStructure structure, GameContext context, YakuResult result)
    {
        var doraTiles = context.DoraIndicators.Select(i => i.NextDora()).ToHashSet();
        foreach (var m in structure.Melds) foreach (var t in m.Tiles) if (doraTiles.Contains(t)) result.Dora++;
        if (doraTiles.Contains(structure.Pair)) result.Dora += 2;
        if (context.IsRiichi && context.YakuRules.Ura && context.UraDoraIndicators.Count > 0)
        {
            var ura = context.UraDoraIndicators.Select(i => i.NextDora()).ToHashSet();
            foreach (var m in structure.Melds) foreach (var t in m.Tiles) if (ura.Contains(t)) result.UraDora++;
            if (ura.Contains(structure.Pair)) result.UraDora += 2;
        }
        result.AkaDora = context.AkaCount;
    }
}
