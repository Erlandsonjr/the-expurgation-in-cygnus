---
name: SafeComponentCheck
description: 'Ensures the agent checks existing components before adding new ones to avoid duplicates. Use when: adding a component via AddComponent, modifying a prefab root, wiring a script to a GameObject, or any operation that could create duplicate MonoBehaviours or colliders.'
argument-hint: 'Target GameObject name or prefab path to inspect before modification'
---

# SafeComponentCheck

## When to Use

- Before calling `AddComponent` on any GameObject or prefab root.
- Before modifying a prefab that may already have the target component serialized.
- When wiring scripts, colliders, rigidbodies, or any component that would break if duplicated.

## Procedure

### Step 1 — Inspect Before Acting

Run `execute_code` to list all current components on the target object **before** making changes:

```csharp
// Example: inspect a scene object
var go = UnityEngine.GameObject.Find("TargetName");
return string.Join("\n", System.Array.ConvertAll(go.GetComponents<UnityEngine.Component>(), c => c.GetType().Name));
```

For a prefab:

```csharp
var root = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Path/To/Prefab.prefab");
try {
    return string.Join("\n", System.Array.ConvertAll(root.GetComponents<UnityEngine.Component>(), c => c.GetType().Name));
} finally {
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}
```

### Step 2 — Decide: Add or Modify

| Condition | Action |
|---|---|
| Component **not** present | Call `AddComponent<T>()` then configure via `SerializedObject` |
| Component **already present** | Obtain the existing instance via `GetComponent<T>()` and modify with `SerializedObject` — **do not add again** |

### Step 3 — Apply and Save

- Apply changes via `so.ApplyModifiedProperties()`.
- For prefabs use `SaveAsPrefabAsset` / `UnloadPrefabContents` in a `try/finally`.
- For scene objects mark dirty with `EditorSceneManager.MarkSceneDirty(...)` (guarded against play mode).

### Step 4 — Verify

Run a second `execute_code` pass to confirm the component count did not increase unexpectedly:

```csharp
var go = UnityEngine.GameObject.Find("TargetName");
return "Component count: " + go.GetComponents<UnityEngine.Component>().Length;
```

## Quality Criteria

- [ ] Component list retrieved before any `AddComponent` call.
- [ ] No duplicate entries of the same component type on any single GameObject.
- [ ] Prefab always saved with `SaveAsPrefabAsset` and unloaded in `finally`.
- [ ] Console checked for errors after the operation.
