# RiichiSharp

日本立直麻将计分引擎（.NET 8 / .NET 10）。算法综合参考 [agari](https://github.com/agari-industries/agari)（Rust）、[tempai-core](https://github.com/dnovikoff/tempai-core)（Go）、[tomohxx](https://github.com/tomohxx/mahjong-algorithms)（C++）、[pyriichi](https://github.com/d4n1elchen/pyriichi)（Python）四个开源项目。

## 功能

| 模块 | 功能 | 参考源 |
|------|------|--------|
| **向听数** | 标准形/七对子/国士无双，递归回溯算法 | agari |
| **进张分析** | 理论进张 / 实际进张（扣除可见牌），改良牌列表 | agari |
| **切牌效率** | 逐牌分析，按向听→进张→牌优先级排序 | tempai-core |
| **役种判定** | 30种常规役 + 16种役满，食下（开门降番），冲突过滤 | agari + tempai-core |
| **符计算** | 延单骑边缘情况，双重风，开门最低30符，平和自摸20符 | agari |
| **点数计算** | 满贯~役满，切上满贯，数え役满，庄闲拆分，本场/立直棒 | agari + tempai-core |
| **可配规则** | EMA / Tenhou / JPML-A / JPML-B 预设 + 自定义接口 | tempai-core |
| **振听检测** | 舍牌振听 / 同巡振听 / 立直永久振听，三级 | 自研 |
| **独立听牌计算器** | 单次面子枚举完成听牌+待牌列表 | tempai-core |
| **对手预测** | 组合枚举听牌形，从牌山+牌河推断对手待牌概率分布 | tomohxx |
| **牌谱解析** | 天凤式字符串，副露，赤宝牌（`0m`），字母记法 | agari |
| **牌谱验证** | mjlog XML 解析，天凤牌ID映射 | 自研 |

## 快速开始

```csharp
using RiichiSharp;

// 一行计分
var result = MahjongEngine.Score("123m456p789s11122z", new GameContext
{
    WinType = WinType.Tsumo, WinningTile = Tile.East,
    RoundWind = Tile.East, SeatWind = Tile.South, IsRiichi = true
});
Console.WriteLine(result);  // → 4翻40符: 立直, 门前清自摸和, 平和, 一盃口

// 向听数
var s = MahjongEngine.Shanten("123m456p789s1112z");
Console.WriteLine(s.Shanten);  // → 0（听牌）

// 切牌分析
var discards = MahjongEngine.EffectiveDiscards("5677m4456899p25s3z");
Console.WriteLine($"最佳切牌: {discards[0].Discarded}");  // → W

// 对手预测
var wall = new TileSet(); // 牌山每种牌剩余枚数
var river = new TileSet(); // 对手牌河
var pred = RiichiSharp.WaitPredictor.Predictor.Predict(wall, river);
foreach (var (tile, prob) in pred.Ranked.Take(5))
    Console.WriteLine($"{tile}: {prob:P1}");  // 对手最可能的5张待牌
```

## 构建

```bash
cd RiichiSharp
dotnet build
cd RiichiSharp.Tests
dotnet test   # 248 个测试
```

.NET 10 SDK。零外部依赖。

## 架构

```
Tile/          → Tile（34常量）, TileSet（int[34]）, Suit 枚举
Parse/         → TenhouParser：字符串 → TileSet + 副露
Hand/          → Meld, HandStructure, Decomposer 递归分解器
Wait/          → WaitType（6种）, WaitDetector, 平和判定
Yaku/          → 30役种 + 16役满枚举, YakuDetector, 冲突过滤
Scoring/       → FuCalculator（延单骑）, ScoreCalculator, Payment
Shanten/       → ShantenCalculator 递归向听 + UkeireCalculator 进张
Tenpai/        → 独立听牌计算器（单次面子枚举）
Effective/     → EffectiveDiscard 切牌效率（排序输出）
Furiten/       → FuritenDetector 振听三级检测
WaitPredictor/ → 对手待牌预测（组合枚举）
Validation/    → TenhouReplayValidator 天凤牌谱验证
Rules/         → IYakuRules + IScoreRules, 4套预设
GameContext.cs → 对局上下文（Builder 模式）
MahjongEngine.cs → 顶层便捷 API
```

## 测试覆盖

| 类别 | 数量 |
|------|:---:|
| 向听数/进张/随机验证 | 26 |
| 役种检测（1~6翻） | 29 |
| 役满检测 | 16 |
| 食下（开门降番） | 10 |
| 冲突过滤 | 3 |
| 符计算 | 15 |
| 点数/支付 | 13 |
| 切牌效率 | 4 |
| 听牌计算器 | 6 |
| 振听检测 | 9 |
| 对手预测 | 3 |
| 规则预设 | 8 |
| 基础类型/Parser | 10 |
| 手牌分解 | 8 |
| 完整流水线/天凤式 | 17 |
| 性能基准 | 4 |
| 随机验证(10000手) | 1 |
| 牌谱验证 | 7 |
| **总计** | **248** |

## 备选计划

- 天凤回放批量验证（精度量化）
- 向听数 O(1) 查表模式（蒙特卡洛场景）
- Nyanten 表压缩
- 三人麻将（拔北/特殊宝牌/自摸拆分）

## 许可

MIT
