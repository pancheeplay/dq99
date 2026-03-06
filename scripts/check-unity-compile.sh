#!/usr/bin/env bash
set -euo pipefail

PROJECT_PATH="$(cd "$(dirname "$0")/.." && pwd)"
UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Unity.app/Contents/MacOS/Unity}"
LOG_PATH="${LOG_PATH:-$PROJECT_PATH/Logs/unity-batch-compile.log}"

if [[ ! -x "$UNITY_BIN" ]]; then
  echo "Unity executable not found: $UNITY_BIN" >&2
  echo "Set UNITY_BIN to your Unity binary, for example:" >&2
  echo "UNITY_BIN=/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity" >&2
  exit 1
fi

mkdir -p "$(dirname "$LOG_PATH")"

"$UNITY_BIN" \
  -batchmode \
  -quit \
  -nographics \
  -projectPath "$PROJECT_PATH" \
  -logFile "$LOG_PATH" \
  -executeMethod Dq99.Prototype.Editor.BatchCompileCheck.Run

STATUS=$?

if [[ -f "$LOG_PATH" ]]; then
  cat "$LOG_PATH"
fi

exit $STATUS
