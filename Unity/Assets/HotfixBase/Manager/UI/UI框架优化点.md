# UI框架优化点分析报告

## 更正说明

**重要更正**：在Unity C#中，`Dictionary<TKey,TValue>`和`List<T>`的`foreach`遍历**本身不会产生GC分配**，因为它们的Enumerator是struct类型。之前的分析有误，特此更正。

真正产生GC的地方主要是：**LINQ操作**、**Lambda闭包**、**字符串拼接**、**装箱操作**等。

---

## 一、性能优化

### 1.1 LINQ使用产生GC（2处）

**位置1**: `UIBaseFactory.cs:55`
```csharp
var keys = _waitDels.Keys.ToList();
foreach (var key in keys)
{
    if (!_waitDels.TryGetValue(key, out var ui) || ui.GetType() != type) continue;
    _waitDels.Remove(key);
    return key;
}
```
**问题**: `Keys.ToList()`创建新的List对象，产生GC。
**优化建议**: 直接遍历字典，避免LINQ转换。
```csharp
// 优化后
foreach (var kv in _waitDels)
{
    if (kv.Value.GetType() != type) continue;
    _waitDels.Remove(kv.Key);
    return kv.Key;
}
```

**位置2**: `UIResourceHandler.cs:100`
```csharp
foreach (var handle in handles.Where(handle => handle.IsValid))
{
    handle.Release();
}
```
**问题**: `Where`产生委托和迭代器对象，产生GC。
**优化建议**: 使用`for`循环手动过滤。
```csharp
// 优化后
for (int i = 0; i < handles.Count; i++)
{
    var handle = handles[i];
    if (!handle.IsValid) continue;
    handle.Release();
}
```

### 1.2 Lambda闭包产生GC（5处）

**位置**: `UIObject.cs:153, 239`
```csharp
ShowAnim?.Play(() => { State = UIState.Show; });
HideAnim?.Play(() => { State = UIState.Hide; });
```
**问题**: Lambda捕获`this`指针，每次调用都创建新的委托实例。
**优化建议**: 缓存委托或使用无捕获lambda。
```csharp
// 优化方案：缓存委托
private Action _onShowComplete;
private Action _onHideComplete;

protected override void OnInit()
{
    _onShowComplete = OnShowCompleteInternal;
    _onHideComplete = OnHideCompleteInternal;
}

private void OnShowCompleteInternal() { State = UIState.Show; }
private void OnHideCompleteInternal() { State = UIState.Hide; }

// 使用时
ShowAnim?.Play(_onShowComplete);
```

**位置**: `UIMgr.cs:115-117`
```csharp
uis.ForEach(ui => { ui.Add(_idUIData); });
uis.ForEach(ui => { ui.Parse(_idUIData); });
itemUrls.ForEach(item => { item.Add(_itemUrls); });
```
**问题**: `ForEach`+Lambda产生委托分配。
**优化建议**: 使用普通`foreach`或`for`循环。
```csharp
// 优化后
for (int i = 0; i < uis.Count; i++)
{
    uis[i].Add(_idUIData);
    uis[i].Parse(_idUIData);
}
for (int i = 0; i < itemUrls.Count; i++)
{
    itemUrls[i].Add(_itemUrls);
}
```

**位置**: `UIMgr.cs:587-594`
```csharp
.WithBtn1("下载", () =>
{
    _idDownloader.Add(id, download);
    download.BeginDownload(_DownloadComplete, new DownloadData(id, param, isAnim));
});
```
**问题**: Lambda捕获`id`, `download`, `param`, `isAnim`多个变量，产生闭包对象。
**优化建议**: 使用结构体传递参数。
```csharp
// 优化方案
private struct DownloadParams
{
    public int Id;
    public Downloader Download;
    public IUIParam Param;
    public bool IsAnim;
}

private void OnDownloadBtnClick(DownloadParams p)
{
    _idDownloader.Add(p.Id, p.Download);
    p.Download.BeginDownload(_DownloadComplete, new DownloadData(p.Id, p.Param, p.IsAnim));
}
```

### 1.3 字符串拼接产生GC（频繁调用时）

**位置**: `UIData.cs:69`
```csharp
public string Name => $"{CType.FullName}_{ID}";
```
**问题**: 每次访问都创建新字符串，频繁调用时产生GC。
**优化建议**: 在构造函数中计算并缓存。
```csharp
// 优化后
public string Name { get; private set; }

public UIData(int id, Type type, IUITabData tabData = null)
{
    ID = id;
    CType = type;
    Name = $"{CType.FullName}_{ID}"; // 只计算一次
    // ...
}
```

