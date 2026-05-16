using FairyGUI;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using static Ux.UIMgr;

namespace Ux
{
    /// <summary>
    /// UI基础接口，所有UI类都需要实现此接口
    /// </summary>
    public interface IUI
    {
        /// <summary>
        /// 当前UI状态
        /// </summary>
        UIState State { get; }
        
        /// <summary>
        /// UI类型（普通、栈式、固定）
        /// </summary>
        UIType Type { get; }
        
        /// <summary>
        /// 模糊效果类型
        /// </summary>
        UIBlur Blur { get; }
        
        /// <summary>
        /// UI名称
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// UI唯一ID
        /// </summary>
        int ID { get; }
        
        /// <summary>
        /// UI数据
        /// </summary>
        IUIData Data { get; }
        
        /// <summary>
        /// 隐藏后销毁时间（毫秒），-1表示不自动销毁
        /// </summary>
        int HideDestroyTime { get; }
        
        /// <summary>
        /// UI是否可见
        /// </summary>
        bool Visible { get; set; }
        
        /// <summary>
        /// 滤镜效果
        /// </summary>
        IFilter Filter { get; set; }
        
        /// <summary>
        /// 初始化UI数据
        /// </summary>
        /// <param name="data">UI数据</param>
        /// <param name="initData">初始化回调数据</param>
        void InitData(IUIData data, CallbackData initData);
        
        /// <summary>
        /// 释放UI资源
        /// </summary>
        void Dispose();
        
        /// <summary>
        /// 执行显示操作
        /// </summary>
        /// <param name="isAnim">是否播放动画</param>
        /// <param name="id">请求显示的UI ID</param>
        /// <param name="param">显示参数</param>
        /// <param name="isStack">是否检查栈</param>
        /// <returns>显示完成的异步任务</returns>
        UniTask DoShow(bool isAnim, int id, IUIParam param, bool isStack);
        
        /// <summary>
        /// 执行隐藏操作
        /// </summary>
        /// <param name="isAnim">是否播放动画</param>
        /// <param name="checkStack">是否检查栈</param>
        void DoHide(bool isAnim, bool checkStack);
    }
    
    /// <summary>
    /// UI类型枚举，定义UI的栈式管理行为
    /// </summary>
    public enum UIType
    {
        /// <summary>
        /// 普通UI，不会进入UI栈管理
        /// </summary>
        Normal,
        
        /// <summary>
        /// 栈式UI，会被加入UI栈管理，隐藏时可能返回上一个UI
        /// </summary>
        Stack,
        
        /// <summary>
        /// 固定UI，始终显示在最顶层，不受栈管理影响
        /// </summary>
        Fixed,
    }

    /// <summary>
    /// UI模糊效果类型枚举
    /// </summary>
    [Flags]
    public enum UIBlur
    {
        /// <summary>
        /// 无模糊效果
        /// </summary>
        None = 0x0,
        
        /// <summary>
        /// 普通模糊
        /// </summary>
        Normal = 0x1,
        
        /// <summary>
        /// 强模糊
        /// </summary>
        Blur = 0x2,
        
        /// <summary>
        /// 固定模糊（用于固定UI）
        /// </summary>
        Fixed = 0x4,
        
        /// <summary>
        /// 场景模糊
        /// </summary>
        Scene = 0x8,
    }

    public partial class UIMgr
    {
        /// <summary>
        /// UI生命周期阶段枚举，描述UI从创建到销毁的各个状态
        /// </summary>
        internal enum UIPhase
        {
            /// <summary>
            /// 空闲状态，UI未创建或已完全释放
            /// </summary>
            Idle,

            /// <summary>
            /// 创建中，正在异步创建UI实例
            /// </summary>
            Creating,

            /// <summary>
            /// 显示中，正在执行显示动画或逻辑
            /// </summary>
            Showing,

            /// <summary>
            /// 可见状态，UI已完全显示在屏幕上
            /// </summary>
            Visible,

            /// <summary>
            /// 隐藏中，正在执行隐藏动画或逻辑
            /// </summary>
            Hiding,

            /// <summary>
            /// 隐藏状态，UI已完全隐藏但未释放
            /// </summary>
            Hidden,

            /// <summary>
            /// 已释放，UI资源已完全清理
            /// </summary>
            Disposed,
        }

        /// <summary>
        /// UI记录类，跟踪每个UI实例的详细状态信息
        /// </summary>
        internal sealed class UIRecord
        {
            public int Id;                          // UI的唯一ID
            public IUI UI;                          // UI实例引用
            public UIPhase Phase = UIPhase.Idle;    // 当前生命周期阶段

