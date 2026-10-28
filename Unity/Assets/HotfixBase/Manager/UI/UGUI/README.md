# UGUI 虚拟列表

核心 `UGUIVirtualList` 是普通 C# 类。`UGUIVirtualListComponent` 是可选的 Inspector / 生命周期适配层。

## Inspector 配置

1. 在带有 `ScrollRect` 的节点上 Add Component → **Ux / UI / UGUI Virtual List**（也可挂在页面其他节点）。
2. 拖入 `Scroll Rect`，并确保 ScrollRect 自己的 `Viewport`、`Content` 已赋值。
3. 选择「列表类型」：**普通列表 / 树形列表 / 分页列表 / 分组列表**。这些是数据类型，不是尺寸布局模式。
4. 选择「尺寸模式」：**固定尺寸 / 可变尺寸**，两种列表类型都支持。
5. 选择「尺寸来源」：`Manual` 手动指定，或 `FromTemplate` 读取默认模板根尺寸。固定模式使用精确宽高；可变模式中主轴为初始估算值，交叉轴固定。
6. 普通/分页列表可以选择滚动方向、多列/多行及可选数量上限。树形/分组固定竖向单列；分组提供空组显示和吸顶开关。分页提供每页数量、自动预加载和状态点击选项。特殊数据类型隐藏聊天末尾跟随策略，避免投影变化时跳走当前内容。
7. 在 `Item Templates` 添加元素，填写 `Type Id`，把 **Hierarchy 中的 GameObject** 或 **Project 中的 Prefab** 拖入 `Template`。
8. `Default Item Type` 必须对应某个模板的 TypeId；只有一个模板时通常都用 0。

**注意**：同一个 ScrollRect 只能由一个列表控制。目前 `UGUIVirtualListDemo` 已改为调用本组件提供数据，可以与本组件同时保留；不要再额外创建第二个核心列表。

### 如果 Inspector 仍显示 Layout Mode / Fixed Grid 等原始字段

正常面板使用「尺寸模式」「启用多列/多行」等中文标签，尺寸模式只有固定/可变两项。旧 `FixedGrid` 枚举仅用于兼容资源，正常面板不会提供它。

如果看到原始字段，请确认 Inspector 窗口右上角菜单为 **Normal**，不是 **Debug / Debug-Internal**。Debug 会绕过自定义 Inspector，条件隐藏也不会按正常面板执行。

选中 Canvas 后也可执行 `UxGame/UI/刷新虚拟列表 Inspector`，恢复目标窗口的 Normal 模式并重建 Editor 缓存，不需要删除组件或重新配置数据。锁定到其他对象的窗口请先解锁或切回目标列表。

### 模板规则

- 模板根节点必须为 `RectTransform`。
- 场景模板初始化时自动隐藏，生成的每个 Item 都是克隆；`Release` 后恢复模板原来的激活状态。
- Project Prefab 只用于 Instantiate，不会修改资源、不销毁原资源。
- 生成的 Item 可被池化；场景模板本身不加入池。
- 可以混合多个场景模板及 Prefab。根节点可选挂 `IUGUIVirtualListItem` 实现；没有 Item 脚本时，用组件的 `ItemRenderer` 回调绑定。
- 根节点有 `Button` 时自动转发点击，无 Button 时不产生点击回调。
- `Content` 上不要挂 `LayoutGroup` / `ContentSizeFitter`。Item 内部允许局部布局组件。
- 原型节点不要挂列表组件自身，也不要把 ScrollRect、Viewport、Content 及其祖先当作模板。

## 代码调用

```csharp
using Ux;
using UnityEngine;
using UnityEngine.UI;

public sealed class MessagePanel : MonoBehaviour
{
    [SerializeField] private UGUIVirtualListComponent _list;

    private void Start()
    {
        // 单模板可不设置 ItemTypeProvider。
        _list.ItemRenderer = (index, data, item) =>
        {
            item.transform.Find("Text").GetComponent<Text>().text = (string)data;
        };
        _list.ItemClicked += (index, data) => Debug.Log($"点击 {index}: {data}");
        _list.SetDatas(new[] { "第一条消息", "第二条消息", "第三条消息" });
    }
}
```

Text 示例适用于传统 UGUI Text；若模板用 TMP，请绑定 `TMP_Text`。

多模板：Inspector 先配置不同 TypeId，再设置回调：

```csharp
_list.ItemTypeProvider = (index, data) => ((Message)data).IsSystem ? 1 : 0;
_list.ItemRenderer = BindMessage;
_list.SetDatas(messages);
```

每次绑定可能是新数据或旧对象复用，必须完整覆盖上条数据的显示状态。需要时订阅 `ItemRecycled` 取消图片加载等异步操作。

## 增量数据更新

不再需要每次用 `SetDatas` 全量重建：

```csharp
int index = _list.AddData(message);    // 追加一条，返回追加位置
_list.AddDatas(nextPage);             // 批量追加
_list.InsertData(0, message);         // 在指定位置插入
_list.InsertDatas(0, previousPage);   // 聊天历史消息/分页头插
_list.RemoveAt(index);
_list.RemoveRange(startIndex, count);
_list.UpdateData(index, updatedData); // 替换并重新绑定指定项
_list.Clear();                       // 清空数据，保留对象池
```

这些接口支持单列、多列、横向/竖向以及固定/可变尺寸。`SetDatas` 仍表示全量替换、重置尺寸缓存并回到起点；增量接口不会这样做。

### 绑定、复用与缓存

- 列表持有数据集合的快照；增量接口修改的是列表内部集合，不会反向修改传入的 `List<T>`。
- 未移动索引、数据未变化、类型也没变的当前项，不重新 Bind，原异步令牌保持有效。
- 插入/删除导致索引移动的项，会更新绑定并让旧令牌失效。对象类型相同时尽量复用原实例，不做全量销毁/重建。
- `ItemTypeProvider` 会重新评估可见项，支持基于索引、数据或数量选择模板。
- 旧尺寸缓存随数据一起移动；新增项先用估算尺寸，进入缓冲区后再测量。
- 这些普通增量接口用于「普通列表」类型；树形组件请使用树接口，避免绕过层级管理。
- `UpdateData` 只使目标项的尺寸失效；不可见项等下次进入视口再绑定，不为此创建隐藏测量对象。
- 自定义布局可实现 `IUGUIVirtualListMutableLayout` 来维护插入/删除/失效缓存。未实现时安全降级为重置布局缓存，并重测当前已创建项。

