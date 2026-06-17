# MahjongAlgorithms

日本立直麻将计分引擎（.NET 10）。算法综合参考 [agari](https://github.com/agari-industries/agari)（Rust）、[tempai-core](https://github.com/dnovikoff/tempai-core)（Go）、[tomohxx](https://github.com/tomohxx/mahjong-algorithms)（C++）三个开源项目。

## 功能

- **向听数计算** — 标准形（4面子+1雀头）、七对子、国士无双
- **进张分析** — 理论进张 / 实际进张（扣除可见牌）
- **切牌效率** — 逐牌切牌分析，按向听→进张数→牌优先级排序
- **役种判定** — 30 种常规役 + 16 种役满，含食下（开门降番）和冲突过滤
- **符计算** — 延单骑（裸单骑）边缘情况、双重风、开门最低30符
- **点数计算** — 满贯~役满、切上满贯、数え役满、庄闲拆分
- **规则可配** — EMA / Tenhou / JPML-A / JPML-B 预设 + 自定义 `IYakuRules` / `IScoreRules` 接口
- **天凤式牌谱解析** — `"123m456p789s11122z"` 格式，支持副露、赤宝牌（`0m`）、字母记法

## 快速开始

```csharp
using MahjongAlgorithms;

// 一行计分
var result = MahjongEngine.Score("123m456p789s11122z", new GameContext
{
    WinType = WinType.Tsumo,
    WinningTile = Tile.East,
    RoundWind = Tile.East,
    SeatWind = Tile.South,
    IsRiichi = true
});
Console.WriteLine(result);  // → 4翻40符: 立直, 门前清自摸和, 平和, 一盃口

// 向听数
var s = MahjongEngine.Shanten("123m456p789s1112z");
Console.WriteLine(s.Shanten);  // → 0（听牌）

// 切牌分析
var discards = MahjongEngine.EffectiveDiscards("5677m4456899p25s3z");
Console.WriteLine($"最佳切牌: {discards[0].Discarded}");  // → W
```

## 构建

```bash
cd MahjongAlgorithms
dotnet build

# 运行测试
cd MahjongAlgorithms.Tests
dotnet test   # 188 个测试
```

需要 .NET 10 SDK。零外部依赖（仅 System.*）。

## API 概览

| 方法 | 说明 |
|------|------|
| `MahjongEngine.Score(handStr, context)` | 完整计分：分解 → 役种 → 符 → 支付 |
| `MahjongEngine.Shanten(handStr)` | 向听数（-1=和了, 0=听牌, 1+=向听） |
| `MahjongEngine.Ukeire(handStr)` | 改良牌列表，含剩余枚数 |
| `MahjongEngine.EffectiveDiscards(handStr)` | 切牌分析，按最优切牌排序 |

## 架构

```
Tile/        → Tile 牌结构体（34常量）, TileSet（int[34]）, Suit 花色枚举
Parse/       → TenhouParser：字符串 → TileSet + 副露
Hand/        → Meld 面子, HandStructure 手牌结构, Decomposer 递归分解器
Wait/        → WaitType 听牌类型（6种）, WaitDetector, 平和判定
Yaku/        → Yaku 役种枚举（30种）+ Yakuman 役满枚举（16种）, YakuDetector, 冲突过滤
Scoring/     → FuCalculator 符计算（含延单骑）, ScoreCalculator 点数计算, Payment 支付
Shanten/     → ShantenCalculator 递归向听数 + UkeireCalculator 进张
Effective/   → EffectiveDiscard 切牌效率分析（排序输出）
Rules/       → IYakuRules + IScoreRules 规则接口, 4套预设（EMA/Tenhou/JPML）
```

## 许可

MIT