            /// <summary>
            /// 递增的请求版本号。
            /// 每次发起新的显示或隐藏请求时都会自增，用来判定异步回调是否仍然对应当前这一次操作，
            /// 避免旧请求在晚于新请求完成时，把界面状态错误地回滚到过期结果。
            /// </summary>
            public int RequestVersion;

            /// <summary>
            /// 当前界面所在父子链路对应的根界面 Id。
            /// 当一个界面作为某个根界面的子级、孙级或更深层级页面存在时，
            /// 这里记录整条链路最上层的根节点，便于按根界面统一管理显示、隐藏与栈关系。
            /// </summary>
            public int ParentRootId;

            /// <summary>
            /// 当前根链路下正在激活的直接子界面 Id。
            /// 用于描述同一条 UI 根链路里当前焦点落在哪个子页面上，
            /// 方便父子界面切换时快速确定当前生效的节点。
            /// </summary>
            public int CurrentChildId;

            public IUIParam LastShowParam;          // 最近一次显示时传递的参数
            public int LastShowRequestFrame;        // 最近一次显示请求的帧号
            public int LastHideRequestFrame;        // 最近一次隐藏请求的帧号

            /// <summary>
            /// 最近一次开始执行显示流程时的帧号。
            /// 主要用于识别"刚发起显示，马上又请求隐藏"的场景，
            /// 将同帧或相邻帧内的 Show -> Hide 抖动请求折叠掉，减少状态抖动和重复逻辑。
            /// </summary>
            public int LastShowStartFrame;

            public int LastVisibleFrame;            // 最近一次变为可见状态的帧号
            public CacheState CacheState;           // 缓存状态

            /// <summary>
            /// 正在执行隐藏流程时共享的等待句柄。
            /// 后续新的显示请求如果发现同一个界面还没完全隐藏结束，
            /// 可以统一等待这个完成源，避免重复发起隐藏或与未完成的隐藏流程互相打架。
            /// </summary>
            public UniTaskCompletionSource<bool> PendingHide;

            /// <summary>
            /// 正在执行显示提交流程时共享的等待句柄。
            /// 当多个调用方几乎同时请求显示同一个界面时，
            /// 可以共同等待这一次可见状态提交完成，避免产生多份重复的异步等待对象。
            /// </summary>
            public UniTaskCompletionSource<bool> PendingShow;

            public WaitDel WaitDel;                 // 等待销毁的延迟处理对象

            /// <summary>
            /// 检查UI是否处于可见提交状态（已完全显示）
            /// </summary>
            public bool IsVisibleCommitted => Phase == UIPhase.Visible;

            /// <summary>
            /// 检查UI是否处于显示相关状态（创建中或显示中）
            /// </summary>
            public bool IsShowingLike => Phase == UIPhase.Creating || Phase == UIPhase.Showing;

            /// <summary>
            /// 检查UI是否处于隐藏相关状态（隐藏中）
            /// </summary>
            public bool IsHidingLike => Phase == UIPhase.Hiding;
        }

        /// <summary>
        /// 缓存状态枚举，描述UI在生命周期缓存中的状态
        /// </summary>
        internal enum CacheState
        {
            /// <summary>
            /// 无缓存状态
            /// </summary>
            None,

            /// <summary>
            /// 已缓存，UI实例被缓存以便复用
            /// </summary>
            Cached,

            /// <summary>
            /// 等待销毁，UI实例标记为等待销毁状态
            /// </summary>
            WaitDestroy,
        }

        /// <summary>
        /// 栈条目结构体，记录UI栈中的一项
        /// 只读结构体确保栈数据在传递过程中不会被修改
        /// </summary>
        internal readonly struct StackEntry
        {
            /// <summary>
            /// 构造函数
            /// </summary>
            public StackEntry(int rootId, int activeId, IUIParam param, UIType type)
            {
                RootId = rootId;
                ActiveId = activeId;
                Param = param;
                Type = type;
            }

            public int RootId { get; }    // 根界面ID
            public int ActiveId { get; }  // 激活界面ID
            public IUIParam Param { get; } // 界面参数
            public UIType Type { get; }   // 界面类型（Normal/Stack/Fixed）
        }
        
        /// <summary>
        /// UI显示构建器类，提供链式API来配置和显示UI
        /// </summary>
        public class UIShowBuilder
        {
            int _id;              // UI ID
            IUIParam _param;      // 显示参数
            bool _isAnim = true;  // 是否播放动画

            /// <summary>
            /// 设置UI ID
            /// </summary>
            public UIShowBuilder SetId(int id)
            {
                _id = id;
                return this;
            }