### 变更后的滚动位置

- 每次操作前以首个真正可见项及其偏移为锚点，插入/删除后跟随同一条旧数据的新索引。
- 锚点被删除时优先使用后继项；尾部无后继时使用前一项，最终合法边界优先。
- 多列插入不是整行数量时，项会重新分组，交叉轴位置可能改变；下一次操作会以当时的首个可见项重新捕获锚点，不承诺两次逆操作后恢复更早的第一项。
- Inspector 的 **增量更新前在末尾时保持末尾** 默认关闭。开启后，原本就在末尾才跟随新末尾，历史消息浏览位置不会被强制拉走。
- 该开关与 **尺寸变化时保持末尾** 独立：前者覆盖追加/更新过程及新项测量，后者控制后续图片异步变高等情况。
- `Clear` 保留可复用对象及模板引用；需要释放实例和资源时调用组件 `Release()` 或核心 `Dispose()`。

### 异常与生命周期

- 无效索引/范围、空的增量数据源会报错。数据源会先完整枚举，枚举失败时不会先清空旧数据。
- 不要在 Bind、Measure、ItemRecycled 或数据枚举回调中增删/替换列表数据；请延后执行。普通 ItemClicked 回调中可以执行增删。
- 绑定失败的项会标记为待重绑，其点击和异步令牌无效；修复回调后可 `RefreshLayout()` 或 `RefreshItem(index)` 重试。
- 回收失败不会把已移出 active 的对象遗留在场景里。全量 SetDatas 的回收失败时，旧数据数量与旧布局数量仍保持一致，可修复回调后刷新/重试。
- 在 Bind/回收期间请求释放会延后到本轮安全位置，旧令牌立即失效。释放回调中禁止通过 facade 重新初始化；需要重新打开请等释放结束后再调用。
- 释放时会继续清理其他项，再向调用方报告回调异常，而不是遇到第一项异常就停止所有清理。

### 增量 Demo

运行时在 Canvas 的 `UGUIVirtualListDemo` 组件右键菜单中测试：

- `测试 / 增量追加10条`
- `测试 / 头部插入10条历史数据`
- `测试 / 删除首个可见项`
- `测试 / 更新首个可见项`
- `测试 / 清空数据（保留对象池）`

先滚动到中间，再头插可观察锚点保持。固定多列 Demo 中单项更新会增加显示的数量；消息 ID 保持稳定，便于区别数据身份与当前索引。

## 树形列表（纯 C# 数据适配）

在组件 Inspector 将「列表类型」选为 **树形列表**，组件会创建并拥有 `UGUIVirtualTreeAdapter`，把当前展开的节点投影成扁平行，继续使用同一套列表、模板回调、尺寸测量和对象池。

组件树模式固定为竖向单列，固定/可变尺寸仍可选择；隐藏的多列数量、网格间距、横向配置及聊天末尾策略均不会参与运行。缩进/箭头由 Renderer 使用 Depth / IsExpanded 绘制。

**选择树形不会自动生成业务节点。** 请调用 `SetTreeRoots` 提供数据；要看完整示例可使用 `Create UGUI Tree Demo` 菜单。

### 节点与行数据

```csharp
var roots = new[]
{
    new UGUIVirtualTreeNode("weapons", "武器", new[]
    {
        new UGUIVirtualTreeNode("weapons/sword", "剑"),
        new UGUIVirtualTreeNode("weapons/bow", "弓")
    }),
    new UGUIVirtualTreeNode("potions", "药水")
};
// Inspector 已选择树形时不必再调用；代码配置应在初始化前，非空列表切类型须先 Release。
component.ConfigureListType(UGUIVirtualListType.Tree);

// 模板 0：有子节点的分类；模板 1：叶子。也可以只用一种模板，省略此回调。
component.ItemTypeProvider = (index, data) => ((UGUIVirtualTreeItem)data).HasChildren ? 0 : 1;
component.ItemRenderer = (index, data, view) =>
{
    var row = (UGUIVirtualTreeItem)data;
    // row.Id / row.Data / row.Depth / row.ParentId / row.HasChildren / row.IsExpanded
    // 根据 Depth 设置缩进，根据 IsExpanded 绘制箭头，再绑定业务数据 row.Data。
};
// 默认已自动展开/折叠，再触发业务事件。不要在这里重复 Toggle，否则会切回原状态。
// 页面实际使用时应保留委托，以便退出时取消订阅。
component.ItemClicked += (index, data) =>
{
    var row = (UGUIVirtualTreeItem)data;
    // 处理业务选中；row.IsExpanded 已是切换后的值。
};
component.SetTreeRoots(roots);
```

`Id` 必须在整棵森林中全局唯一、区分大小写；可以用完整路径或业务 GUID。根节点 Depth=0。节点构造时复制子节点集合，Id/Children/初始展开标记之后不变；同一个节点不能同时挂在两个父节点下。可让两个不同 Id 的节点共享同一业务 Data 对象。

交给 `ItemRenderer` / `ItemTypeProvider` / `ItemClicked` 的 data 是 **`UGUIVirtualTreeItem`**，不是业务数据本身；通过 `row.Data` 获取业务对象。

### 展开、定位与更新

```csharp
component.ExpandTreeNode("weapons");
component.CollapseTreeNode("weapons");
component.ToggleTreeNode("weapons");
component.SetTreeNodeExpanded("weapons", true);
int index = component.GetTreeVisibleIndex("weapons/sword"); // 展开投影中不存在时为 -1

component.ScrollToTreeNode("weapons/sword"); // 自动展开祖先，然后按节点 Id 定位
component.ScrollToTreeNode("weapons/sword", expandAncestors: false); // 隐藏时返回 false

component.UpdateTreeNodeData("weapons/sword", updatedSword);
component.RefreshTreeNode("weapons/sword"); // 外部修改 Node.Data 或业务对象后，通知重新绑定
component.Clear(); // 树模式清空根节点，仍保持树数据源归属
```