**位置**: `UIMgr.cs:586, 589` - 日志字符串
```csharp
Log.Debug($"一共发现了{download.TotalDownloadCount}个资源需要更新下载。");
.WithContent($"一共发现了{download.TotalDownloadCount}个资源需要更新下载。")
```
**问题**: 频繁日志调用时产生GC。
**优化建议**: 使用`StringBuilder`或条件编译。

### 1.4 装箱操作产生GC

**位置**: `UIParam.cs:91, 132, 141, 184, 193, 198`
```csharp
if (_a is T a)
{
    t = a;
    return true;
}
```
**问题**: 泛型约束为值类型时，`is`检查可能导致装箱。
**优化建议**: 使用`EqualityComparer<T>.Default`或限制引用类型。

### 1.5 委托+=操作产生新委托实例

**位置**: `UIBase.cs:34-35`
```csharp
OnHideCallBack += _Hide;
OnShowCallBack += _Show;
```
**问题**: 每次`InitData`都执行`+=`，创建新的委托实例。
**优化建议**: 确保只在初始化时添加一次，或使用字段缓存。
```csharp
// 优化方案：检查是否已添加
if (OnHideCallBack == null)
{
    OnHideCallBack = _Hide;
    OnShowCallBack = _Show;
}
```

### 1.6 异步任务Token创建

**位置**: `UIBase.cs:159, 210`
```csharp
_showToken = new CancellationTokenSource();
_hideToken = isAnim && HideAnim != null ? new CancellationTokenSource() : null;
```
**问题**: 频繁创建和销毁`CancellationTokenSource`。
**优化建议**: 复用Token或使用`CancellationToken`池。

---

## 二、内存优化

### 2.1 对象池使用不当

**位置**: `UIParam.cs:17-34`
```csharp
public static IUIParam Create<A>(A a)
{
    var p = Pool.Get<UIParam<A>>();
    p.Init(a, true);
    return p;
}
```
**问题**: 泛型参数不同会导致不同类型（如`UIParam<int>`和`UIParam<float>`），可能无法正确池化或池子过多。
**优化建议**: 限制常用类型组合或使用非泛型包装。

### 2.2 事件监听器Key冲突风险

**位置**: `UIEvent.cs:51, 74`
```csharp
if (_mulClickEvtList.ContainsKey(RuntimeHelpers.GetHashCode(gObject))) return;
```
**问题**: `RuntimeHelpers.GetHashCode`可能冲突（不同对象相同hash）。
**优化建议**: 使用对象引用作为key或添加唯一ID。
```csharp
// 优化方案
private Dictionary<GObject, UIMultipleClickEventData> _mulClickEvtList;
// 或使用对象ID
private Dictionary<int, UIMultipleClickEventData> _mulClickEvtList; // ID从对象获取
```

### 2.3 字典容量预分配

**位置**: `UIMgr.cs:37-44`
```csharp
private readonly Dictionary<UILayer, GComponent> _layerCom = new Dictionary<UILayer, GComponent>()
{
    { UILayer.Root, GRoot.inst },
    { UILayer.Bottom, _CreateLayer(UILayer.Bottom, -100) },
    { UILayer.Tip, _CreateLayer(UILayer.Tip, 200) },
    { UILayer.Top, _CreateLayer(UILayer.Top, 300) }
};
```
**优化建议**: 预分配容量避免扩容。
```csharp
private readonly Dictionary<UILayer, GComponent> _layerCom = new Dictionary<UILayer, GComponent>(4)
```

---

## 三、代码结构优化

### 3.1 循环嵌套过深

**位置**: `UIMgr.cs:196-259` - `_ShowAsync<T>`方法
**问题**: 方法逻辑复杂，嵌套层次深，可读性差。
**优化建议**: 提取子方法，简化主流程。

### 3.2 重复代码

**位置**: `UIMgr.cs:362-407` - `_HideAll`和`_HideAllWithSet`
**问题**: 两个方法逻辑几乎相同。
**优化建议**: 合并为一个方法，使用不同的过滤策略。

### 3.3 魔法数字

**位置**: 
- `UIBase.cs:21` - `public virtual int HideDestroyTime => 60;`
- `UIMgr.cs:14` - `const float _showTimeout = 5f;`
- `UIEvent.cs` - 多次点击默认参数 `gapTime = 0.3f`

**优化建议**: 定义为常量或配置项。
```csharp
public static class UIConfig
{
    public const int DefaultHideDestroyTime = 60;
    public const float ShowTimeout = 5f;
    public const float DefaultClickGapTime = 0.3f;
}
```

### 3.4 空检查冗余

**位置**: `UIObject.cs:330-450` - 事件相关方法
**问题**: 多层方法调用都有空检查。
**优化建议**: 在入口点统一检查，内部方法假设非空。

---

## 四、线程安全优化

### 4.1 异步加载竞态条件