            /// <summary>
            /// 设置显示参数
            /// </summary>
            public UIShowBuilder SetParam(IUIParam param)
            {
                _param = param;
                return this;
            }

            /// <summary>
            /// 设置是否播放动画
            /// </summary>
            public UIShowBuilder SetAnim(bool isAnim)
            {
                _isAnim = isAnim;
                return this;
            }

            /// <summary>
            /// 显示UI并返回泛型任务
            /// </summary>
            /// <typeparam name="T">UI类型</typeparam>
            /// <returns>UI任务对象</returns>
            public UITask<T> Show<T>() where T : IUI
            {
                if (_id == 0)
                {
                    _id = Ins.GetTypeId(typeof(T));
                }
                var task = Ins._ShowAsync<T>(_id, _param, _isAnim, true);
                _Release();
                return new UITask<T>(task);
            }

            /// <summary>
            /// 显示UI并返回接口任务
            /// </summary>
            /// <returns>UI任务对象</returns>
            public UITask<IUI> Show()
            {
                var task = Ins._ShowAsync<IUI>(_id, _param, _isAnim, true);
                _Release();
                return new UITask<IUI>(task);
            }

            /// <summary>
            /// 释放构建器，将对象回收到对象池
            /// </summary>
            private void _Release()
            {
                _id = 0;
                _param = null;
                _isAnim = true;
                Pool.Push(this);
            }
        }

        /// <summary>
        /// 回调数据结构体，用于存储UI显示和隐藏的回调函数
        /// </summary>
        public readonly struct CallbackData
        {
            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="showCb">显示回调函数</param>
            /// <param name="hideCb">隐藏回调函数</param>
            public CallbackData(Action<IUI, IUIParam, bool> showCb, Action<IUI> hideCb)
            {
                this.showCb = showCb;
                this.hideCb = hideCb;
            }
            public readonly Action<IUI, IUIParam, bool> showCb;  // 显示回调
            public readonly Action<IUI> hideCb;                  // 隐藏回调
        }

        /// <summary>
        /// 模糊效果栈结构体
        /// </summary>
        public struct BlurStack
        {
            public readonly UIBlur Blur;   // 模糊类型
            public readonly int ID;        // UI ID
            
#if UNITY_EDITOR
            /// <summary>
            /// UI ID字符串表示（仅编辑器模式使用）
            /// </summary>
            public readonly string IDStr;
            
            /// <summary>
            /// 构造函数（编辑器模式）
            /// </summary>
            public BlurStack(string idStr, int id, UIBlur blur)
            {
                IDStr = idStr;
                ID = id;
                Blur = blur;
            }
#else
            /// <summary>
            /// 构造函数（发布模式）
            /// </summary>
            public BlurStack(int id, UIBlur blur)
            {
                ID = id;
                Blur = blur;
            }
#endif
        }
        
        /// <summary>
        /// UI栈结构体，用于调试器显示
        /// </summary>
        public struct UIStack
        {
            public readonly int ParentID;  // 父UI ID
            public int ID;                  // 当前UI ID
            public IUIParam Param;         // UI参数
            public readonly UIType Type;    // UI类型

            /// <summary>
            /// 构造函数
            /// </summary>
            public UIStack(int parentID, int id, IUIParam param, UIType type)
            {
                ParentID = parentID;
                ID = id;
                Param = param;                
                Type = type;
            }            
        }
        
        /// <summary>
        /// UI解析数据结构体，用于注册UI信息
        /// </summary>
        public readonly struct UIParse
        {
            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="type">UI类型</param>
            /// <param name="id">UI ID，0表示自动生成</param>
            /// <param name="tabData">Tab数据</param>
            public UIParse(Type type, int id, IUITabData tabData)
            {
                id = id == 0 ? type.FullName.ToHash() : id;
                Data = new UIData(id, type, tabData);
            }

            UIData Data { get; }

            /// <summary>
            /// 将UIData添加到字典
            /// </summary>
            public void Add(Dictionary<int, IUIData> _id2data)
            {
                if (_id2data.ContainsKey(Data.ID))
                {
                    Log.Error("UIData注册重复了。ID[{0}]", Data.ID);
                    return;
                }

                _id2data.Add(Data.ID, Data);
            }

            /// <summary>
            /// 解析父子关系
            /// </summary>
            public void Parse(Dictionary<int, IUIData> _id2data)
            {
                if (Data.TabData == null) return;

                if (Data.TabData.PID == 0)
                {
                    Log.Error("UITabData父ID为空。ID[{0}]", Data.ID);
                    return;
                }

                var pId = Data.TabData.PID;
                if (!_id2data.TryGetValue(pId, out var pIData))
                {
                    Log.Error("UIData注册的父面板不存在。ID[{0}]", Data.ID);
                    return;
                }

                var pData = pIData as UIData;
                pData?.Children.Add(Data.ID);
            }
        }