- 展开只插入该节点当前应该显示的子孙区间；折叠只删除对应区间，不调用全量 SetDatas。
- 父行箭头只重绑父行；前方没移动的行保留绑定，索引发生移动的后方行按现有规则更新绑定和令牌。
- 折叠父节点不会清掉隐藏子分支的展开状态，重新展开会恢复；也可以在祖先折叠时修改隐藏子分支的展开意图。
- 不存在的 Id、叶子节点、重复的展开/折叠请求返回 false，不修改投影。
- 程序化折叠正在浏览的祖先时，会把父行带回视口，而不是停到无关的后继分支。
- 可见结构的展开/折叠暂不套用聊天的“增量更新保持末尾”策略，避免点击最后一个分类却跳到整个子树末端；原策略值会保留。
- 树中隐藏节点的数据更新不会创建 GameObject；已展开但不在视口的行也等下次复用时渲染。
- 子节点被折叠后，原 UI 令牌会失效；再次展开后须获取新的令牌。异步 UI 加载仍用列表的绑定令牌保护。

`VisibleItems` 是展开后的行，不等于视口内实际实例；`VisibleCount` 是其数量，`TotalNodeCount` 是全部注册模型数量。虚拟化只减少 UI 实例，不会懒加载所有业务树模型。

### 数据源与生命周期

- 树的展开、折叠、定位和数据更新都应在 Unity 主线程执行，不提供跨线程同步。
- 组件树模式禁止 `component.AddData/SetDatas/UpdateData/RemoveAt` 等普通数据接口，避免破坏层级。`Clear()` 会走清空树；直接绕过 facade 修改 Controller 仍会被适配器版本号检测到。
- 列表的滚动、刷新布局、尺寸通知仍然可以正常使用。
- 完整替换结构使用 `component.SetTreeRoots(newRoots, preserveExpansion: true)`，按 Id 保留展开状态；这仍是全量替换并回到起点。`component.SetTreeRoots(null)` 表示清空树。
- 每次 SetRoots 都先校验全树；重复 Id、共享节点、空节点或枚举失败不会先覆盖旧投影。
- 提交前回收失败时恢复旧投影；提交后 Bind / Measure 错误保留已经提交的数据，修复 Renderer 后 `component.RefreshLayout()` 可以重试。
- 组件树模式由组件拥有适配器并自动处理可选的点击展开，页面退出时解绑自己的 ItemRenderer / ItemTypeProvider / ItemClicked，再 `component.Release()`。不要单独 Dispose `component.TreeAdapter`。
- 节点级 facade 调用在 Release 后返回 false，不会因旧回调而重新创建空列表；显式 `SetTreeRoots` / `TreeAdapter` 访问可以初始化。
- 组件可以通过 `TryGetTreeAdapter` 无副作用查询适配器；`TreeAdapter` 属性用于显式访问/初始化。
- 仍保留纯 C# 手工方式：`new UGUIVirtualTreeAdapter(core)`，适用于不用组件或自定义布局的调用方。该 Adapter 不拥有 Core，Dispose 不会 Clear / Dispose Core，点击和外层释放由调用方负责；不要在组件树模式再创建第二个适配器。

当前树结构在构造后固定，还没有异步加载子节点、节点拖拽排序、树内增删节点、搜索过滤或连线图。结构变化暂用新模型 SetRoots；后续可以在适配层扩展，不侵入核心布局。

### 树形 Demo

退出运行后执行 `UxGame/Test/Create UGUI Tree Demo`，再运行场景：

- 默认注册 2120 个节点，初始展开行 45 条，只创建视口和缓冲区附近的 Item。
- 点击 `[-]` / `[+]` 分类或分组行折叠/展开，叶子点击只打印节点信息。
- 分类和叶子使用两个模板、不同的默认高度；缩进由 Depth 决定，复用时每次重置。
- 在 `UGUIVirtualTreeDemo` 组件菜单测试展开/折叠第一个分类、定位最后叶子、更新第一个叶子文本。
- Demo 菜单同时设置「列表类型」并互斥启用树形或普通列表脚本，不让两个控制器抢同一个 ScrollRect。切回普通聊天/多列 Demo 使用原菜单即可。
- 未配置类型的旧树 Demo 场景会在 Inspector / 启动时迁移；用户明确选择的普通类型不会被强制改回。
- 空场景中菜单会按当前输入系统补齐 EventSystem；已有可用 EventSystem 则复用。

## 分页数据源

在 Inspector 选择 **分页列表**，设置：

- **每页数量**：传给加载函数的请求数量，至少 1。
- **接近末尾自动加载**：依据实际视口判断，不依据 Buffer 中预先创建的对象。
- **预加载项数**：离已加载内容末尾还剩多少条业务数据时请求，不是像素；关闭自动加载后隐藏且不生效。
- **点击状态项加载/重试**：默认开启，点击尾项自动请求/重试。状态点击被组件消费，不触发业务 ItemClicked，防止同步返回后尾项索引已经移动。

分页支持现有固定/可变尺寸、横向/竖向和多列布局。**尾部状态项占一个普通单元格，不会跨满一整行**；需要跨行 Footer 的布局尚未实现。

### 绑定异步加载函数

```csharp
using System.Threading;
using System.Threading.Tasks;

// 先配置 Renderer / 类型回调。data 是 UGUIVirtualPageItem，不是直接的业务对象。
_list.ItemTypeProvider = (index, data) => ((UGUIVirtualPageItem)data).IsStatus ? 1 : 0;
_list.ItemRenderer = RenderPageRow;

await _list.SetPageLoader(async (pageIndex, pageSize, token) =>
{
    // pageIndex 从 0 开始；服务端从 1 开始时可传 pageIndex + 1。
    var result = await RequestPage(pageIndex, pageSize, token);
    token.ThrowIfCancellationRequested();
    return UGUIVirtualPage.Create(result.Items, result.HasMore);
});
```

加载器是 `Func<int, int, CancellationToken, Task<UGUIVirtualPage>>`，不绑定某个网络插件。项目使用 UniTask 时可在 async Task 加载函数里 await UniTask，或按需 AsTask。

- 由加载结果明确返回 HasMore，不根据是否满页猜测结束。
- `HasMore=true` 时不允许返回空页，防止数据不前进而无限自动加载。
- 页号只在有效页提交后推进；网络失败重试原页，连续请求只启动一个加载器并共享同一任务。
- 不按业务 Id 自动去重，重复业务记录的处理由服务端/加载函数负责。
- 创建和调用必须在 Unity 主线程；待完成的异步任务需要 Unity SynchronizationContext，不能从线程池直接操作 UI。

### 渲染业务行与状态行

`UGUIVirtualPageItem` 包含：

