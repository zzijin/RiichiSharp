namespace RiichiSharp.Scoring;

/// <summary>点数计算：从翻数+符数确定满贯等级，计算基本点和支付。</summary>
public static class ScoreCalculator
{
    /// <summary>确定满贯等级。</summary>
    public static ScoreLevel DetermineLevel(int han, int fu, bool hasYakuman, int yakumanUnits, bool kazoeYakuman, bool manganRound)
    {
        if (hasYakuman) return ScoreLevel.Yakuman(yakumanUnits);
        if (kazoeYakuman && han >= 13) return ScoreLevel.Yakuman(1);
        if (han >= 13 || yakumanUnits > 0) return ScoreLevel.Yakuman(yakumanUnits > 0 ? yakumanUnits : 1);
        if (han >= 11) return new ScoreLevel.Sanbaiman();  // 三倍满
        if (han >= 8) return new ScoreLevel.Baiman();      // 倍满
        if (han >= 6) return new ScoreLevel.Haneman();     // 跳满
        if (han >= 5) return new ScoreLevel.Mangan();      // 满贯
        if (han >= 4 && fu >= 40) return new ScoreLevel.Mangan();  // 4翻40符→满贯
        if (han >= 3 && fu >= 70) return new ScoreLevel.Mangan();  // 3翻70符→满贯
        return new ScoreLevel.Normal();
    }

    /// <summary>计算基本点（基本点）。</summary>
    public static int CalculateBasicPoints(int han, int fu, ScoreLevel level, bool manganRound)
        => level switch
        {
            ScoreLevel.Normal => CalculateNormalBasic(han, fu, manganRound),
            ScoreLevel.Mangan => 2000,
            ScoreLevel.Haneman => 3000,
            ScoreLevel.Baiman => 4000,
            ScoreLevel.Sanbaiman => 6000,
            ScoreLevel.YakumanLevel y => y.Units * 8000,
            _ => 0
        };

    private static int CalculateNormalBasic(int han, int fu, bool manganRound)
    {
        long basic = (long)fu * (1L << (han + 2));            // 基本公式: 符 × 2^(翻+2)
        if (basic > 2000) basic = 2000;
        if (manganRound && basic > 1900 && basic < 2000) basic = 2000; // 切上满贯: 1920→2000
        return (int)Math.Min(basic, 2000);
    }

    /// <summary>从基本点计算各玩家支付额。</summary>
    public static Payment CalculatePayment(int basicPoints, bool isDealer, bool isTsumo, int honba = 0, int riichiSticks = 0)
    {
        var payment = new Payment { BasicPoints = basicPoints, IsDealer = isDealer, IsTsumo = isTsumo };
        if (isTsumo)
        {
            if (isDealer)
            {   // 庄家自摸：每家付 basic×2
                int per = RoundUp100(basicPoints * 2);
                payment.Payments = new() { ["nonDealer"] = per + honba * payment.HonbaValue };
                payment.Total = (per + honba * payment.HonbaValue) * 3 + riichiSticks * 1000;
            }
            else
            {   // 闲家自摸：庄家付 basic×2，闲家付 basic
                int d = RoundUp100(basicPoints * 2), nd = RoundUp100(basicPoints);
                payment.Payments = new() { ["dealer"] = d + honba * payment.HonbaValue, ["nonDealer"] = nd + honba * payment.HonbaValue };
                payment.Total = d + nd * 2 + honba * payment.HonbaValue * 3 + riichiSticks * 1000;
            }
        }
        else
        {   // 荣和：庄家×6，闲家×4
            int mult = isDealer ? 6 : 4;
            int ronPay = RoundUp100(basicPoints * mult);
            payment.Payments = new() { ["ron"] = ronPay + honba * payment.HonbaValue * 3 };
            payment.Total = ronPay + honba * payment.HonbaValue * 3 + riichiSticks * 1000;
        }
        return payment;
    }

    private static int RoundUp100(int value) => value % 100 == 0 ? value : ((value / 100) + 1) * 100;
}

public abstract record ScoreLevel
{
    public sealed record Normal : ScoreLevel;
    public sealed record Mangan : ScoreLevel;
    public sealed record Haneman : ScoreLevel;
    public sealed record Baiman : ScoreLevel;
    public sealed record Sanbaiman : ScoreLevel;
    public sealed record YakumanLevel(int Units) : ScoreLevel;
    public static ScoreLevel Yakuman(int units) => new YakumanLevel(units);
}

public class Payment
{
    public int BasicPoints { get; set; }
    public bool IsDealer { get; set; }
    public bool IsTsumo { get; set; }
    public Dictionary<string, int> Payments { get; set; } = [];
    public int Total { get; set; }
    public int HonbaValue { get; set; } = 300;
}
