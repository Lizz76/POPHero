# POPHero

POPHero 是一个基于 Unity 2022.3 的弹球 Roguelike 战斗原型。玩家通过瞄准发射小球，让球在战斗区域内反弹并命中方块；方块会提供伤害、护盾、倍率等回合收益，再结合敌人接敌、双敌同场、商店、地图事件、删卡/工坊、Sticker 镶嵌和 Mod 规则，形成一局可迭代的爬塔式流程。

本文档用于帮助开发者或 AI 协作者快速接手当前项目。更细的历史说明可参考 [docs/AI_HANDOFF.md](docs/AI_HANDOFF.md)，但 README 以当前代码事实为准。

## 环境

- Unity：`2022.3.62f2c1`
- 主要 UI：uGUI Canvas + TextMeshPro
- 测试框架：Unity Test Framework EditMode
- MCP：项目已接入 `com.coplaydev.unity-mcp`，方便通过 Unity Editor 自动化检查场景、Console 和测试
- 主要运行场景：`Boot -> MainMenu -> Battle`

## 快速开始

1. 用 Unity Hub 打开项目根目录，例如：`E:\UnityProject\POPHero-main`
2. 打开 `Assets/Scenes/Boot.unity`
3. 运行 Play Mode，流程会进入主菜单，再进入 `Battle`
4. 如果只调战斗/UI，也可以直接打开 `Assets/Scenes/Battle.unity`

常用场景：

- `Assets/Scenes/Boot.unity`：启动入口
- `Assets/Scenes/MainMenu.unity`：主菜单
- `Assets/Scenes/Battle.unity`：当前正式战斗场景
- `Assets/Scenes/SampleScene.unity`：历史/调试残留，不作为正式入口

## 当前玩法概览

- 弹球战斗：玩家拖拽瞄准，绿色预测线展示弹射路线，球按路径进入战斗区域并命中方块。
- 方块收益：命中的 `Block` 会贡献伤害、护盾、倍率、金币或其他回合效果。
- 敌人接敌：近战敌人默认从 3 步外出生，逐步前进，贴脸后攻击。
- 飞行支援敌人：从第二场战斗开始，会出现一个后排飞行敌人；它不接敌，每回合从原点远程攻击。
- 双敌目标：玩家输出始终打离主角最近的存活敌人，伤害不溢出到另一只敌人。
- 起始方块：开局不再弹方块三选一，固定获得 1 张白色攻击方块。
- 普通战斗奖励：普通战斗清场后进入弹珠奖励，不再直接给方块。
- 方块获取：当前主要来自商店方块商品、路线工坊置换升级、Boss 战后方块三选一。
- Boss 续局：Boss 清场后给最低蓝色品质的方块三选一，选择后生成下一张地图并保留局内成长。
- 地图治疗：普通战斗后不再自动回满血，回血主要来自休息节点或路线事件选项。
- Sticker：可拖拽镶嵌到方块 socket 上，提供局部强化。
- Mod：全局规则/经济/信息展示等长期效果，和 Sticker 分层存在。
- GM 调试：Battle 中可用 `D` 键打开 GM 面板，里面有事件调试按钮，方便触发战斗、Boss、商店、工坊、回血和路线事件；GM Boss 秒杀同样会进入 Boss 方块奖励。

## 方块获取 v1

- 商店：`shop.csv` 里 `shop_block` 使用 `ShopSlotKind.Block` 固定生成 1 个方块商品。购买后优先加入上阵；上阵满了进仓库；上阵和仓库都满时购买失败且不扣金币。
- 工坊：`blockOperation.csv` 的 `map_workbench` 开启 `allowUpgrade`，关闭直接删除。玩家点某张已有方块的“升级”按钮后，会消耗原方块并生成随机类型、品质 +1 的新方块，最高保持 `Gold`，并替换回原来的上阵/仓库位置。
- Boss：Boss 战清场后跳过普通弹珠奖励，进入方块三选一；奖励品质按方块奖励阶段计算但最低为 `Blue`。选择后完成 Boss 节点、清空旧地图节点状态并生成下一张地图。
- 保留内容：Boss 续局会保留玩家、方块、弹珠、Sticker、Mod、金币等局内成长。

