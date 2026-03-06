# dq99 项目代理说明

本项目是一个 Unity 2022.3 的 JRPG 原型工程，目标是做出类似《勇者斗恶龙》城镇跑动、交谈、室内外切换的可迭代原型。

## 工作目标

- 优先维护“代码驱动 UI + 数据驱动玩法 + 场景负责空间布局”的边界。
- 尽量减少 `MonoBehaviour` 中的业务逻辑。
- 新增功能时，优先扩展纯 C# 逻辑层和轻量适配层，不要把状态机和规则散落到 Inspector。
- 面向快速验证美术和玩法闭环，而不是一开始追求复杂系统。

## 当前架构约定

- 运行时逻辑入口在 `Assets/Scripts/Runtime`。
- `Domain`：纯玩法状态与流程，不应依赖 `UnityEngine`。
- `Infrastructure`：内容加载。
- `Unity`：场景扫描、表现层、输入、相机、场景切换。
- 每个 Unity 场景对应一份内容配置：`Assets/Resources/Prototype/<SceneName>.json`。
- 场景中的空间锚点使用 `SceneMarker`。
- NPC/玩家/门的可视对象通过 `PlayerAvatarMarker`、`ActorMarker`、`PortalMarker` 与逻辑绑定。
- 场景中的阻挡物体通过 `WalkBlocker` 明确声明，不能默认依赖所有 collider。

## 内容制作边界

- 场景、美术摆放、碰撞、灯光：在 Unity 场景里配置。
- NPC 身份、对话、门跳转目标、flag 条件：在 JSON 里配置。
- 玩家/NPC 模型 prefab 应尽量只承载模型、Animator、必要 marker，不承载玩法规则。
- 角色表现由 `ActorPresentation` 驱动：
  - 面向
  - 移动状态
  - Animator 参数

## 修改原则

- 不要重新引入自动生成 blockout 场景作为默认流程，除非用户明确要求。
- 不要把玩法真相迁回 `ScriptableObject` 或 Inspector 字段。
- 如果需要新增场景，优先同步补齐：
  - Unity Scene
  - `Assets/Resources/Prototype/<SceneName>.json`
  - 必要的 `SceneMarker`
  - Build Settings 说明
- 如果新增 NPC 或门，优先复用现有 marker + JSON 流程。
- 如果需要做美术表现增强，优先在 presenter 层加能力，不要把逻辑判断写进 Animator StateMachine。

## 编译与检查

- 当前环境下，常常不能使用 Unity batch compile，因为项目可能正被编辑器占用。
- 优先使用：

```bash
dotnet build Assembly-CSharp.csproj -nologo
```

- 如需完整 Unity 批处理检查，可使用：

```bash
bash /Users/usr/Documents/unity_projects/dq99/scripts/check-unity-compile.sh
```

- 但在 Unity 编辑器打开同一工程时，这个 batch 命令会失败。

## 当前重点

如果没有用户明确改方向，默认优先级如下：

1. 角色表现层：朝向、动画、巡逻、交互反馈
2. 场景配置流：marker、portal、scene content
3. 触发器与 flag 流程
4. 调试工具与配置可视化

## 回答风格

- 使用中文。
- 优先给出可落地的改法，而不是泛泛建议。
- 如果用户是在配场景或模型，要明确说明：
  - 哪些改在 Unity Scene
  - 哪些改在 JSON
  - 哪些组件需要挂在 prefab 或场景物体上
