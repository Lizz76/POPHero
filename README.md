# POPHero

`POPHero` 是一个 Unity 2022.3 弹球轨迹 Roguelike 战斗原型。当前版本已经从早期“弹球 + Buff 三选一”演进为带有方块构筑、中场奖励、商店、Sticker 镶嵌、Mod 全局规则和 Canvas 前端的可迭代原型。

本文档用于给后续接手的开发者或 AI 工具快速建立上下文。更细的系统说明请继续阅读 [docs/AI_HANDOFF.md](docs/AI_HANDOFF.md)。

## 当前项目状态

- 战斗载体是 `Block`，玩家通过弹球命中方块获得伤害、护盾、倍率等回合收益。
- 局部强化叫 `Sticker`，通过 socket 镶嵌到具体方块实例上。
- 全局规则层叫 `Mod`，用于影响经济、信息展示、操作手感和成长上限。
- 玩家方块组拆成 `Active Blocks` 与 `Reserve Blocks`，战斗只读取上阵区。
- 中场流程包含 `BlockRewardChoose -> RewardChoose -> Shop -> LoadoutManage`。
- 正式前端使用 `Canvas + TMP + Presenter/Command`，旧 `PopHeroHud` 只保留为调试 fallback。

## 启动入口

正式场景链路为：

1. `Boot`
2. `MainMenu`
3. `Battle`

相关脚本入口：

- 场景跳转：[Assets/Scripts/POPHero/Core/SceneFlow.cs](Assets/Scripts/POPHero/Core/SceneFlow.cs)
- Boot 初始化：[Assets/Scripts/POPHero/Core/ProjectBootstrap.cs](Assets/Scripts/POPHero/Core/ProjectBootstrap.cs)
- 主菜单：[Assets/Scripts/POPHero/Core/MainMenuController.cs](Assets/Scripts/POPHero/Core/MainMenuController.cs)
- 战斗组合根：[Assets/Scripts/POPHero/Core/PopHeroGame.cs](Assets/Scripts/POPHero/Core/PopHeroGame.cs)

`SampleScene.unity` 只应视作历史/调试遗留，正式运行请以 `Battle.unity` 为准。

## 核心系统入口

- 战斗流程与全局状态：[Assets/Scripts/POPHero/Core/PopHeroGame.cs](Assets/Scripts/POPHero/Core/PopHeroGame.cs)
- 弹球发射与输入区域限制：[Assets/Scripts/POPHero/Combat/PlayerLauncher.cs](Assets/Scripts/POPHero/Combat/PlayerLauncher.cs)
- 轨迹/碰撞共享求解：[Assets/Scripts/POPHero/Combat/BounceStepSolver.cs](Assets/Scripts/POPHero/Combat/BounceStepSolver.cs)
- 方块实例、奖励、运行时板面：[Assets/Scripts/POPHero/Board/BoardServices.cs](Assets/Scripts/POPHero/Board/BoardServices.cs)
- 方块世界表现：[Assets/Scripts/POPHero/Board/BlockWorldView.cs](Assets/Scripts/POPHero/Board/BlockWorldView.cs)
- Canvas HUD 与中场 UI：[Assets/Scripts/POPHero/UI/CanvasHudController.cs](Assets/Scripts/POPHero/UI/CanvasHudController.cs)
- Canvas UI 小组件：[Assets/Scripts/POPHero/UI/CanvasHudViews.cs](Assets/Scripts/POPHero/UI/CanvasHudViews.cs)
- 场景脚手架：[Assets/Scripts/POPHero/Editor/SceneBuilder.cs](Assets/Scripts/POPHero/Editor/SceneBuilder.cs)

## 当前交互规则

- 发射输入只允许在中间棋盘战斗区域内生效，点击左侧状态栏、右侧方块栏、商店、设置面板等 UI 不会发射。
- 设置按钮位于 Battle 右上角，打开后暂停游戏，并提供“继续游戏 / 返回菜单 / 退出游戏”。
- Sticker 背包现在是真拖拽交互：按住嵌片拖到右侧高亮 socket，松手安装；松在空白处或非法槽位会取消并归位。
- 拖拽嵌片时鼠标旁会显示一个与 socket 尺寸接近的小 ghost，位于最高 UI 排序层，不会被面板挡住，也不会阻挡 drop。
- 拖拽过程中普通 tooltip 会隐藏；停止拖拽后，背包、socket、方块图标 tooltip 恢复显示。
- 已安装的 socket 在未拖拽时仍可点击卸下。

## 最近开发记录

### 2026-04-13

- 接入真实 `uGUI` 嵌片拖拽：新增背包嵌片 `BeginDrag/Drag/EndDrag` 与 socket `Drop` 事件链。
- 拖拽视觉从“大文本浮窗”改成 `28x28` 小嵌片 ghost，并提升到独立高排序 Canvas，避免被右侧栏或中场面板遮挡。
- 拖拽过程中隐藏普通 tooltip，避免悬停窗口与拖拽 ghost 同时出现。
- 增加发射输入区域限制：只有鼠标在棋盘区域内才允许瞄准和发射，UI 区点击不会误发射。
- 设置入口改为真实场景对象/Prefab，并补充继续游戏、返回菜单、退出游戏三按钮。
- 给方块 Icon 受击动画准备 Animator 资源骨架，命中时只触发 `Hit` Trigger，动画曲线可在 Unity Animator 中继续调整。
- Canvas 前端、右侧方块管理栏、中场整理界面继续保留现有 `Presenter -> View -> HudCommand` 低耦合链路。

## 验证方式

常用命令：

```powershell
dotnet build E:\UnityProject\POPHero-main\POPHero-main.sln
```

当前已知构建结果：

- `0 errors`
- 可能出现 Unity/MCP 相关程序集版本 warning，例如 `System.Net.Http` 或 `System.IO.Compression` 冲突；这些 warning 当前不阻塞运行。

建议 Unity 内回归：

- `Boot -> MainMenu -> Battle` 能正常进入。
- Battle 中设置面板打开后，点击战场不会发射；继续游戏后需要重新点击棋盘才可发射。
- 进入背包/整理界面，从嵌片背包拖拽到合法 socket 能安装；松到空白处会取消并归位。
- 拖拽 ghost 始终显示在 UI 最上层，不被面板遮挡。
- 商店、右侧栏、上阵/仓库交换、socket 卸下保持可用。

## 文档索引

- AI 接手说明：[docs/AI_HANDOFF.md](docs/AI_HANDOFF.md)
- 开发日志：[docs/DEVLOG_2026-03-31.md](docs/DEVLOG_2026-03-31.md)
- 场景迁移计划：[docs/scene_migration_plan.md](docs/scene_migration_plan.md)

## 维护提示

- README 用于记录项目当前事实与近期开发记录，不要写过期假设。
- 深层系统规则、接手 checklist 和详细约束优先维护在 [docs/AI_HANDOFF.md](docs/AI_HANDOFF.md)。
- 若后续继续重构 UI 或战斗流程，请同步更新“当前交互规则”和“最近开发记录”。
