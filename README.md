# U3D+HybridCLR+YooAssest+FGUI 缝合怪

# 只是加了一些平时开发觉得有用的东西

## HybridCLR + YooAssest 集成

- c#热更新+资源热更新，通过可视化工具+YooAssets打包，可一键直接构建到指定目录，包括资源差异化分析，拷贝指定目录。配合DHFS（本地资源服务器），可以实现编辑器模式热更逻辑运行测试

## Event

- 独立的事件系统+单例默认事件系统
- 通过特性注册，减少N多重复代码（通过反射实现，虽然内部实现了对象池，但是还是建议只在初始化模块这种只会实例化一次的地方使用
- 事件队列+可同步调用接口，默认事件走队列触发，每帧事件有触发上限，解决一帧触发极多事件导致掉帧。部分需同步触发事件，也可调用接口直接触发。
- 一键取消关联对象所有事件监听，避免事件漏取消状况（注册事件时带标签，默认传注册实例化对象）在需要对象取消监听时，只需要传此对象去取消监听即可，无需所有事件都再重复走一遍取消流程 

- 可视化工具，默认事件系统会存储所有事件展示在可视化工具里，可随时查看事件是否有泄漏

## Net

- 内部集成了TCP、KCP、WebSocket
- 自定义RPC流程，同个方法内即可实现请求->响应。代码嘎嘎好看。（rpc这个需跟服务器约束规则，参考即可）

- 可定义数据序列号大小端，轻松解决跟服务器端不一致问题

## Res

- 二次封装的YooAssets加载接口，加载gameObject对象会自动挂载脚本，等对象销毁时，回收handle。
- 资源懒加载，通过可视化工具可定义资源标签，部分资源可以在游戏内再加载
- 可视化工具，暂实现了UI包的引用计数

## Time

 **集成多种定时器，每种定时器注册时都会进行排序插入，当队列不满足触发条件会直接中断后续定时器，减少循环次数**

- 计时定时器，可定义初始触发时间+后续间隔时间+触发次数+触发完毕回调
- 帧数定时器，可定义初始触发帧数+后续间隔帧数+触发次数+触发完毕回调
- 时间戳定时器，指定时间戳到达触发
- Cron表达式触发器
- 可视化工具，你所注册的所有触发器都可以在这里查看

## UI

**二次封装FairyGUI，使用特性注册或代码动态注册，逻辑与资源的拆分，界面可自定义对应资源包，实现异步加载、懒加载、界面模糊、界面栈、界面动画、自定义界面嵌套（A界面嵌套了B、C界面，打开B、C界面时会自动根据嵌套先打开A界面）。通过可视化工具可自动生成代码绑定、自动绑定点击事件、类型自定义**

- 常规界面 UIView 
- 弹窗界面 UIWindow,封装的FairyGUI.Window
- 嵌套界面 UITabView 
- 辅助界面 UITip（提示语）、UIMessageBox（弹窗确认）。
- UIModel、RTModel 傻瓜式在2D显示3D模型
- 循环列表UIList，同个列表可自定义多个不同Item，通过你自定义规则，给你生成不同的Item
- 可视化工具，你所有注册的UI界面的状态与缓存都在这可以查看

## Eval 公式解析器

使用抽象树+AST、对象池，高性能解析字符串公式，相同的公式，在预热后基本无消耗。实现了基础加减乘除取余。

- 自定义函数，除了内置常规数字函数，可随意自定义函数名，执行你想要的逻辑
- 自定义变量，字符串公式，变量不可缺少，一个计算战力的公式 hp+atk ,通过传入变量数值，轻松获取战力数值
  ```c#
  string eval = "10+max(1+1,test(a+b+c,c-b)*-((-b-2)*(a+c)))";
  EvalMgr.Ins.AddVariable("a", 2);
  EvalMgr.Ins.AddVariable("b", -1);
  EvalMgr.Ins.AddVariable("c", 3);
  EvalMgr.Ins.AddFunction("test", (nums) =>
  {
      return nums[0] + nums[1];
  });
  EvalMgr.Ins.Release();
  
  System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
  sw.Start();
  var value = EvalMgr.Ins.Parse(eval);
  sw.Stop();
  //10+max(1+1,test(a+b+c,c-b)*-((-b-2)*(a+c))):50,ms:5
  Log.Debug(eval + ":{0},ms:{1}", value, sw.ElapsedMilliseconds);
  ```
## 其他开发中功能

- Timeline 用于战斗编辑，实现了Animator，但是其他上层业务逻辑还没实现其他的，现在牛马生活，天天到家已经快12点了，没动力了
- Condition 条件系统，这个只能根据不同的游戏定制
- Tag 红点系统，其实已经是实现了的，只不过有部分逻辑也得根据游戏来定制



## 辅助工具

![UI](https://github.com/xhaoh94/UxGame/blob/main/IMG/ui.png)

![Res](https://github.com/xhaoh94/UxGame/blob/main/IMG/res.png)

![Event](https://github.com/xhaoh94/UxGame/blob/main/IMG/event.png)

![Time](https://github.com/xhaoh94/UxGame/blob/main/IMG/time.png)

![tool](https://github.com/xhaoh94/UxGame/blob/main/IMG/tool.png)

![uiGen](https://github.com/xhaoh94/UxGame/blob/main/IMG/uiGen.png)

![构建](https://github.com/xhaoh94/UxGame/blob/main/IMG/build.png)

