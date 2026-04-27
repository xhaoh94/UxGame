# UIMgr UI Framework Review

Date: 2026-04-27

## Scope

Reviewed files:

- `Unity/Assets/HotfixBase/Manager/UI/UIMgr.cs`
- `Unity/Assets/HotfixBase/Manager/UI/Handler/UICacheHandler.cs`
- `Unity/Assets/HotfixBase/Manager/UI/Handler/UIResourceHandler.cs`
- `Unity/Assets/HotfixBase/Manager/UI/Handler/UIStackHandler.cs`
- `Unity/Assets/HotfixBase/Manager/UI/UIObject.cs`
- `Unity/Assets/HotfixBase/Manager/UI/View/UIBase.cs`

## Findings

### P0

#### 1. Low-memory cleanup mutates `WaitDels` during iteration

Location:

- `Unity/Assets/HotfixBase/Manager/UI/Handler/UICacheHandler.cs:29-31`
- `Unity/Assets/HotfixBase/Manager/UI/UIMgrDefine.cs:213-228`

Problem:

- `UICacheHandler.ClearMemory()` iterates `WaitDels`.
- Each `WaitDel.Dispose()` removes the same key through `OnRemoveFromWaitDel`.
- This mutates the dictionary during `foreach`, which can throw at runtime.
- The risk is high because this path is used by `_LowMemory()` and `UIMgr.Release()`.

Recommendation:

- Drain a snapshot of keys or values before disposal.
- Or replace the `foreach` with a `while (WaitDels.Count > 0)` loop that removes one entry at a time.

### P1

#### 2. Package ref-counts leak on partial load failure

Location:

- `Unity/Assets/HotfixBase/Manager/UI/Handler/UIResourceHandler.cs:94-133`

Problem:

- `LoadUIPackage()` increments ref-counts for already loaded packages before all requested packages succeed.
- If a later package fails, the method returns `false` without rolling back the increments already made.
- Packages loaded earlier in the same call can also remain resident.
- This makes package lifetime non-transactional and will eventually block correct unload behavior.

Recommendation:

- Split the flow into `prepare`, `load`, `commit`.
- Record all ref-count changes and newly loaded packages for the current call.
- If any package fails, rollback all changes from the current request.

#### 3. Duplicate `Show` callers can fail after an arbitrary timeout

Location:

- `Unity/Assets/HotfixBase/Manager/UI/UIMgr.cs:299-313`

Problem:

- When a UI is already in `_showing`, later callers wait through polling.
- The polling exits after `_showTimeout` even if the first show is still valid and still running.
- Under cold resource load, long animations, or asset stalls, later callers can observe a false failure while the UI eventually opens successfully.

Recommendation:

- Replace polling with a per-id shared task or completion source.
- Let concurrent callers await the same show operation.
- Remove the fixed timeout from the normal dedup path.

#### 4. `UI_SHOW` is fired before the UI reaches a stable shown state

Location:

- `Unity/Assets/HotfixBase/Manager/UI/UIMgr.cs:240-251`
- `Unity/Assets/HotfixBase/Manager/UI/UIObject.cs:292-319`

Problem:

- `await ui.DoShow(...)` only waits until show logic has been scheduled.
- `UIObject._CheckShow()` finishes later, after animation state settles.
- `UIMgr` adds the UI into `_showed` and fires `UI_SHOW` before stack and blur callbacks are fully synchronized.
- Event listeners can observe a partially initialized shown state.

Recommendation:

- Clarify whether `UI_SHOW` means "start showing" or "fully shown".
- If it should mean "fully shown", delay event emission until `_CheckShow()` completes.
- If the existing semantic must remain, add a second event such as `UI_SHOWN`.

### P2

#### 5. `HideAll(ignoreList)` silently truncates the ignore set to 32 items

Location:

- `Unity/Assets/HotfixBase/Manager/UI/UIMgr.cs:429-460`

Problem:

- `HideAll(IList<int>)` and `HideAll(IList<Type>)` only copy the first 32 items into `_staticIgnoreArray`.
- Extra items are silently dropped.
- This is a fragile API contract because the caller gets no warning and behavior changes with list size.

Recommendation:

- Replace the fixed array with a pooled `HashSet<int>` or growable buffer.
- If the fixed-size optimization must remain, log or assert when input exceeds capacity.

#### 6. `CreateChildren()` relies on per-instance reflection

Location:

- `Unity/Assets/HotfixBase/Manager/UI/UIObject.cs:203-258`

Problem:

- Each UI instance resolves children, controllers, transitions, and component wrappers through reflection.
- `Activator.CreateInstance` is also used repeatedly for component creation.
- This increases first-open cost for large panels and repeated dynamic UI creation.

Recommendation:

- Cache binding metadata by UI type.
- Cache constructor delegates for component wrappers.
- Prefer generated binding code for high-frequency or large UIs.

#### 7. Hot-path collections still use linear lookup

Location:

- `Unity/Assets/HotfixBase/Manager/UI/UIMgr.cs:24-30`
- `Unity/Assets/HotfixBase/Manager/UI/Handler/UICacheHandler.cs:10-12`

Problem:

- `_showing` and `CreatedDels` are `List<int>`.
- The code frequently performs `Contains`, `Remove`, and repeated membership checks on them.
- This stays acceptable at small scale, but it does not age well as UI count and stack depth grow.

Recommendation:

- Convert hot membership sets to `HashSet<int>` or unify them into a single explicit UI state table.

## Suggested Optimization Order

1. Fix the `WaitDels` mutation bug in low-memory cleanup.
2. Make UI package loading transactional and rollback-safe.
3. Refactor the `Show` concurrency path to use shared tasks instead of polling and timeout.
4. Split or redefine show lifecycle events so `UI_SHOW` semantics are stable.
5. Remove the 32-item `HideAll` cap.
6. Optimize reflection-based child binding with metadata caching or code generation.
7. Replace hot-path linear collections with hash-based state tracking.

## Notes

- This review is based on static code inspection.
- No Unity runtime scene verification, animation validation, or memory profile was executed.
