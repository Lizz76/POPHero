# POPHero AI 接手开发文档

本文档用于帮助后续 AI 工具或新开发者快速接手 `POPHero` 当前原型。  
目标不是解释所有历史，而是说明：

- 现在项目已经做到了什么
- 核心系统的数据从哪里来
- 应该从哪一层改功能
- 哪些地方已经开始做低耦合重构
- 当前有哪些已知坑点和维护约束

## 1. 项目定位

`POPHero` 是一个 Unity 2022.3 原型项目，核心玩法是：

- 下半屏进行弹球战斗
- 上半屏显示主角与敌人的对峙和攻击演出
- 玩家发射球，球在墙和方块间反弹
- 命中方块累积本轮伤害/护盾/倍率收益
- 球落地后统一结算
- 敌人死亡后进入中场构筑

当前项目已经不再是“简单 Buff 三选一”原型，而是逐步迁移到类似 STICKER BALL 的设计语言：

- 方块是载体
- sticker 是可安装的规则单元
- mod 是全局规则层
- 商店和奖励阶段是完整中场流程
- 方块有上阵区与仓库区

## 2. 当前推荐入口

### 启动入口

- 场景：`Assets/Scenes/SampleScene.unity`
- 启动脚本：`Assets/Scripts/POPHero/Core/PopHeroBootstrap.cs`
- 主组合根：`Assets/Scripts/POPHero/Core/PopHeroGame.cs`

### 构建命令

```powershell
dotnet build E:\UnityProject\POPHero\POPHero.sln
```

说明：

- 当前可以稳定编译通过
- 仍然存在一个历史遗留 warning：`System.Net.Http` 版本冲突
- 该 warning 目前不是功能性阻塞问题

## 3. 当前目录结构

脚本已经按职责整理为以下目录：

### Core

路径：`Assets/Scripts/POPHero/Core`

用途：

- 组合根和全局配置
- 跨层通用类型与接口

关键脚本：

- `PopHeroGame.cs`
- `PopHeroBootstrap.cs`
- `PopHeroPrototypeConfig.cs`
- `GameContracts.cs`
- `GameplayTypes.cs`

### Flow

路径：`Assets/Scripts/POPHero/Flow`

用途：

- 回合推进
- 游戏阶段和应用层流程控制

关键脚本：

- `GameFlowControllers.cs`
- `RoundController.cs`
- `RoundResolveResult.cs`

### Combat

路径：`Assets/Scripts/POPHero/Combat`

用途：

- 瞄准
- 轨迹预览
- 球飞行与反弹求解

关键脚本：

- `AimStateController.cs`
- `AimInputStrategies.cs`
- `PlayerLauncher.cs`
- `BallController.cs`
- `BounceStepSolver.cs`
- `TrajectoryPredictor.cs`
- `TrajectoryPreviewResult.cs`
- `ArenaSurfaceMarker.cs`

### Board

路径：`Assets/Scripts/POPHero/Board`

用途：

- 方块实例
- 方块组管理
- 运行时棋盘生成

关键脚本：

- `BoardManager.cs`
- `BoardServices.cs`
- `BoardBlock.cs`
- `AttackAddBlock.cs`
- `AttackMultiplyBlock.cs`
- `ShieldBlock.cs`

### Characters

路径：`Assets/Scripts/POPHero/Characters`

用途：

- 主角/敌人数据与表现

关键脚本：

- `PlayerData.cs`
- `EnemyData.cs`
- `PlayerPresenter.cs`
- `EnemyController.cs`

### Systems

路径：`Assets/Scripts/POPHero/Systems`

用途：

- sticker 系统
- mod 系统
- 商店系统
- 奖励系统

关键脚本：

- `StickerCatalog.cs`
- `StickerExecution.cs`
- `StickerFlow.cs`
- `StickerRuntime.cs`
- `ModShopSystems.cs`
- `BuffManager.cs`

说明：

- `BuffManager.cs` 现在是遗留兼容文件，不是强化主系统
- 新功能应优先落在 `Sticker` / `Mod` / `Shop` 体系，不要继续扩旧 Buff 语义

### UI

路径：`Assets/Scripts/POPHero/UI`

用途：

- IMGUI HUD
- presenter / view model
- 运行时生成的世界空间文本/视觉对象

关键脚本：

- `PopHeroHud.cs`
- `HudPresenters.cs`
- `PrototypeVisualFactory.cs`

## 4. 当前核心流程

### 战斗主流程

1. 进入 `Aim`
2. 玩家瞄准并发射
3. 进入 `BallFlying`
4. 球在场内反弹并累计本轮收益
5. 球落地后进入 `RoundResolve`
6. 先结算数值，再播放主角/敌人的攻击演出
7. 若敌人未死，回到 `Aim`
8. 若敌人死亡，进入中场流程