- `Kind`：Data / LoadMore / Loading / Error / End。
- `IsStatus`：是否为尾项。
- `Data`：业务记录，状态项为 null。
- `DataIndex`：业务记录的索引；状态项为 -1。
- `Message` / `Error`：状态提示和网络异常。

可以按 Kind 使用更多模板，也可像 Demo 一样业务一套、状态共用一套。状态依次显示点击加载、加载中、失败重试、没有更多；空的最终结果显示暂无数据。

组件 `Count` 包含尾项，业务数量请用 `LoadedPageDataCount`；适配器用 `LoadedCount`。不同模板高度仍由可变布局测量。

```csharp
await _list.LoadNextPageAsync();
await _list.RetryPageAsync();        // 仅失败状态重试；自动加载不会反复重试错误页
await _list.RefreshPagesAsync();     // 取消旧代、清空结果、从第0页开始
_list.RefreshPagePresentation();     // 修复 Renderer 后只重试视图，不重复请求已提交页
```

返回 true 表示该页已经成功提交；完成、取消、网络失败或无法启动返回 false。配置/调用错误和 Renderer 错误可能抛出异常。`loadFirstPage:false` 只是不立即加载；若自动加载开启，下一次 Tick 仍可能请求初始页。

### 取消、投影与生命周期

- Refresh / 替换加载器 / Release 会取消旧 CancellationToken；即使加载器忽略取消，旧代结果也不会追加。
- 同步取消回调允许关闭页面，刷新和绑定会检查释放状态，不再继续写 UI。
- 分页组件禁止普通 AddData/SetDatas/Clear 等接口，避免破坏尾项和页号；使用分页刷新或 Release。
- Renderer 错误和网络错误分开记录：`LastError`、`LastPresentationError`。已提交页不会因视图出错而重复请求；修复后用 RefreshPagePresentation。
- 组件自动 Tick；纯 C# 使用 `new UGUIVirtualPagedAdapter(core, loader, pageSize)`，Initialize 后在自己的更新里调用 `Tick(autoLoad, preloadItems)`。
- Tick 返回本轮新启动的任务，调用方应观察其异常；已经请求中的任务不会每帧重新返回。短页可以逐帧补齐视口，不会同步无限递归加载。
- 纯适配器 Dispose 取消请求但不清空/销毁 Core；组件 Release 会统一清理适配器和 Core。
- 单次请求/重试/刷新入口在组件释放后返回 false，不会重新初始化；重新打开须再次 SetPageLoader。
- 加载器必须管理自己的网络/资源生命周期。取消和旧代隔离不能替调用方释放任意业务对象内的资源，返回前应检查 token 并处理失败/取消的资源句柄。

### 分页 Demo

退出运行，执行 `UxGame/Test/Create UGUI Paging Demo`，保存后运行：

- 总业务记录 95 条，每页 20 条，模拟加载延迟。
- 第 3 页（页号2）首次模拟网络失败，点击尾部错误状态项重试。
- 滚动到后面会自动预加载；最后一页15条后显示没有更多。
- 在 `UGUIVirtualPagingDemo` 右键菜单测试下一页、失败重试、刷新、滚动到末尾。
- 三种 Demo 互斥启用，切换时不会让树/普通/分页控制器争用同一个列表。

## 分组列表与吸顶组头

在 Inspector 选择 **分组列表**：

- 竖向单列，支持固定/可变高度，隐藏不生效的多列和横向配置。
- **显示空分组组头**：默认开启，关闭后空组不产生任何行，但 Id 仍可查询。
- **组头吸顶**：默认开启，Header 到达视口顶部后显示独立副本；下一组到来时顶出旧 Header。

```csharp
// Inspector 已选择分组列表；模板0为Header，模板1为业务项。
_list.ItemTypeProvider = (index, data) => ((UGUIVirtualGroupItem)data).IsHeader ? 0 : 1;
_list.ItemRenderer = RenderGroupItem;
// 可选：吸顶副本的独立Renderer，不设置时回退到 ItemRenderer。
_list.StickyHeaderRenderer = RenderStickyHeader;

_list.SetGroups(new[]
{
    UGUIVirtualGroup.Create("weapons", "武器", weaponItems),
    UGUIVirtualGroup.Create("potions", "药水", potionItems),
    new UGUIVirtualGroup("empty", "暂无内容的分类")
});
_list.ScrollToGroup("weapons");
_list.UpdateGroupHeader("weapons", "已更新武器分类");
_list.UpdateGroupItem("weapons", 0, updatedWeapon);
```

`UGUIVirtualGroup` 是输入快照：Id/头数据/条目集合在构造后固定，Items 会复制。适配器内的 Update 修改当前投影，不反向修改输入集合；完整结构变化用新数据 SetGroups。

Renderer 接收 `UGUIVirtualGroupItem`：

- `GroupId` / `GroupIndex`：所属分组和输入顺序。
- `ItemIndex`：组内条目索引，Header 为 -1。
- `IsHeader`：是否组头。
- `IsSticky`：是否吸顶副本，真实 Header 为 false。
- `Data`：对应头数据或业务数据。

### 吸顶视图规则

- 不把真实 Header 移出 Content，独立克隆并按模板类型复用副本。
- 副本位于 Viewport 下的自动吸顶层，接受同一个 Mask / RectMask2D 裁剪，位置不跟随 Content 滚动。
- 下一组头距离顶部小于当前 Header 长度时，旧 Header 向上顶出；首个 Header 还在起点留白区域时不提前显示副本。
- 吸顶副本只用于显示，CanvasGroup 不阻挡 Raycast，不拦截 ScrollRect 拖拽，也不会发出业务 ItemClicked。
- 吸顶 Renderer 默认复用普通 ItemRenderer；若希望不同外观，可通过 IsSticky 判断，或单独设置 StickyHeaderRenderer。
- 回收副本触发 StickyHeaderRecycled，而不是普通 ItemRecycled。页面退出应分别解绑自己的回调。
- 可变高度副本单独测量，不把副本尺寸反向写入真实 Header 缓存。真实行仍遵循原来的估算/可见测量规则；远距离未测量区间依旧是估算值。
- Header 数据未变时，仅更新位置，不因每帧滚动重复 Bind。Header 更新、切组或 SetGroups 会使旧副本令牌失效。

### 吸顶副本的异步保护

副本不属于普通 active Item 索引，不能用普通 `TryGetItemToken(headerIndex)` 来取得它。根据行的 IsSticky 选择正确令牌：

