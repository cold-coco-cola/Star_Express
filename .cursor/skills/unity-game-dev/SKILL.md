---
name: unity-game-dev
description: Guides Unity game development with scene-first design, minimal editor scripts, root-cause debugging, and cleanup. Use when developing Unity games, fixing bugs, handling DontDestroyOnLoad/scene switching, or when the user mentions Unity workflows, scene persistence, or direct fixes.
---

# Unity Game Development Rules

## Development Philosophy

### Prefer Scene-Created GameObjects
- Create GameObjects in scenes/prefabs, not purely at runtime
- Designers can adjust positions in the editor
- Use `[SerializeField]` instead of `Find` for references
- Avoid hardcoded positions

### Minimize Editor Scripts
- No menu bar extensions
- Use MonoBehaviour Awake/Start for initialization
- Use ScriptableObject for configuration
- Keep implementations simple and direct

### Identify Root Cause Before Acting
- Logic bugs often have subtle causes
- Trace the full flow before modifying
- Understand edge cases (loops, empty state, etc.)
- Fix root cause, not symptoms

### Clean Up After Changes
- Delete obsolete scripts after refactoring
- Remove unused assets
- Keep project structure tidy
- Remove redundant objects, components, and code
- Ensure in-game display matches scene hierarchy

---

## Direct Fix Mode (unity-direct-fix)

When the user reports a bug or asks for a fix:

### Workflow
1. Use search tools to locate relevant code
2. Read related files to understand root cause
3. Modify code directly without asking for confirmation
4. Delete obsolete files (if any)
5. Check for related issues

### User Preferences
- Modify code directly, do not write prompts
- Do not ask many questions; make reasonable assumptions and execute
- Fix root cause immediately once found
- Only explain when explicitly requested; otherwise do not modify files

### Response Format
```
Root cause: [问题的根源]

Fixed in [filename]:
- [changes made]

Deleted: [obsolete files] (if any)
```

---

## Scene Persistence Mode (unity-scene-persistence)

Handling DontDestroyOnLoad and scene switching:

### Common Fixes

**1. UI Persisting Across Scenes**
```csharp
private void HideNonLevelPopups()
{
    var pauseMenu = FindObjectOfType<PauseMenu>(true);
    if (pauseMenu != null) pauseMenu.Hide();
}
```

**2. Event Listeners Stale**
```csharp
// Rebind on each scene load
btn.onClick.RemoveAllListeners();
btn.onClick.AddListener(() => { /* handler */ });
```

**3. Popup Not Hidden**
```csharp
private void OnBackToMenu()
{
    Hide();  // Must call before LoadScene
    SceneManager.LoadScene(sceneName);
}
```

**4. Missing Component Check**
```csharp
if (existing.continueButton == null || existing.backToMenuButton == null)
{
    DestroyImmediate(existing.gameObject);
    // Recreate
}
```

### Scene Switch Checklist
- [ ] Hide all popups before LoadScene
- [ ] Destroy GameManager if needed
- [ ] Rebind button listeners in new scene
- [ ] Check for stale UI references
- [ ] Reset static state

---

## Additional Resources

- For game logic patterns (state machines, pathfinding, route management), see [reference.md](reference.md)

---

## General Guidelines

- Operate like a real game developer
- Remove redundant objects, components, and code after changes
- Keep the project efficient
- Ensure in-game object display matches the scene hierarchy
