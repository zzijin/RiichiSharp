using System.Xml.Linq;

namespace RiichiSharp.Validation;

using RiichiSharp.Parse;
using RiichiSharp.Tiles;

/// <summary>天凤牌谱（mjlog）验证器。解析天凤牌谱 XML，提取和了手牌并与本引擎计分结果交叉验证。</summary>
public static class TenhouReplayValidator
{
    /// <summary>单局验证结果。</summary>
    public record RoundResult
    {
        public string HandStr { get; init; } = "";
        public int ExpectedHan { get; init; }
        public int ExpectedFu { get; init; }
        public int ActualHan { get; init; }
        public int ActualFu { get; init; }
        public bool Match => ExpectedHan == ActualHan && ExpectedFu == ActualFu;
        public bool IsYakuman { get; init; }
        public override string ToString() => Match ? $"✅ {HandStr}: {ExpectedHan}翻{ExpectedFu}符" : $"❌ {HandStr}: 期望{ExpectedHan}翻{ExpectedFu}符 实际{ActualHan}翻{ActualFu}符";
    }

    /// <summary>验证结果汇总。</summary>
    public record ValidationReport
    {
        public int Total { get; set; }
        public int Matched { get; set; }
        public int Mismatched { get; set; }
        public double AccuracyPercent => Total == 0 ? 100 : 100.0 * Matched / Total;
        public List<RoundResult> Rounds { get; set; } = [];
        public List<RoundResult> Failures => Rounds.Where(r => !r.Match).ToList();
        public override string ToString() => $"验证 {Total} 局: 一致 {Matched}, 差异 {Mismatched} ({AccuracyPercent:F1}%)";
    }

    /// <summary>
    /// 从天凤 mjlog XML 文件验证计分。
    /// mjlog 格式: &lt;mjloggm ver="2.3"&gt;&lt;SHUFFLE .../&gt;&lt;INIT .../&gt;&lt;AGARI .../&gt;&lt;/mjloggm&gt;
    /// </summary>
    public static ValidationReport Validate(string mjlogXml)
    {
        var doc = XDocument.Parse(mjlogXml);
        var report = new ValidationReport();
        var rounds = new List<RoundResult>();

        foreach (var agari in doc.Descendants("AGARI"))
        {
            // 提取 AGARI 元素属性
            // ten="30,60,30" 每位玩家的累计得分
            // hai="1,2,3,4,5,6,7,8,9,..." 手牌+副露+和了牌
            // m="512,0,0" 副露信息
            // yaku="1,1,2,1,..." 役种信息 (id,han 对)
            // doraHai="60" 宝牌指示牌 ID
            // doraHaiUra="12" 里宝牌指示牌 ID
            // ba="0,0" 场风,本场
            // sc="0,0,0,0,96,0,48,256,48,128" 支付详情

            var haiAttr = agari.Attribute("hai")?.Value;
            var yakuAttr = agari.Attribute("yaku")?.Value;
            var doraAttr = agari.Attribute("doraHai")?.Value;
            var baAttr = agari.Attribute("ba")?.Value;

            if (haiAttr == null) continue;

            // 解析手牌
            var tiles = ParseTenhouHaiList(haiAttr);
            if (tiles == null) continue;

            // 解析期望的翻数和符数
            var (expectedHan, expectedFu, isYakuman) = ParseTenhouYaku(yakuAttr);

            // 用本引擎计分
            int actualHan = 0, actualFu = 0;
            // 注：完整计分需要更多的上下文（场风、自风、和了方式等），
            // 这里仅做框架搭建，具体计分逻辑需进一步填充

            rounds.Add(new RoundResult
            {
                HandStr = tiles.ToString()!,
                ExpectedHan = expectedHan,
                ExpectedFu = expectedFu,
                ActualHan = actualHan,
                ActualFu = actualFu,
                IsYakuman = isYakuman
            });
        }

        report.Rounds = rounds;
        report.Total = rounds.Count;
        report.Matched = rounds.Count(r => r.Match);
        report.Mismatched = rounds.Count(r => !r.Match);
        return report;
    }

    /// <summary>解析天凤手牌列表。天凤内部使用 0-135 的牌 ID（136 张）。</summary>
    private static TileSet? ParseTenhouHaiList(string haiStr)
    {
        // 天凤 hai 格式: "a,b,c,..." 逗号分隔的牌 ID (0-135)
        // 0-3=1m,4-7=2m,...,132-135=7z (Red)
        var ids = haiStr.Split(',').Select(s => int.TryParse(s.Trim(), out var id) ? id : -1).Where(id => id >= 0).ToList();
        if (ids.Count == 0) return null;

        var ts = new TileSet();
        foreach (int haiId in ids)
        {
            int tileIdx = TenhouHaiIdToTileIndex(haiId);
            if (tileIdx >= 0 && tileIdx < 34)
                ts[tileIdx]++;
        }
        return ts;
    }

    /// <summary>天凤牌 ID (0-135) → 本引擎牌下标 (0-33)。</summary>
    public static int TenhouHaiIdToTileIndex(int haiId)
    {
        if (haiId is >= 0 and <= 135)
        {
            // 天凤: 每4张连续ID对应一种牌
            // 0-3=m1, 4-7=m2,..., 32-35=m9
            // 36-39=p1,..., 68-71=p9
            // 72-75=s1,..., 104-107=s9
            // 108-111=E, 112-115=S, 116-119=W, 120-123=N
            // 124-127=White, 128-131=Green, 132-135=Red
            int tileType = haiId / 4;
            // 调整: m1(0-3) → idx 0, p1 → 9, s1 → 18, E → 27
            if (tileType <= 8) return tileType;              // 万子 0-8
            if (tileType <= 17) return tileType - 9 + 9;     // 筒子 9-17 → idx 9-17
            if (tileType <= 26) return tileType - 18 + 18;   // 索子 18-26 → idx 18-26
            if (tileType <= 33) return tileType - 27 + 27;   // 字牌 27-33 → idx 27-33
        }
        return -1;
    }

    /// <summary>解析天凤役种字符串。格式: "id1,han1,id2,han2,..."。</summary>
    private static (int han, int fu, bool isYakuman) ParseTenhouYaku(string? yakuStr)
    {
        if (string.IsNullOrEmpty(yakuStr)) return (0, 0, false);

        var parts = yakuStr.Split(',').Select(s => int.TryParse(s.Trim(), out var v) ? v : 0).ToList();
        int han = 0;
        bool isYakuman = false;

        for (int i = 0; i + 1 < parts.Count; i += 2)
        {
            int yakuId = parts[i];
            int yakuHan = parts[i + 1];
            // 天凤役满 ID 范围: 38-53
            if (yakuId >= 38) isYakuman = true;
            han += yakuHan;
        }

        // 符数需要从 sc 属性提取（暂简化）
        return (han, 30, isYakuman); // 默认30符
    }
}
