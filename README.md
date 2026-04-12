# POPHero

这是 `POPHero` 当前版本的仓库首页接手说明。

目标读者优先是下一位接手项目的 AI 或开发者。这里不再描述最早那版“弹珠 + Buff 三选一”原型，而是只保留当前代码里已经成立、能直接验证的事实。更详细的系统说明请继续看 [docs/AI_HANDOFF.md](D:\Unity%202022.3.0f1c1\POPhero\POPHero\docs\AI_HANDOFF.md)。

## 项目现状

`POPHero` 目前已经从早期的弹珠战斗验证原型，演进为一套带中场构筑流程的 Unity 2022.3 战斗原型：

- 战斗载体是 `Block`
- 可安装到方块上的局部强化是 `Sticker`
- 全局规则层是 `Mod`
- 中场流程包含 `方块奖励 / 奖励三选一 / 商店 / 整理配置`
- 玩家拥有 `上阵区 + 仓库区`，当前战斗只读取上阵区

核心组合根在 [PopHeroGame.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Core\PopHeroGame.cs)，全局原型配置在 [PopHeroPrototypeConfig.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Core\PopHeroPrototypeConfig.cs)。

## 启动入口

当前正式场景链路是：

- `Boot`
- `MainMenu`
- `Battle`

相关入口：

- 场景名定义和跳转： [SceneFlow.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Core\SceneFlow.cs)
- Boot 自动跳主菜单： [ProjectBootstrap.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Core\ProjectBootstrap.cs)
- 主菜单开始游戏： [MainMenuController.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Core\MainMenuController.cs)
- Battle 缺少 `PopHeroGame` 时的兜底提示： [PopHeroBootstrap.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Core\PopHeroBootstrap.cs)

说明：

- [SampleScene.unity](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scenes\SampleScene.unity) 现在应视为历史场景 / 调试遗留，不应继续作为 README 里的主入口。
- 若需要重建正式场景，使用 [SceneBuilder.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Editor\SceneBuilder.cs) 里的编辑器菜单入口。

## 当前核心循环

战斗阶段：

- `Aim`
- `BallFlying`
- `RoundResolve`

中场阶段：

- `BlockRewardChoose`
- `RewardChoose`
- `Shop`
- `LoadoutManage`

这些状态都由 [PopHeroGame.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Core\PopHeroGame.cs) 驱动。当前战斗不是飞行中实时扣敌人真血，而是先累计结果，再在结算演出中刷新主角 / 敌人的生命表现。

## 当前关键系统

### 方块与卡组

- 方块真实实例数据是 `BlockCardState`
- 玩家卡组由 `activeBlocks + reserveBlocks` 组成
- 当前战斗板面只从上阵区生成
- 上阵/仓库交换的是完整实例，不是模板引用

主要入口在 [BoardServices.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Board\BoardServices.cs) 和 [BoardManager.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Board\BoardManager.cs)。

### Sticker 安装与整理

- `Sticker` 是当前主强化层
- 整理阶段仍然通过右侧 `BlockManagementPanel` 槽位安装
- 操作路径是“拿起 sticker -> 点击高亮槽位 -> 安装 / 卸下”

相关 UI 和交互汇总在 [CanvasHudController.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\UI\CanvasHudController.cs)。

### Mod / Growth / Shop

- `Mod` 是全局规则层，不装在方块上
- 商店支持 sticker、mod、growth，以及删除一张方块
- 方块奖励和成长奖励与商店之间已经形成完整中场流程

### Canvas HUD 与 Tooltip

- 当前主 HUD 已经是 Canvas 驱动
- 方块 tooltip、socket tooltip、drag sticker 面板、loadout modal 都走同一套 HUD 控制器
- 世界方块与右侧栏位都能把详细信息接到 tooltip，而不是把数值常驻写在方块本体上

主入口仍是 [CanvasHudController.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\UI\CanvasHudController.cs)。

## 最近已落地的重要改动

以下内容都是当前代码里已经能确认的改动：

- 开局不再弹“初始选块”，而是直接发 1 张白稀有攻击方块进入首战准备
- 击败敌人后的普通方块奖励仍保留 `BlockRewardChoose`
- 攻击演出排层已经改成“攻击者在前”
- 方块本体改成图标化展示，详细数值与说明通过 tooltip 展示
- 方块视觉已经转成 `prefab + config` 驱动
- 当前 block 美术配置模型是：
  - `4` 个稀有度背景
  - `12` 个类型 x 稀有度 icon
- 方块不再依赖程序稀有度颜色来表达最终美术效果

其中方块视觉配置和资源入口在 [PopHeroPrototypeConfig.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Core\PopHeroPrototypeConfig.cs)，世界/右栏 block 视图在：

- [BlockWorldView.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Board\BlockWorldView.cs)
- [BlockCellView.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\UI\BlockCellView.cs)

## 后续修改优先入口

如果后续 AI 要继续维护，优先从这些入口建立上下文：

- 组合根与战斗流程： [PopHeroGame.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Core\PopHeroGame.cs)
- 方块实例、奖励、运行时展示： [BoardServices.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Board\BoardServices.cs)
- HUD、tooltip、loadout、右侧栏位交互： [CanvasHudController.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\UI\CanvasHudController.cs)
- 原型配置和 block 视觉资源入口： [PopHeroPrototypeConfig.cs](D:\Unity%202022.3.0f1c1\POPhero\POPHero\Assets\Scripts\POPHero\Core\PopHeroPrototypeConfig.cs)
- 深入接手说明： [AI_HANDOFF.md](D:\Unity%202022.3.0f1c1\POPhero\POPHero\docs\AI_HANDOFF.md)

## 文档索引

- 详细 AI 接手说明： [docs/AI_HANDOFF.md](D:\Unity%202022.3.0f1c1\POPhero\POPHero\docs\AI_HANDOFF.md)
- 开发日志： [docs/DEVLOG_2026-03-31.md](D:\Unity%202022.3.0f1c1\POPhero\POPHero\docs\DEVLOG_2026-03-31.md)
- 场景迁移计划： [docs/scene_migration_plan.md](D:\Unity%202022.3.0f1c1\POPhero\POPHero\docs\scene_migration_plan.md)

## 维护提示

- 根 README 现在应作为“仓库首页总览”
- 深层系统规则、维护约束、接手 checklist 继续放在 [AI_HANDOFF.md](D:\Unity%202022.3.0f1c1\POPhero\POPHero\docs\AI_HANDOFF.md)
- 如果后续系统再发生结构级变化，优先同步 README 的“项目现状 / 启动入口 / 最近改动 / 修改入口”四段
