# Unity CLI Integration for Claude Code

You have access to the `unity-cli` tool in the terminal to interact directly with the Unity Editor. Use these commands to inspect and modify the Unity project without writing explicit editor scripts.

## Core Commands
- `unity-cli editor play` : Enter play mode.
- `unity-cli editor stop` : Stop play mode.
- `unity-cli console` : Read Unity console logs (errors/warnings).
- `unity-cli console --filter error` : Read only errors.
- `unity-cli exec "<C# Code>"` : Execute arbitrary C# code in the Editor and return the result. If using multiple statements, you MUST include a `return` statement.
- `unity-cli reserialize <path>` : Fix YAML formatting after manually editing prefab/scene/material files in text mode.

## Execution Rules
- When you need to check the state of the Unity scene or hierarchy, ALWAYS use `unity-cli exec` instead of asking the user.
- All C# code executed via `unity-cli exec` runs on the Unity Main Thread.
- Example 1: `unity-cli exec "GameObject.FindObjectsOfType<Camera>().Length"`
- Example 2: `unity-cli exec "Selection.activeGameObject?.name ?? \"nothing selected\""`
- Example 3: `unity-cli exec "var go = new GameObject(\"AI_Generated\"); return go.name;"`