        /// <summary>
        /// Item URL解析结构体
        /// </summary>
        public readonly struct ItemUrlParse
        {
            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="type">类型</param>
            /// <param name="url">URL</param>
            public ItemUrlParse(Type type, string url)
            {
                Type = type;
                Url = url;
            }
            Type Type { get; }   // 类型
            string Url { get; }  // URL

            /// <summary>
            /// 将URL添加到字典
            /// </summary>
            public void Add(Dictionary<Type, string> _urls)
            {
                if (_urls.ContainsKey(Type))
                {
                    Log.Error("ItemRenderer 注册URL 重复了。Type[{0}]", Type.FullName);
                    return;
                }
                _urls.Add(Type, Url);
            }
        }

        /// <summary>
        /// UI任务结构体，用于异步UI操作
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        public readonly struct UITask<T> where T : IUI
        {
            readonly UniTask<T> _task;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="task">异步任务</param>
            public UITask(UniTask<T> task)
            {
                _task = task;
            }

            /// <summary>
            /// 获取异步任务
            /// </summary>
            public UniTask<T> Task()
            {
                return _task;
            }
        }

        /// <summary>
        /// 下载数据结构体，用于存储UI下载相关信息
        /// </summary>
        public readonly struct DownloadData
        {
            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="uiid">UI ID</param>
            /// <param name="param">UI参数</param>
            /// <param name="isAnim">是否播放动画</param>
            public DownloadData(int uiid, IUIParam param, bool isAnim)
            {
                this.UIID = uiid;
                this.Param = param;
                this.IsAnim = isAnim;
            }
            public int UIID { get; }         // UI ID
            public IUIParam Param { get; }   // UI参数
            public bool IsAnim { get; }      // 是否播放动画
        }

        /// <summary>
        /// 等待销毁处理类，用于延迟销毁UI
        /// </summary>
        public class WaitDel
        {
            IUI ui;                  // UI实例
            long timeKey;            // 定时器键值
            
            /// <summary>
            /// UI名称
            /// </summary>
            public string Name => ui.Name;
            
            /// <summary>
            /// UI ID
            /// </summary>
            public int ID => ui.ID;
            
            /// <summary>
            /// 从等待删除列表移除时的回调
            /// </summary>
            public Action<int> OnRemoveFromWaitDel;
            
            /// <summary>
            /// 从等待删除列表销毁时的回调
            /// </summary>
            public Action<IUI> OnDisposeFromWaitDel;
            
            /// <summary>
            /// 初始化等待销毁对象
            /// </summary>
            public void Init(IUI _ui, Action<int> onRemove,Action<IUI> onDispose)
            {
                ui = _ui;
                OnRemoveFromWaitDel = onRemove;
                OnDisposeFromWaitDel = onDispose;
                timeKey = TimeMgr.Ins.Timer(_ui.HideDestroyTime, this, Exe).Repeat(1).Build();
            }

            /// <summary>
            /// 释放资源，将对象回收到对象池
            /// </summary>
            void Release()
            {
                RemoveTime();
                ui = null;
                OnRemoveFromWaitDel = null;
                OnDisposeFromWaitDel = null;
                Pool.Push(this);
            }

            /// <summary>
            /// 销毁UI并清理资源
            /// </summary>
            public void Dispose()
            {
                OnRemoveFromWaitDel?.Invoke(ui.ID);
                Dialog.RemoveWaitDelById(ui.ID);
                Tip.RemoveWaitDelById(ui.ID);    
                OnDisposeFromWaitDel?.Invoke(ui);                
                Release();
            }

            /// <summary>
            /// 获取UI实例并从等待列表移除
            /// </summary>
            public void GetUI(out IUI outUI)
            {
                outUI = ui;
                OnRemoveFromWaitDel?.Invoke(ui.ID);
                Dialog.RemoveWaitDelById(ui.ID);
                Tip.RemoveWaitDelById(ui.ID);                
                Release();
            }

            /// <summary>
            /// 执行销毁逻辑（定时器回调）
            /// </summary>
            void Exe()
            {
                if (timeKey == 0 || ui == null) return;
                timeKey = 0;
                Dispose();
            }

            /// <summary>
            /// 移除定时器
            /// </summary>
            void RemoveTime()
            {
                if (timeKey == 0) return;
                TimeMgr.Ins.RemoveTimer(timeKey);
                timeKey = 0;
            }
        }

    }
}