### 中场流程

当前标准顺序：

1. 方块三选一
2. sticker / mod / growth 奖励三选一
3. 商店
4. 整理阶段（socket 安装、上阵/仓库调整）
5. 下一只敌人

### 状态枚举

定义在 `GameplayTypes.cs`：

- `Aim`
- `BallFlying`
- `RoundResolve`
- `BlockRewardChoose`
- `RewardChoose`
- `Shop`
- `LoadoutManage`
- `GameOver`

## 5. 核心数据源

这是后续开发时最重要的部分。

### 玩家真实方块组数据源

不要再用“等级决定方块数”的老思路。

当前真正的数据源是：

- `PlayerBlockCollection.activeBlocks`
- `PlayerBlockCollection.reserveBlocks`

对应服务在 `BoardServices.cs` 里。

规则：

- 上阵区最多 10
- 仓库区最多 3
- 当前战斗只读取上阵区

### 当前战斗板面来源

运行时出场的方块来自：

- `BoardManager.ActiveCardStates`
- 实际运行时生成由 `RuntimeBoardService` 负责

**仓库中的方块不会参与当前战斗生成。**

### 方块实例语义

当前不是“方块模板池”，而是完整实例：

- `BlockCardState`
  - 类型
  - 稀有度
  - 基础值
  - socket
  - 已装 sticker
  - family / tags / 文案信息

所以：

- 把方块移入仓库，不会丢 sticker
- 上阵/仓库互换，本质是交换完整实例
- 删除方块，删的是完整实例

### 伤害显示规则

当前已经修正为：

- 飞行中只更新左侧伤害累计数字
- 敌人不会在球飞行中实时掉血
- 敌人血条和数字只在主角跳击命中帧刷新
- 敌人反击命中主角时，主角血条才刷新

不要把“飞行中的 pendingDamage”重新接回敌人血条。

## 6. 当前已完成的低耦合重构

这个项目已经进入“低耦合重构 Phase 2”的中途状态，不是完全重写，但也不是原始版本了。

### 已经搭好的层

#### `PopHeroGame` 正在收口为组合根

职责应理解为：

- 装配服务和控制器
- 桥接 Unity 生命周期
- 提供少量 facade / command sink

不要继续把新业务大段塞回 `PopHeroGame`。

#### `BoardManager` 已退成 facade

`BoardManager` 现在主要包装三个具体服务：

- `BlockCollectionService`
- `BlockRewardService`
- `RuntimeBoardService`

如果你要改：

- 上阵/仓库/删除/互换 => 优先看 `BlockCollectionService`
- 方块三选一/稀有度曲线 => 优先看 `BlockRewardService`
- 场上生成/重排/高亮 => 优先看 `RuntimeBoardService`

#### sticker 触发与执行已开始分层

当前拆为：

- `StickerEffectRunner`：兼容入口 + 事件监听壳
- `StickerTriggerDispatcher`：决定谁该触发
- `StickerEffectExecutor`：执行效果原语

后续如果扩 sticker：

- 新触发逻辑先加在 dispatcher
- 新效果原语优先加在 executor
- 不要让 `RoundController` 直接知道具体 sticker id 逻辑

#### 反弹求解已共享

现在真实飞行和轨迹预览共用：

- `BounceStepSolver`

所以：

- 普通反弹
- 角落合成反射
- 嵌入恢复
- 忽略上一碰撞体

都应该优先改这一个 solver，不要再在 `BallController` 和 `TrajectoryPredictor` 各修一份。

#### HUD 已经部分 presenter 化

当前：

- `PopHeroHud` 仍然是最大 UI 文件
- 但状态栏 / 战斗栏 / 右侧方块管理 / 中场大面板已经开始改成 presenter 输出 model

对应文件：

- `HudPresenters.cs`

后续如果继续解耦：

- 优先把剩余直接业务判断从 `PopHeroHud` 搬到 presenter / command sink
- 不要让 HUD 直接操作库存、商店、奖励、方块组

## 7. 输入与瞄准

当前瞄准逻辑已经不是早期的“纯锁墙点”。

现状：

- `PlayerLauncher` 只做输入适配
- 输入策略在 `AimInputStrategies.cs`
  - `PcAimInputStrategy`
  - `MobileAimInputStrategy`
- 真正的锁输入、阈值判断、是否重算轨迹都在 `AimStateController`

这意味着：

- 轻微晃鼠标不应该持续漂移轨迹
- 若想改手感，应优先改 `AimStateController` 和配置
- 不要重新把锁定逻辑塞回 `PlayerLauncher`

## 8. sticker / mod / 商店

### sticker

当前 sticker 是主强化层，不是旧 Buff。

已具备：

