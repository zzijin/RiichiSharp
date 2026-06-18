namespace RiichiSharp.Yaku;

/// <summary>日麻全部常规役种（役），含固定翻数（门清/副露）。</summary>
public enum Yaku
{
    // --- 1翻 ---
    Riichi,              // 立直
    Ippatsu,             // 一发
    MenzenTsumo,         // 门前清自摸和
    Tanyao,              // 断幺九
    Pinfu,               // 平和
    Iipeikou,            // 一盃口
    YakuhaiHaku,         // 役牌·白
    YakuhaiHatsu,        // 役牌·发
    YakuhaiChun,         // 役牌·中
    YakuhaiRoundWind,    // 役牌·场风
    YakuhaiSeatWind,     // 役牌·自风
    RinshanKaihou,       // 岭上开花
    Chankan,             // 抢杠
    HaiteiRaoyue,        // 海底捞月
    HouteiRaoyui,        // 河底捞鱼

    // --- 2翻 ---
    DoubleRiichi,        // 两立直
    Toitoi,              // 对对和
    SanshokuDoujun,      // 三色同顺
    SanshokuDoukou,      // 三色同刻
    Ittsu,               // 一气通贯
    Chiitoitsu,          // 七对子
    Chanta,              // 混全带幺九
    Sanankou,            // 三暗刻
    Sankantsu,           // 三杠子
    Honroutou,           // 混老头
    Shousangen,          // 小三元

    // --- 3翻 ---
    Ryanpeikou,          // 二盃口
    Junchan,             // 纯全带幺九
    Honitsu,             // 混一色

    // --- 6翻（门清）/ 5翻（副露）---
    Chinitsu,            // 清一色

    // --- 宝牌（不计入役种，但计入结果）---
    Dora,                // 宝牌
    UraDora,             // 里宝牌
    AkaDora,             // 赤宝牌
}

/// <summary>日麻全部役满。</summary>
public enum Yakuman
{
    Tenhou,              // 天和
    Chiihou,             // 地和
    KokushiMusou,        // 国士无双
    KokushiMusou13Wait,  // 国士无双十三面待
    Suuankou,            // 四暗刻
    SuuankouTanki,       // 四暗刻单骑
    Daisangen,           // 大三元
    Shousuushi,          // 小四喜
    Daisuushi,           // 大四喜
    Tsuuiisou,           // 字一色
    Chinroutou,          // 清老头
    Ryuuiisou,           // 绿一色
    ChuurenPoutou,       // 九莲宝灯
    JunseiChuurenPoutou, // 纯正九莲宝灯
    Suukantsu,           // 四杠子
    Renhou,              // 人和
}

/// <summary>返回役种的翻数（门清/副露）。副露无效役种返回 null。</summary>
public static class YakuHan
{
    public static int? GetHan(this Yaku yaku, bool isOpen)
    {
        // 这些役种副露时无效
        if (isOpen && yaku is Yaku.Riichi or Yaku.Ippatsu or Yaku.MenzenTsumo or Yaku.Pinfu
            or Yaku.Iipeikou or Yaku.DoubleRiichi or Yaku.Ryanpeikou or Yaku.Chiitoitsu)
            return null;

        return yaku switch
        {
            // 1翻
            Yaku.Riichi => 1,
            Yaku.Ippatsu => 1,
            Yaku.MenzenTsumo => 1,
            Yaku.Tanyao => 1,
            Yaku.Pinfu => 1,
            Yaku.Iipeikou => 1,
            Yaku.YakuhaiHaku => 1,
            Yaku.YakuhaiHatsu => 1,
            Yaku.YakuhaiChun => 1,
            Yaku.YakuhaiRoundWind => 1,
            Yaku.YakuhaiSeatWind => 1,
            Yaku.RinshanKaihou => 1,
            Yaku.Chankan => 1,
            Yaku.HaiteiRaoyue => 1,
            Yaku.HouteiRaoyui => 1,

            // 2翻（部分有食下）
            Yaku.DoubleRiichi => 2,
            Yaku.Toitoi => 2,
            Yaku.SanshokuDoujun => isOpen ? 1 : 2,   // 食下 2→1
            Yaku.SanshokuDoukou => 2,
            Yaku.Ittsu => isOpen ? 1 : 2,             // 食下 2→1
            Yaku.Chiitoitsu => 2,
            Yaku.Chanta => isOpen ? 1 : 2,            // 食下 2→1
            Yaku.Sanankou => 2,
            Yaku.Sankantsu => 2,
            Yaku.Honroutou => 2,
            Yaku.Shousangen => 2,

            // 3翻（部分有食下）
            Yaku.Ryanpeikou => 3,
            Yaku.Junchan => isOpen ? 2 : 3,           // 食下 3→2
            Yaku.Honitsu => isOpen ? 2 : 3,           // 食下 3→2

            // 6/5翻
            Yaku.Chinitsu => isOpen ? 5 : 6,          // 食下 6→5

            // 宝牌
            Yaku.Dora => 1,
            Yaku.UraDora => 1,
            Yaku.AkaDora => 1,

            _ => 0
        };
    }

    /// <summary>获取役满的单位数（双倍役满返回2）。</summary>
    public static int GetYakumanUnits(this Yakuman yakuman, bool isDouble)
    {
        return yakuman switch
        {
            Yakuman.KokushiMusou13Wait => 2,
            Yakuman.SuuankouTanki => 2,
            Yakuman.Daisuushi => 2,
            Yakuman.JunseiChuurenPoutou => 2,
            _ => isDouble ? 2 : 1
        };
    }
}