**位置**: `UIResourceHandler.cs:141-162`
```csharp
if (_pkgToLoading.Contains(pkg))
{
    // 等待其他线程加载完成
    while (_pkgToLoading.Contains(pkg))
    {
        await UniTask.Yield();
    }
    // ...
}
```
**问题**: 自旋等待浪费CPU，且可能有竞态条件。
**优化建议**: 使用`TaskCompletionSource`或`AsyncLock`进行协调。
```csharp
// 优化方案
private Dictionary<string, TaskCompletionSource<bool>> _pkgLoadingTasks = new();

public async UniTask<bool> LoadUIPackage(string[] pkgs)
{
    // ...
    if (_pkgToLoading.Contains(pkg))
    {
        if (!_pkgLoadingTasks.TryGetValue(pkg, out var tcs))
        {
            tcs = new TaskCompletionSource<bool>();
            _pkgLoadingTasks[pkg] = tcs;
        }
        var success = await tcs.Task;
        // ...
    }
}
```

### 4.2 字典并发访问

**位置**: `UIMgr.cs:25-28`
```csharp
private readonly Dictionary<int, AutoResetUniTaskCompletionSource<IUI>> _pendingShows = new Dictionary<int, AutoResetUniTaskCompletionSource<IUI>>();
```
**问题**: 异步方法中访问字典，没有同步机制。
**优化建议**: 使用`ConcurrentDictionary`或添加锁。

---

## 五、 FairyGUI 相关优化

### 5.1 资源加载字符串格式化

**位置**: `UxLoader.cs:16`
```csharp
var sa = ResMgr.Ins.LoadAsset<SpriteAtlas>(string.Format(PathHelper.Res.Atlas,"items"), YooType.Main);
```
**问题**: 每次加载都格式化字符串。
**优化建议**: 缓存格式化字符串或使用常量。

### 5.2 组件查找优化

**位置**: `UIDialog.cs:20-25`
```csharp
protected virtual GTextField __txtTitle { get; private set; } = null;
protected virtual GTextField __txtContent { get; private set; } = null;
```
**问题**: 虚属性每次访问可能有开销（虽然FairyGUI内部有缓存）。
**优化建议**: 在`CreateChildren`中一次性查找并缓存到字段。

---

## 六、优化优先级

### 6.1 高优先级（运行时频繁调用）
1. ✅ **Lambda闭包** - 动画回调、ForEach
2. ✅ **LINQ操作** - `Where`, `ToList()`
3. ✅ **字符串拼接** - `Name`属性频繁访问
4. ✅ **异步竞态条件** - 自旋等待

### 6.2 中优先级（中等频率）
1. 对象池泛型处理
2. 委托缓存管理
3. 字典容量预分配
4. 复杂方法拆分

### 6.3 低优先级（初始化/低频）
1. 魔法数字常量化
2. 空检查策略统一
3. 代码风格统一

---

## 七、具体优化代码示例

### 优化1: 移除LINQ
```csharp
// 优化前
var keys = _waitDels.Keys.ToList();
foreach (var key in keys)

// 优化后
foreach (var kv in _waitDels)
{
    var key = kv.Key;
    var ui = kv.Value;
    // ...
}
```

### 优化2: 缓存Lambda委托
```csharp
// 优化前
ShowAnim?.Play(() => { State = UIState.Show; });

// 优化后
private Action _onShowComplete;
protected override void OnInit()
{
    _onShowComplete = () => State = UIState.Show;
}
// 使用时
ShowAnim?.Play(_onShowComplete);
```

### 优化3: 字符串缓存
```csharp
// 优化前
public string Name => $"{CType.FullName}_{ID}";

// 优化后
public string Name { get; private set; }
public UIData(int id, Type type, ...)
{
    Name = $"{type.FullName}_{id}";
    // ...
}
```

### 优化4: 异步加载同步
```csharp
// 优化前
while (_pkgToLoading.Contains(pkg))
{
    await UniTask.Yield();
}

// 优化后
if (_pkgLoadingTasks.TryGetValue(pkg, out var tcs))
{
    await tcs.Task;
}
```

---

## 总结

**真正需要关注的GC来源**:
1. **LINQ操作** - 每次调用都分配
2. **Lambda闭包** - 捕获变量时分配
3. **字符串拼接** - 频繁调用时分配
4. **装箱操作** - 值类型转引用类型
5. **Task/Token创建** - 异步操作频繁时

**无需担心的地方**:
- ✅ `Dictionary`/`List`的`foreach`遍历 - 无GC
- ✅ 普通方法调用 - 无GC
- ✅ 属性访问（非字符串拼接）- 无GC

建议优先处理高优先级优化项，可显著减少GC压力和提升运行效率。