```csharp
if (_list.TryGetStickyHeaderToken(out var token))
{
    var result = await LoadIconAsync();
    if (!token.IsCurrent) return;
    ApplyIconAndResize(token.GameObject, result);
    _list.NotifyStickyHeaderSizeChanged(token); // 只测量，不重新Bind
}
```

普通真实行继续使用普通绑定令牌。更新组头数据用 UpdateGroupHeader，会同步更新真实行并让副本在下一 Tick 重新绑定；必要时可 RefreshStickyHeader 显式刷新或修复 Renderer 错误后重试。

### 分组数据与生命周期

- 组件拥有分组适配器和吸顶控制器，Release 统一释放；晚到的头更新/尺寸通知不会重新打开组件。
- 不混用普通 AddData / SetDatas 改分组投影，数据版本会检测越权修改。Clear 在分组模式会清空分组。
- 纯 C# 调用方可构造 UGUIVirtualGroupAdapter(core) 和 UGUIVirtualStickyHeader(core, groups, factory)，每帧 Tick；它们不拥有 Core，退出时分别 Dispose。
- 同步 Renderer / 回收过程中不能重新改数据源。释放可延后到渲染安全位置，吸顶回收完成前 facade 也禁止重新初始化。
- 本轮不含多列满行 Header、可折叠分组、多级吸顶、运行时组内增删或组排序；结构替换用 SetGroups，需树状展开请用树模式。

### 分组 Demo

退出运行后执行 `UxGame/Test/Create UGUI Grouped Demo`：

- 20 个分组，每组20条业务数据，总400条业务数据+20个组头；只创建视口附近的行。
- 向下滚动观察金色文字吸顶副本和下一组顶出效果。
- 在 UGUIVirtualGroupDemo 组件菜单测试定位第二组、更新当前吸顶组头、重新设置分组。
- 新菜单仅显式切换 Demo，不会因更新代码自动覆盖现有场景。

## 滚动定位与平滑导航

默认 `ScrollToView(index, alignment)` 仍保留为即时定位。组件新增可选「平滑滚动导航」（仅竖向）：

```csharp
_list.ScrollTo(500, UGUIVirtualListAlignment.Center);
_list.ScrollTo(12, UGUIVirtualListAlignment.Start, padding: 16f, durationSeconds: 0.35f);
_list.StopScroll();
```

- `durationSeconds<0` 使用 Inspector 中的时长；`0` 立即定位，并在后续帧用测量结果校正。
- 平滑滚动使用无缩放时间，目标位置每帧按最新尺寸缓存重新计算，不会因为中途测量而偏位。
- Start/Center/End 会在测量后重新对齐；Nearest 在目标已完全可见时不移动。
- 用户开始拖拽 `UGUIVirtualListScrollRect`、数据增删/替换、重复调用导航、或列表释放都会取消当前导航。
- 普通 `ScrollRect` 无法上报拖拽开始，这种情况下需在业务层自行调用 `StopScroll()`。
- 横向列表暂不支持平滑导航：组件会打印警告并退化为直接定位；纯 C# 构造 `UGUIVirtualScrollNavigator` 会显式抛出 `NotSupportedException`。
- 未知尺寸的长距离定位仍是估算后测量、再校正的路径；不是全量预计算，滚动条在远距离仍可能随后续测量变化。

纯 C# 使用：

```csharp
var navigator = new UGUIVirtualScrollNavigator(list);
navigator.ScrollTo(100, UGUIVirtualListAlignment.End, durationSeconds: 0.25f);
// 在宿主的每帧更新中：
navigator.Tick(); // 可传自定义 delta 秒数，默认 unscaledDeltaTime
navigator.Dispose(); // 只解绑事件，不释放列表
```

在聊天 Demo 组件菜单可执行「平滑滚动到第500项（居中）」和「停止滚动」观察效果。

## 选中状态（单选 / 多选）

Inspector 的「选择模式」可选 **无 / 单选 / 多选**。选择保存的是**数据索引**，不是 GameObject，所以滚动复用、对象池回收都不会丢失选中状态。

```csharp
_list.SelectionMode = UGUIVirtualSelectionMode.Multiple;

_list.ToggleSelection(index);      // 点击常用
_list.Select(index);
_list.Deselect(index);
_list.SelectRange(10, 20);         // 拖动框选/范围选
_list.SelectRange(0, 4, additive: true);
_list.SelectAll();
_list.ClearSelection();

bool selected = _list.IsSelected(index);
int count = _list.SelectedCount;
IReadOnlyList<int> indices = _list.GetSelectedIndices();

_list.SelectionChanged += (index, data, isSelected) => { /* 绘制/数据同步 */ };
_list.SelectionBatchCompleted += () => { /* 批量操作后统一处理 */ };
```

### 渲染选中态

在 `ItemRenderer` 中读取 `IsSelected(index)` 即可。选中状态变化时，**只有受影响的已创建 Item 会重新绑定**，不会让整个可见区重刷：

```csharp
_list.ItemRenderer = (index, data, item) =>
{
    text.text = data.ToString();
    background.color = _list.IsSelected(index) ? selectedColor : normalColor;
};
```

### 与数据变更的配合

- 在选中位置**之前**插入数据，选中索引自动后移；删除时自动前移。
- 被删除的选中项会被移除，并触发一次 `SelectionChanged(..., false)`。
- `SetDatas` 全量替换会清空选择，并对旧的选中索引发出取消通知。
- 分页、树形、分组内部也走同一套增量接口，因此选中会自动跟随索引变化。
- 从多选切到单选时保留索引最小的项；切到「无」清空全部。

### 边界

- 选择只记索引，不理解业务身份（例如被服务器重新排序的两条同 Id 数据无法自动跟随）。需要按业务 Id 保持选中时，请在业务层用 `SelectionChanged` 自行同步。
- 不提供内置的选中高亮/多选框控件，只提供状态与事件；视觉由模板和 Renderer 决定。
- 范围选择按数据索引连续区间，不处理树形跨层级或分组跨组语义。

在聊天 Demo 组件菜单可执行 `测试 / 切换选择模式（无/单选/多选）` 和 `测试 / 选中前10项`，点击行即可看到选中背景色变化，滚动复用后仍保持。

## 结构编辑、Id 选中、游标分页与轮播

### 树 / 分组运行时结构编辑

