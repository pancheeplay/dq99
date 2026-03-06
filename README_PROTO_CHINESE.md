# dq99 原型配置说明

这份文档给场景配置人员、美术配置人员、内容配置人员使用。  
目标是让你知道：什么东西在 Unity 里配，什么东西在 JSON 里配。

## 一、项目当前在做什么

当前原型支持：

- 城镇内跑动
- 与 NPC 交谈
- 门口传送
- 独立场景切换
- 玩家/NPC 朝向与基础动画参数驱动

当前推荐流程是：

- Unity Scene 负责空间、模型、碰撞、灯光
- JSON 负责 NPC 身份、对话、门目标、flag 条件

## 二、你需要改哪些文件

### 1. Unity 场景

你主要会改这些场景：

- `Assets/Scenes/SampleScene.unity`
- `Assets/Scenes/InnScene.unity`
- `Assets/Scenes/ShopScene.unity`

如果你新增场景，比如教堂、民居，也按同样模式扩展。

### 2. 场景对应的数据文件

每个 Unity 场景都要有一份对应的 JSON：

- `Assets/Resources/Prototype/SampleScene.json`
- `Assets/Resources/Prototype/InnScene.json`
- `Assets/Resources/Prototype/ShopScene.json`

规则是：

- `SampleScene.unity` 对应 `SampleScene.json`
- `InnScene.unity` 对应 `InnScene.json`
- `ShopScene.unity` 对应 `ShopScene.json`

如果你新建 `ChurchScene.unity`，就应该配 `Assets/Resources/Prototype/ChurchScene.json`。

## 三、什么在 Unity 里配

这些内容在 Unity 场景里处理：

- 环境模型，比如 `City.prefab`
- 玩家模型，比如 `Hero.prefab`
- NPC 模型
- 门、柜台、家具、地形、灯光
- 碰撞体
- marker 空物体

## 四、什么在 JSON 里配

这些内容在 `Assets/Resources/Prototype/<SceneName>.json` 里处理：

- 玩家出生点使用哪个 marker
- 本场景有哪些 NPC
- NPC 对话 id
- 本场景有哪些门
- 门会跳去哪个场景
- 门会落到哪个 marker
- 门是否需要 flag 才能打开
- 对话内容和选项

## 五、玩家模型怎么配

玩家模型在 Unity 场景里配置，不在 JSON 里配置模型路径。

做法：

1. 把 `Hero.prefab` 拖进场景
2. 在 `Hero` 根节点上挂 `PlayerAvatarMarker`
3. 如果模型有 `Animator`，保留它
4. 在场景里创建一个空物体，挂 `SceneMarker`
5. 把这个 `SceneMarker.MarkerId` 填成对应场景 JSON 里的玩家出生点 id

例如在 `SampleScene` 里：

- `SampleScene.json` 里写：`playerSpawnMarkerId = "player_spawn_town"`
- 场景里就必须有一个 `SceneMarker`，它的 `MarkerId` 是 `player_spawn_town`

运行时会自动把玩家模型同步到这个 marker。

## 六、NPC 模型怎么配

NPC 的位置在场景里配，身份和对话在 JSON 里配。

做法：

1. 把 NPC prefab 拖进场景
2. 在 NPC 根节点上挂 `ActorMarker`
3. `ActorMarker.ActorId` 要和 JSON 里的 `actors[].id` 一致
4. 再创建一个 `SceneMarker`
5. `SceneMarker.MarkerId` 要和 JSON 里的 `actors[].markerId` 一致

例如：

```json
{
  "id": "npc_innkeeper",
  "markerId": "spawn_innkeeper",
  "displayName": "旅店老板",
  "dialogueId": "innkeeper_intro"
}
```

那你就需要：

- 一个 NPC 模型根节点，挂 `ActorMarker`，`ActorId = npc_innkeeper`
- 一个 `SceneMarker`，`MarkerId = spawn_innkeeper`

## 七、门和场景切换怎么配

门的空间位置在 Unity 场景里配，门跳去哪里在 JSON 里配。

### 场景里要做的事

1. 在门口放一个空物体或入口物体
2. 挂 `PortalMarker`
3. `PortalMarker.PortalId` 要和 JSON 里的 `portals[].id` 一致
4. 再放一个 `SceneMarker`
5. `SceneMarker.MarkerId` 要和 JSON 里的 `portals[].markerId` 一致

### JSON 里要做的事

例如：

```json
{
  "id": "portal_inn_enter",
  "markerId": "door_inn_outside",
  "destinationSceneName": "InnScene",
  "destinationMarkerId": "player_spawn_inn",
  "displayName": "旅店",
  "promptText": "进入"
}
```

表示：

- 这个门位于 `door_inn_outside`
- 进入后切换到 `InnScene`
- 玩家落到 `player_spawn_inn`

### 目标场景里也要有 marker

比如 `InnScene` 里必须有：

- `SceneMarker.MarkerId = player_spawn_inn`

否则进入旅店后玩家没有正确落点。

## 八、碰撞怎么配

不要让所有模型都自动参与阻挡。

推荐规则：

- 需要挡路的墙、柜台、房屋外壳：挂 collider，再挂 `WalkBlocker`
- 不挡路的装饰：不要挂 `WalkBlocker`
- 玩家、NPC、门入口：保持 trigger 或不参与阻挡

重点：

- 不要给整个 `City.prefab` 根节点挂一个巨大碰撞体
- 应该只给真正挡路的部分加小而明确的碰撞

## 九、动画怎么配

运行时通过 `ActorPresentation` 驱动角色表现。

如果角色 prefab 上有 `Animator`，建议控制器先至少有两个参数：

- `IsMoving`，`bool`
- `MoveSpeed`，`float`

最小动画树建议：

- `Idle`
- `Run`

可先用：

- `IsMoving == true` 切到 `Run`
- `IsMoving == false` 切回 `Idle`

角色模型朝向如果不对，优先检查 prefab 正前方是否正确。

## 十、加一个新场景时怎么做

例如你要新增 `ChurchScene`：

1. 新建 `Assets/Scenes/ChurchScene.unity`
2. 新建 `Assets/Resources/Prototype/ChurchScene.json`
3. 把 `ChurchScene` 加入 Unity Build Settings
4. 在场景里放玩家出生点 `SceneMarker`
5. 在场景里放 NPC 模型和 `ActorMarker`
6. 在场景里放门口 marker 和 `PortalMarker`
7. 在其他场景 JSON 中增加通往 `ChurchScene` 的 portal

## 十一、编译检查

推荐先用：

```bash
dotnet build Assembly-CSharp.csproj -nologo
```

如果要跑 Unity batch compile：

```bash
bash /Users/usr/Documents/unity_projects/dq99/scripts/check-unity-compile.sh
```

注意：

- 如果 Unity 编辑器已经打开了同一个项目，batch compile 会失败

## 十二、当前最容易出错的点

- 场景名和 JSON 文件名不一致
- 场景里缺少 `SceneMarker`
- `ActorMarker.ActorId` 和 JSON `id` 不一致
- `PortalMarker.PortalId` 和 JSON `id` 不一致
- 目标场景没有加入 Build Settings
- 整个场景根节点带了大 collider，导致玩家完全不能移动
- 模型没有 Animator，或者 Animator 参数名不匹配
