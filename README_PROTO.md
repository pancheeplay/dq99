# Prototype Runtime Notes

Chinese setup guide:

- `/Users/usr/Documents/unity_projects/dq99/README_PROTO_CHINESE.md`

## Play Mode

- Movement: `WASD` or arrow keys
- Interact: `Z`, `Enter`, or `Space`
- Dialogue choices: `1` to `9`

## Batch Compile Check

Run:

```bash
bash /Users/usr/Documents/unity_projects/dq99/scripts/check-unity-compile.sh
```

If Unity is installed elsewhere, override `UNITY_BIN`:

```bash
UNITY_BIN="/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity" \
bash /Users/usr/Documents/unity_projects/dq99/scripts/check-unity-compile.sh
```

The full editor log is written to:

```text
/Users/usr/Documents/unity_projects/dq99/Logs/unity-batch-compile.log
```

## Scene Marker Authoring

- `SceneMarker`: stable spatial anchor, used by gameplay data
- `ActorMarker`: optional scene object for an NPC view, keyed by `ActorId`
- `PortalMarker`: optional scene object for a portal view, keyed by `PortalId`

Recommended authoring flow:

1. Place your town geometry normally in the Unity scene.
2. Create empty objects for logical anchors and add `SceneMarker`.
3. Set marker ids like `player_spawn_town`, `spawn_innkeeper`, `door_inn_outside`.
4. Keep NPC config, dialogue config, and portal routing in `Assets/Resources/Prototype/<SceneName>.json`.
5. If you want a custom NPC model or door effect, put `ActorMarker` or `PortalMarker` on that scene object and match the ids from data.

The runtime loads data by active Unity scene name. For example:

- `SampleScene.unity` -> `Assets/Resources/Prototype/SampleScene.json`
- `InnScene.unity` -> `Assets/Resources/Prototype/InnScene.json`
- `ShopScene.unity` -> `Assets/Resources/Prototype/ShopScene.json`

The runtime uses scene marker positions as the source of truth for gameplay placement.

## Scene Switching

- Portal data supports `destinationSceneName` and `destinationMarkerId`.
- Add target scenes to Unity Build Settings, otherwise `LoadSceneAsync` will fail.
- Each target scene needs its own `SceneMarker` objects and matching JSON file.

## Character Presentation

- Player prefabs should use `PlayerAvatarMarker`.
- NPC prefabs should use `ActorMarker`.
- Animator parameters expected by the current presenter:
  - `IsMoving` (`bool`)
  - `MoveSpeed` (`float`)