```csharp
// 树
tree.AddRoot(node);
tree.InsertChild("parentId", 0, node);   // parentId 为 null 表示根层
tree.AddChild("parentId", node);
tree.RemoveNode("nodeId");               // 连同子树
tree.MoveNode("nodeId", "newParentId", 0);

// 分组
groups.InsertGroup(0, group);
groups.RemoveGroup("groupId");
groups.MoveGroup("groupId", 1);
groups.AddItem("groupId", data);
groups.InsertItem("groupId", 0, data);
groups.RemoveItem("groupId", 0);
```

- 结构变化后重建投影并做前后缀 diff，**只增删真正变化行**；未移动的行保留实例和绑定。
- 移动节点会更新 Depth/Parent，被移动子树的行会重建并重绑。
- 不能把节点移动到自己的子孙下；重复 Id、未知父节点、越界索引会被拒绝且不改变现状。
- 组件 facade 对应 `AddTreeRoot / AddTreeNode / RemoveTreeNode / MoveTreeNode` 与
  `AddGroup / InsertGroup / RemoveGroup / MoveGroup / AddGroupItem / RemoveGroupItem`。

### 按业务 Id 保持选中

```csharp
_list.SelectionKeySelector = data => ((Item)data).Id;
_list.SelectByKey("item-42");
_list.IsSelectedByKey("item-42");
_list.GetSelectedKeys();
```

- 设置后，插入/删除/服务器重排/全量替换都会**按 Id 重新定位**选中项。
- 数据里已不存在的 Id 会从选中集合移除。
- 未设置时仍按索引保存，`SetDatas` 清空选择。

### 游标分页

```csharp
await _list.SetCursorPageLoader(async (cursor, size, token) =>
{
    var page = await RequestByCursor(cursor, size, token);
    return UGUIVirtualCursorPage.Create(page.Items, page.NextCursor, page.HasMore);
});
_list.PageCursor;      // 当前游标
_list.IsCursorPaging;  // true
```

- 首次 cursor 为空串；之后由适配器传入上一次的 `NextCursor`。
- `HasMore=true` 时必须返回**非空且与上次不同**的游标，否则判为错误，避免无限加载。
- 与页号分页共用同一套状态行、重试、刷新和取消逻辑；`RefreshPagesAsync` 会重置游标。

### 轮播吸附

```csharp
_list.EnableCarousel(pageLength: viewportHeight, snapDuration: 0.2f);
_list.SnapToPage(2);
_list.SnapToNextPage();
_list.DisableCarousel();
```

- 拖拽结束后按 `pageLength` 四舍五入吸附到最近整页；偏移小于 0.5 不启动动画。
- 仅支持竖向列表；树/分组模式会拒绝启用。
- 依赖 `UGUIVirtualListScrollRect` 的拖拽结束事件；普通 `ScrollRect` 无法自动吸附。
- 自动 Tick 由组件完成；纯 C# 用法为 `new UGUIVirtualCarousel(core, pageLength)` 后每帧 `Tick()`。

## 无限循环列表

Inspector 勾选 **无限循环**（仅普通列表、单列/单行）即可把内容按周期重复滚动：

```text
真实数据：A B C D E
虚拟行：  A B C D E | A B C D E | A B C D E
                      ↑ 默认从中间周期开始
```

运行时：

```csharp
_list.IsLoop;                  // 是否启用
_list.ToDataIndex(virtualIndex); // 虚拟索引 → 真实数据索引
```

- 真实 `Count`、`GetData` 语义保持为业务数据；渲染/点击回调拿到的是**真实索引**（0..N-1）。
- 尺寸缓存按真实索引共享，各周期外观一致；支持固定与可变主轴尺寸。
- 滚动进入首/尾周期时静默平移一个周期长度，视觉无缝。
- 需要至少 3 个周期；内容长度（一个周期）必须大于视口，否则不做重定位。
- 只支持单列/单行：多列、树形、分组会拒绝；分页不建议与循环组合。
- 选择、滚动定位、尺寸通知都对虚拟行生效；`ScrollToView` 传虚拟索引，业务层通常用 `ToDataIndex` 换算。

纯 C# 用法：

```csharp
var inner = new UGUIFlowLayout(UGUIVirtualListAxis.Vertical, new Vector2(620, 120));
var layout = new UGUIVirtualLoopLayout(inner, repeat: 3);
var list = new UGUIVirtualList(scrollRect, layout, factory);
list.SetDatas(items);   // 自动从中间周期开始
```

### 循环 Demo

退出运行后执行 `UxGame/Test/Create UGUI Loop Demo`，保存并运行：

- 8 张彩色卡片，固定 620×120，3 个周期首尾相接。
- 从中间周期开始，向上/向下连续滚动都不会到头；进入首尾周期时会静默换周期。
- 默认开启按页吸附（每页 = 卡片高度 + 间距），松手后吸附到最近一张；可用组件菜单 `测试 / 下一页`、`上一页`、`回到第一张` 验证。
- 点击卡片打印的是**真实索引 0..7**，可以直接确认虚拟行与业务索引的映射。
- 视口附近只保留少量卡片实例（验证中不超过 20 个），不是把 3 个周期全部实例化。

## 模板异步加载与资源句柄

Inspector 里的模板要求场景对象或已加载的 Prefab。正式项目通常从 YooAsset 包按地址取模板，可用下面的组合，不必修改组件或列表核心：

```csharp
// 1) 资源加载器：按地址返回 Prefab 资源本身，并在释放时归还句柄。
var loader = new YooAssetItemTemplateLoader(YooType.Main);

// 2) 缓存：去重并发请求，把加载成功的模板写入组件。
var cache = new UGUIVirtualListTemplateCache(loader.LoadAsync, loader.Release);

// 3) 必须先加载模板，再提供数据。
await cache.LoadAsync(_list, new Dictionary<int, string>
{
    { 0, "Assets/GameRes/Res/UI/Items/MailItem.prefab" },
    { 1, "Assets/GameRes/Res/UI/Items/RewardItem.prefab" }
});

_list.ItemTypeProvider = (index, data) => ((Mail)data).HasReward ? 1 : 0;
_list.ItemRenderer = BindMail;
_list.SetDatas(mails);
```

`LoadAsync` 只在加载成功后调用组件的 `SetRuntimeTemplate`；运行时模板会覆盖同 TypeId 的序列化模板。**它必须在组件初始化前调用**，已初始化时请先 `Release()`。

### 行为约定