- 库存
- 拖拽附着
- 点击 socket 安装
- 点击已装 socket 卸下
- 目标类型校验

关键文件：

- `StickerCatalog.cs`
- `StickerRuntime.cs`
- `StickerFlow.cs`
- `StickerExecution.cs`

### mod

mod 是全局规则层，不装在方块上。

关键文件：

- `ModShopSystems.cs`

当前仍有一部分中文字符串在该文件里是历史乱码，后续清理 UI 文案时可以优先修这里。

### 商店

当前商店支持：

- 购买 sticker
- 购买 mod
- 购买 growth
- 刷新
- 钱不够反馈
- 删除 1 张方块

删除规则：

- 每次进商店最多删除 1 张
- 可删上阵或仓库

## 9. UI 与表现约束

### 当前 UI 技术栈

仍然使用：

- `IMGUI`
- 运行时代码创建世界对象
- 世界空间文本用 `TextMesh`

没有迁移到：

- uGUI
- TMP
- Canvas 驱动的正式 UI 架构

这意味着后续修改要注意：

- `PopHeroHud.cs` 是 IMGUI
- 世界空间文本要走 `PrototypeVisualFactory`
- 中文字体依赖运行时动态字体链

### 中文字体

`PrototypeVisualFactory` 已经在尝试使用 Windows 中文字体链。  
如果出现世界空间中文乱码，优先检查：

- `PrototypeVisualFactory`
- 是否新建了 `TextMesh` 但没走统一创建入口

### 页面适配

当前中场弹层是：

- 内容区可滚动
- 底部关键按钮固定

这是为了避免“选项太多看不到退出键”的问题。  
不要轻易回退成整页不滚动。

## 10. 已知维护约束

### 1. `Assembly-CSharp.csproj` 是显式列文件路径的

这很重要。

如果你移动脚本文件：

- Unity 侧需要保留 `.meta`
- 本地 `dotnet build` 还需要同步更新 `Assembly-CSharp.csproj`

否则会出现：

- Unity 里能编
- `dotnet build` 失败

这次目录整理已经同步修过一轮，但以后继续移动文件时仍要注意。

### 2. `README.md` 当前有乱码

根因是历史编码污染。  
如果后续要继续维护文档，**优先看本文件和 `docs`**，不要把 `README.md` 当作最可信来源。

### 3. `BuffManager.cs` 是旧系统残留

新功能不要再往它上面叠。  
如果未来有时间，建议把它彻底退役或改成清晰的兼容壳。

### 4. 部分系统仍是“Facade + 兼容入口”状态

这是刻意为之，用来降低一次性重构风险。  
所以你会看到：

- 新服务已经拆出
- 但旧 public 方法名还在保留

后续重构建议优先：

1. 先把调用点慢慢改到接口/服务
2. 最后再删兼容壳

## 11. 后续 AI 修改建议

### 如果要改玩法规则

优先入口：

- 回合结算：`RoundController.cs`
- 结算后流程：`GameFlowControllers.cs`
- 伤害/演出显示：`PopHeroGame.cs` + `EnemyController.cs` + `PlayerPresenter.cs`

### 如果要改方块成长/拿牌

优先入口：

- `BlockRewardService`
- `GameplayTypes.cs`
- `PopHeroPrototypeConfig.cs`

### 如果要改上阵/仓库/删方块

优先入口：

- `BlockCollectionService`
- `BoardManager.cs`
- `PopHeroHud.cs`

### 如果要改 sticker 行为

优先入口：

- `StickerTriggerDispatcher`
- `StickerEffectExecutor`
- `StickerCatalog`

### 如果要改轨迹、反弹、穿块、角落卡住

优先入口：

- `BounceStepSolver.cs`

不要先在：

- `BallController`
- `TrajectoryPredictor`

各修一份。

### 如果要继续做低耦合重构

当前最值得继续做的是：

1. 把 `PopHeroHud` 剩余的直接业务判断继续移到 presenter / command sink
2. 让 `StickerEffectRunner` 兼容壳进一步变薄
3. 把 `ModShopSystems.cs` 按 `Mod / Shop / Growth` 再拆小

## 12. 建议接手检查清单

后续 AI 在开始动代码前，建议先确认：

1. `dotnet build E:\UnityProject\POPHero\POPHero.sln` 是否通过
2. 当前正在改的是哪一层
3. 是否已经有对应 service / presenter / solver 可以接入
4. 是否会破坏这些关键约束：
   - 战斗只读上阵区
   - 飞行中敌人不实时掉血
   - 演出是先结算后表现
   - 轨迹和真实飞行共用同一套 solver
   - socket / sticker / 仓库 / 上阵都存完整实例

如果你的改动会跨多层，优先从服务层开始，不要先把逻辑写进 HUD 或 Presenter。