## 核心目录

- `Assets/Scripts/POPHero/Core`：游戏核心数据、配置服务、只读模型、HUD 命令、场景流转和组合根。
- `Assets/Scripts/POPHero/Flow`：回合结算、敌人行动、遭遇生成、战斗表现控制。
- `Assets/Scripts/POPHero/Combat`：发射器、瞄准输入、轨迹预测、弹射求解和球飞行模拟。
- `Assets/Scripts/POPHero/Board`：方块数据、运行时棋盘、世界表现和奖励服务。
- `Assets/Scripts/POPHero/Systems`：Sticker、Mod、商店、地图、治疗、删卡/工坊等系统。
- `Assets/Scripts/POPHero/UI`：Canvas HUD、Presenter、可复用 UI View、地图路线视图、旧 IMGUI fallback。
- `Assets/Scripts/POPHero/Editor`：场景构建、配置表导入、Editor 工具。
- `Assets/ConfigTables`：CSV 配置源数据。
- `Assets/Tests/EditMode`：当前 EditMode 回归测试。

## 关键代码入口

- [PopHeroGame.cs](Assets/Scripts/POPHero/Core/PopHeroGame.cs)：游戏组合根、状态切换和对外只读模型入口。
- [GameContracts.cs](Assets/Scripts/POPHero/Core/GameContracts.cs)：`IGameReadModel`、`IHudCommandSink`、服务接口和 HUD 命令。
- [RoundController.cs](Assets/Scripts/POPHero/Flow/RoundController.cs)：单回合结算入口。
- [EnemyTurnResolver.cs](Assets/Scripts/POPHero/Flow/EnemyTurnResolver.cs)：敌方行动结算，包含近战前进、贴脸攻击和飞行远程攻击。
- [EncounterDirector.cs](Assets/Scripts/POPHero/Flow/EncounterDirector.cs)：遭遇生成、双敌编队、目标选择、奖励汇总。
- [BattlePresentationController.cs](Assets/Scripts/POPHero/Flow/BattlePresentationController.cs)：战斗演出和敌人站位表现。
- [PlayerLauncher.cs](Assets/Scripts/POPHero/Combat/PlayerLauncher.cs)：玩家发射输入。
- [TrajectoryPredictor.cs](Assets/Scripts/POPHero/Combat/TrajectoryPredictor.cs)：预测轨迹。
- [BounceStepSolver.cs](Assets/Scripts/POPHero/Combat/BounceStepSolver.cs)：预测与真实弹射共用的弹跳求解。
- [CanvasHudController.cs](Assets/Scripts/POPHero/UI/CanvasHudController.cs)：正式 Canvas HUD 绑定、刷新和命令发送。
- [HudPresenters.cs](Assets/Scripts/POPHero/UI/HudPresenters.cs)：从只读游戏状态构建 UI view model。
- [SceneBuilder.cs](Assets/Scripts/POPHero/Editor/SceneBuilder.cs)：Battle UI/场景结构生成与修复工具。

## 配置表

CSV 位于 `Assets/ConfigTables`，运行时和 Editor 导入共用解析逻辑。

- `enemy.csv`：敌人模板，包含近战/飞行行为类型、血量、攻击、奖励、初始距离等。
- `globalConfig.csv`：全局数值，例如成长、发射次数、敌人距离默认值等。
- `mapConfig.csv`：地图层数、节点权重、Boss 等配置，包含 `restWeight`。
- `encounter.csv`：路线遭遇池，区分普通战斗和 Boss 遭遇，并配置敌人编队槽位。
- `blockType.csv` / `blockRarity.csv` / `blockRewardStage.csv`：方块类型、稀有度和奖励阶段。
- `sticker.csv` / `stickerToken.csv`：Sticker 定义和 token。
- `mod.csv`：Mod 定义。
- `shop.csv`：商店配置；`shop_block` 是当前固定方块商品槽位，`price` 优先作为方块购买价格，`rarityWeights` 控制商品品质权重。
- `blockOperation.csv`：删卡/工坊配置；`map_workbench` 当前允许替换和 1 次免费置换升级，`allowUpgrade / upgradeCostGold / maxUpgradeCount` 控制升级规则。