- 相同地址的并发请求只加载一次；同一地址可被多个 TypeId 共用。
- 部分地址失败时返回 `false`，但已成功的模板仍会生效并记录 `LastError`；再次调用只会重试失败地址。
- 取消或释放后不会再写入模板。
- `Dispose` 对每个已加载地址调用一次释放回调，重复 Dispose 不会重复释放。
- 空地址直接判失败，不会调用加载器。
- 模板资源本身由缓存持有；列表只 `Instantiate` 克隆，不销毁或激活模板。

### 替换资源系统

`UGUIVirtualListTemplateCache` 只依赖两个委托：

```csharp
new UGUIVirtualListTemplateCache(
    load:  (location, token) => MyAssets.LoadPrefabAsync(location, token),
    release: location => MyAssets.Release(location));
```

因此 Addressables、自定义 Bundle 或测试替身都能直接接入。

### 注意事项

- `YooAssetItemTemplateLoader` 位于 `Hotfix/Modules/UI`，依赖项目的 `YooMgr`，与纯 C# 列表核心分离。
- 它只加载 Prefab 资源并持有 `AssetHandle`，**不**调用 `Instantiate`；实例化与对象池仍由列表工厂负责，避免和 `UnityPool` 的实例池语义冲突。
- 模板加载失败、包未初始化会抛出异常；请在页面加载流程中处理并给出重试入口。
- 这里没有实现「滚动时按需加载模板」的懒加载。模板数量通常很少，建议进页面前一次性加载完；列表只做实例级虚拟化。

## 自动读取模板尺寸

- `FromTemplate` 在每次初始化时读取 `Default Item Type` 对应模板的 `RectTransform.rect.size`。
- 场景对象和 Project Prefab 均支持，不会激活模板，也不会创建临时克隆。
- 此时无需填写 `Item Size`，Inspector 会显示模板尺寸预览；切换到 `Manual` 可覆盖。
- 只读取默认模板作为全局固定尺寸或初始估算尺寸，不是把每种 Prefab 都当成不同尺寸的网格单元。
- 建议模板根节点使用固定宽高。依赖父节点拉伸、根节点尺寸为零的模板，请先设置根尺寸或选择 `Manual`。
- 不会为了读尺寸重建全部模板的布局；内容驱动高度仍在绑定后按可变布局测量。
- 老场景缺省仍为 `Manual`，不会悄悄改变已有尺寸；新添加的组件默认选择 `FromTemplate`。

## 统一列表与多列布局

使用者不再单独选择 Grid。组件统一使用 `UGUIFlowLayout`：一行/列就是线性列表，多行/列就是自动换行布局。

- 竖向：按照 `(视口宽度 + 水平间距) / (Item 宽度 + 水平间距)` 向下取整计算列数，至少 1 列。
- 横向：同理使用视口高度、Item 高度和垂直间距计算行数。
- 数量限制是**上限**，不是强制数量。例如上限 4 列，但空间只能放下 2 列时，实际为 2 列。
- 不限制数量时，宽度够就继续增加列；不会把 Item 自动拉伸或缩小。
- 单个 Item 比视口还大时仍保留一行/列，可能裁剪，Inspector 会提示。
- 固定尺寸不分配逐项尺寸缓存，位置/容量查询走公式快速路径。
- 可变竖向多列：每行按最高 Item 的高度推进；横向多行：每列按最宽 Item 的宽度推进。较短项会留白，不是瀑布流。
- 不支持某一项独占多列、跨行、任意交叉轴尺寸的紧密装箱。
- 支持末行不足、不同类型模板复用、滚动定位和尺寸变化通知。

例如可用宽度 640、Item 宽度 148、水平间距 8，则可排 4 列；可用宽度缩为 400 时自动变为 2 列。若勾选上限 3，宽度 640 时也只排 3 列。

视口改变时组件会自动更新容量并保持首个可见项的偏移，不重新绑定仍可见的 Item，其异步令牌也不会仅因换行而失效。

纯 C# 使用方式：

```csharp
var layout = new UGUIFlowLayout(
    UGUIVirtualListAxis.Vertical,
    itemSize: new Vector2(148, 110),
    variableSize: false,     // true 为可变主轴尺寸
    allowMultiple: true,
    maxCountPerLine: 4,      // 0 表示不限制
    spacing: new Vector2(8, 8),
    paddingStart: 12,
    paddingEnd: 12);
var list = new UGUIVirtualList(scrollRect, layout, factory);
list.SetDatas(items);
list.ScrollToView(500, UGUIVirtualListAlignment.Start);
// 不使用 MonoBehaviour 宿主时，可在自己的更新中调用：
list.RefreshViewportIfChanged(); // 同时消费上帧未收敛的布局工作
```

旧版 `FixedGrid=2` 配置自动迁移为「固定尺寸 + 启用多列/多行 + 数量上限」，旧列数成为上限。窄视口会自动减少容量，不再强行塞入并裁剪。原 `UGUIFixedLinearLayout`、`UGUIVariableLinearLayout`、`UGUIFixedGridLayout` 仍保留给已有纯 C# 调用方兼容使用。

### Inspector 按模式隐藏选项

- 自动模板尺寸：隐藏手填尺寸，仅显示模板尺寸预览。
- 单行/列：隐藏多列间距、数量限制和上限，只显示主轴间距。
- 多行/列：隐藏单列间距，显示 X/Y 间距及可选数量限制。
- 关闭数量限制：隐藏具体上限输入框。
- 固定尺寸：隐藏动态尺寸变化的锚点和保持末尾选项。
- 已使用专用 ScrollRect 时：隐藏升级提示。普通 ScrollRect 在各布局中都可能因增量数据更新需要拖拽补偿，因此会保留升级提示。

### 测试 Demo

退出 Play Mode 后执行：

- `UxGame/Test/Create UGUI Multi Column Demo`：把当前示例切换成自动换行、最多 4 列、148×110 的布局，填充 1000 个测试道具。
- `UxGame/Test/Create UGUI VirtualList Demo`：恢复已验收的垂直可变高度聊天示例。

两个菜单都复用现有 Canvas、ScrollRect 和模板，标记场景为脏状态，由用户保存；不会自动覆盖磁盘场景。仅更新脚本不会切换现有 Demo 的布局。

## 可变高度

布局不要求数据预先提供尺寸。业务只需在 Bind / ItemRenderer 中更新内容：

- Item 使用局部 LayoutGroup / ContentSizeFitter 驱动自身高度；或
- 绑定时自行设置根 RectTransform 的高度；或
- 自定义 Item 实现 `Measure()` 返回测量结果。

