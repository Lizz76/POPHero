# POPHero 场景化改造计划

## 目标
将代码动态生成的游戏视觉对象转化为预设在场景/Prefab 中的对象，方便后续在编辑器中直接替换美术资源。

---

## Phase 1: 场景骨架 + 3D 场景对象（本次执行）

### Step 1 — 新建游戏场景 + 搭建层级结构
在场景中预搭好以下层级：
```
POPHeroGame          (挂 PopHeroGame, PopHeroHud, DamageCounterView 组件)
├── World
│   ├── Board
│   │   ├── BoardFrame       (SpriteRenderer)
│   │   ├── BoardBackground  (SpriteRenderer)
│   │   ├── LaunchGuide      (SpriteRenderer)
│   │   ├── LaunchMarker     (SpriteRenderer)
│   │   ├── BottomLine       (SpriteRenderer + BoxCollider2D + ArenaSurfaceMarker)
│   │   ├── WallTop          (空容器，运行时填充砖块)
│   │   ├── WallLeft         (空容器)
│   │   └── WallRight        (空容器)
│   ├── EnemyLayer
│   │   ├── EnemyPanel       (SpriteRenderer)
│   │   ├── BattleStage
│   │   │   ├── Hero         (挂 PlayerPresenter)
│   │   │   │   ├── HeroBody    (SpriteRenderer)
│   │   │   │   ├── HeroCore    (SpriteRenderer)
│   │   │   │   ├── HpBack      (SpriteRenderer)
│   │   │   │   ├── HpFill      (SpriteRenderer)
│   │   │   │   ├── HeroName    (TextMesh)
│   │   │   │   └── HeroHp      (TextMesh)
│   │   │   └── Enemy        (挂 EnemyController)
│   │   │       ├── EnemyBody   (SpriteRenderer)
│   │   │       ├── EnemyCore   (SpriteRenderer)
│   │   │       ├── HpBack      (SpriteRenderer)
│   │   │       ├── HpFill      (SpriteRenderer)
│   │   │       ├── HpPreview   (SpriteRenderer)
│   │   │       ├── EnemyName   (TextMesh)
│   │   │       ├── EnemyIntent (TextMesh)
│   │   │       └── EnemyHp     (TextMesh)
│   │   └── BattleEffects    (空容器)
│   ├── Blocks               (运行时动态方块的容器)
│   └── Ball                 (挂 BallController, PlayerLauncher, Rigidbody2D, CircleCollider2D)
│       ├── BallVisual       (SpriteRenderer - 圆形)
│       ├── AimPreviewLine   (LineRenderer)
│       └── AimMemoryLine    (LineRenderer)
├── Main Camera              (正交摄像机)
```

### Step 2 — 修改 PopHeroGame.cs
- 移除 `BuildPrototype()` 中的 `new GameObject(...)` 代码
- 改为 `[SerializeField]` 引用 + `Awake()` 中查找/绑定
- `SetupCamera()` 改为读取场景中已有的 Camera
- 移除 `PopHeroBootstrap` 自动创建逻辑

### Step 3 — 修改 PlayerPresenter.cs
- 移除 `Initialize()` 中所有 `PrototypeVisualFactory.CreateSpriteObject/CreateTextObject` 调用
- 改为 `[SerializeField]` 引用场景中预设好的子对象
- `Initialize()` 只做数据初始化，不再创建 GameObject

### Step 4 — 修改 EnemyController.cs
- 同 PlayerPresenter，移除视觉对象创建代码
- 改为序列化字段引用

### Step 5 — 修改 BallController.cs + PlayerLauncher.cs
- Ball 的 Rigidbody2D、CircleCollider2D、TrailRenderer 预设在场景中
- PlayerLauncher 的 LineRenderer（AimPreviewLine, AimMemoryLine）预设在场景中
- 移除代码中的动态创建

### Step 6 — 修改 BoardBlock.cs
- `Initialize()` 中仍需运行时创建方块（因为方块是动态的）
- 但 `EnsureLabel()` 中的 TextMesh 创建可保留（方块本身就是动态生成的）
- **方块不改**，因为数量和种类在运行时变化

### Step 7 — 修改 BoardManager / BuildBoard 相关
- BoardFrame、BoardBackground、LaunchGuide、BottomLine、LaunchMarker 改为场景引用
- 砖墙容器（WallTop/Left/Right）预设为空 GameObject，砖块仍然运行时填充
- `BuildBoard()` 方法大幅简化

### Step 8 — 清理
- 禁用 PopHeroBootstrap（不再自动创建 POPHeroGame）
- PrototypeVisualFactory 保留（仍被动态方块使用），但场景对象不再依赖它
- 确保运行正常

---

## Phase 2: UI 改 UGUI（后续）
- 将 OnGUI 的所有面板迁移到 Canvas + UGUI

## Phase 3: 最终清理（后续）
- 完全移除 PrototypeVisualFactory（方块也用 Prefab）
- 移除所有硬编码颜色常量