如果修改 CSV 后需要重建 ScriptableObject 配置，可使用根目录的 `RebuildConfigTables.bat`，或在 Unity 内通过对应 Editor 工具导入。

## UI 现状

- 正式前端是 `CanvasFrontend`，主要由 `CanvasHudController` 绑定。
- `BlockOperationsModal` 已烘进 `Battle.unity`，不再由运行时偷偷生成，方便在 Hierarchy 里直接编辑。
- 商店中旧的内嵌 `DeletePanel` 已移除，方块删除/替换统一进入独立工坊面板。
- `CanvasHudViews.cs` 放置卡片、Sticker、GM 调试项、工坊条目等小型 View。
- `PopHeroHud` 是旧 IMGUI fallback，保留用于调试，不应继续承载正式业务 UI。

## 测试与验证

推荐优先跑 Unity EditMode 测试：

```text
Window -> General -> Test Runner -> EditMode
```

当前测试程序集：

- `POPHero.EditModeTests`
- 覆盖敌人距离/飞行敌人/双敌目标选择/共享减伤池
- 覆盖地图事件、回血规则、CSV 解析与默认值
- 覆盖方块获取路径：商店购买、容量满失败、工坊升级、Boss/GM Boss 方块奖励和新地图续局

如果通过 Unity MCP 运行，可调用 `run_tests`：

```json
{
  "mode": "EditMode",
  "assembly_names": ["POPHero.EditModeTests"],
  "include_failed_tests": true,
  "include_details": false
}
```

手测建议：

- `Boot -> MainMenu -> Battle` 能正常进入。
- 第一场为单近战敌人，第二场开始为近战 + 飞行支援。
- 近战敌人按距离均匀前进，0 步时已经到主角面前。
- 飞行敌人原地远程攻击，不移动到主角面前。
- 普通战斗胜利后玩家不会自动回满血。
- 普通战斗胜利后只进入弹珠奖励，不出现方块奖励。
- 地图休息节点和事件营火能恢复 30% 最大生命。
- 商店能刷出方块商品；购买后上阵有空位则进上阵，上阵满则进仓库，仓库也满时购买失败且不扣钱。
- 地图工坊能替换上阵/仓库方块，并能对已有方块执行 1 次免费升级。
- Boss 战胜利后出现方块三选一，选完后进入下一张地图且保留局内成长。
- `D` 键 GM 面板中的事件调试按钮可直接触发对应节点或路线事件；GM Boss 秒杀后也应出现 Boss 方块三选一。

## 开发约定

- 新战斗规则优先落在 `Flow` 或 `Systems` 的小服务中，不要继续把所有逻辑塞回 `PopHeroGame`。
- UI 写入口保持 `IHudCommandSink` / `HudCommand`，UI 读取优先走 `IGameReadModel` 和 Presenter。
- 敌人模板数据不要存运行时距离；遭遇内距离应放在 `EnemyEncounterState`。
- 场景 UI 可以在 `Battle.unity` 里常驻，但生成/修复逻辑要同步更新 `SceneBuilder`。
- 修改配置字段时，Runtime CSV loader、Editor importer、fallback 配置和测试要一起更新。
- 不要手改 Unity YAML 做大规模结构变更；优先用 Unity Editor/PrefabUtility 或已有场景构建工具。

## 已知注意点

- 项目仍处于原型阶段，部分系统已有解耦方向，但 `PopHeroGame` 和 `CanvasHudController` 仍是较大的中枢。
- `Library/`、`Temp/`、`Logs/` 等 Unity 生成目录不应提交。
- Windows 下 Unity 可能自动改动 `ProjectSettings/ProjectSettings.asset`，提交前请确认是否为本次任务相关。
- 如果出现中文显示异常，优先确认文件编码为 UTF-8，以及 TextMeshPro 使用了项目中的 CJK 字体 fallback。

## 相关文档

- [AI_HANDOFF.md](docs/AI_HANDOFF.md)：历史接手说明。
- [DEVLOG_2026-03-31.md](docs/DEVLOG_2026-03-31.md)：早期开发记录。
- [scene_migration_plan.md](docs/scene_migration_plan.md)：场景迁移计划。