列表会先设置交叉轴尺寸 → 绑定内容 → 局部布局测量 → 缓存主轴尺寸 → 重排。

可变竖向布局中，每个 Item 宽度统一、高度可变；横向中高度统一、宽度可变。多列/多行以行/列最大尺寸推进，不是任意二维瀑布流。

### 尺寸变化通知（不重新绑定数据）

当图片加载完成、文本展开，且 Item 对象内容/尺寸已更新时，使用：

```csharp
_list.NotifyItemSizeChanged(index);
// 一次修改多项后统一通知，避免每项都做一次重排：
_list.NotifyVisibleItemSizesChanged();
```

与 `RefreshItem` 的区别：

- `RefreshItem(index)` 会重新 Bind，再测量，适合数据需要重新渲染的情况。
- `NotifyItemSizeChanged(index)` 仅测量，不调用 Bind，不重启图片加载、不清空刚完成的结果。
- 通知只处理当前已创建的 Item（含 Buffer），不可见、固定布局、已 Release 的列表会忽略通知。
- 不可见项应更新数据，等下次进入视口时绑定并测量，不会为了测量而创建全量 Item。
- 请在 Unity 主线程调用。无效、NaN、无穷大或非正的测量尺寸会报错，不会写入尺寸缓存。

### 异步回调与绑定令牌

不要只记住 index 或 GameObject。对象可能被复用，同一 index 也可能通过 Refresh / SetDatas 重新绑定。开始加载时先保存令牌，完成后先验证：

```csharp
if (!_list.TryGetItemToken(index, out var token))
    return;

var result = await LoadImageAsync();
if (!token.IsCurrent)
{
    // 回收当前任务的结果/句柄，不再修改 UI。
    return;
}

ApplyImageAndResize(token.GameObject, result);
_list.NotifyItemSizeChanged(token);
```

令牌在回收、重新绑定、替换数据及销毁后失效。**检查必须在修改 UI 之前**；仅在通知时检查，不能撤销先前对错误对象的写入。令牌不会自动取消任务或释放资源，资源生命周期仍由调用方负责。

### 滚动锚点

「可变尺寸」模式的 Inspector 提供：

- **保持滚动锚点**：默认开启。记录真正与视口相交的首项及其偏移，前面 Item 尺寸变化后补偿滚动位置。
- **原本在末尾时保持末尾**：默认关闭。开启后，仅当尺寸变化前已经停在末尾（容差 1 个 UI 单位）时贴住新末尾；浏览历史内容时不会强行跳到底部。

支持横向和纵向，处理批量尺寸变化、间距、收缩后的边界夹紧及弹性越界距离。若锚点自身变高，保持其起点偏移，而不是固定它内部某一行文字的位置。可见区或内容边界不足时，合法滚动范围优先。

### 拖拽期间的补偿

普通 `ScrollRect` 可用于静止/惯性时的锚点保持。由于 Unity 未公开拖拽起点的修改接口，若 Item 会在按住拖动期间变高，应使用可选的 `UGUIVirtualListScrollRect`：

1. 退出 Play Mode。
2. 在列表组件 Inspector 点击 **升级 ScrollRect（保留配置和场景引用）**，或使用原 ScrollRect 的右键升级菜单。
3. 保存场景后运行。

升级会保留 Content / Viewport、滚动参数、滚动条和 onValueChanged 持久化事件，重新连接已加载场景中的序列化引用，支持 Undo/Redo。不会自动替换其他自定义 ScrollRect 子类，也不会自动操作未加载的场景。

该适配器只维护拖拽参考系和速度采样；核心仍是普通 C# 类，不修改 Unity 包、不反射内部私有字段。新的 Demo 菜单会创建/升级为该适配器，单纯更新代码不会自动更改已有场景。

### 尺寸变化 Demo

运行聊天 Demo 并滚动到中间，在 Canvas 的 `UGUIVirtualListDemo` 右键菜单执行：

`测试 / 延迟展开视口前的缓冲 Item`

一秒后，前一项会新增多行内容，当前视口应保持原位。等待期间快速滚走，Console 会提示旧回调被忽略。可按住拖动时触发同样的测试，检查升级后的 ScrollRect 不回跳。

## 生命周期与刷新

- `SetDatas` 会自动初始化，不依赖其他组件的 Start 顺序。
- `Start` 也会初始化并隐藏场景模板，但不会自动生成测试数据。
- `OnDestroy` 自动回收并销毁生成的 Item，恢复仍存活的场景模板。
- 禁用适配组件不释放数据和对象；页面重新显示时会刷新。需要立即释放时主动 `Release()`。
- Inspector 布局参数在初始化时生效，不支持运行中直接改参数自动切换。需要重建时先 `Release()`，再配置并 `SetDatas`。
- ScrollRect / Viewport 引用和模板在初始化前配置。不要在已初始化后替换它们。
- 组件自动检测视口变化并重排，不重新绑定现有项；纯 C# 调用方可显式调用 `RefreshLayout()` 或在更新中调用 `RefreshViewportIfChanged()`。
- 极端估算差异可能需要超过 20 轮测量；达到单轮上限时 `HasPendingLayoutWork` 为 true，宿主下一帧继续，连续未收敛仅提示一次。纯 C# 调用方也应持续调用 `RefreshViewportIfChanged()` 消费这些待处理工作。
- `Dispose()` 在业务回调内部可延迟清理，`IsDisposalComplete` 表示清理结束；组件会管理这一过程。

```csharp
_list.Refresh();
_list.RefreshItem(3);
_list.ScrollToView(20, UGUIVirtualListAlignment.Start);
_list.ScrollToView(20, UGUIVirtualListAlignment.Center);
_list.Release(); // 可重新 SetDatas 初始化
```

## 当前边界

目前支持统一的固定/可变单轴流式布局、视口自适应多行/列、树形数据适配、页号分页、竖向分组与单级吸顶、滚动定位校正与平滑导航、单选/多选状态；不支持瀑布流、跨行跨列、循环、多级吸顶、游标分页和树的异步子节点加载等高级功能。
可变尺寸仍采用估算加测量，未知区间的总长度和远距离定位是估算值；本轮支持尺寸变化后的视觉锚点保持，但未实现未知全量尺寸的预计算、全局精确滚动条，以及测量后对 Center/End 定位目标的持续追踪。
