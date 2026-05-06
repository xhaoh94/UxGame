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
    public enum UIBlur
    {
        /// <summary>
        /// 无模糊效果
        /// </summary>
        None = 0x1,
        
        /// <summary>
        /// 普通模糊
        /// </summary>
        Normal = 0x2,
        
        /// <summary>
        /// 强模糊
        /// </summary>
        Blur = 0x4,
        
        /// <summary>
        /// 固定模糊（用于固定UI）
        /// </summary>
        Fixed = 0x8,
        
        /// <summary>
        /// 场景模糊
        /// </summary>
        Scene = 0x16,
    }

    public partial class UIMgr
    {
        /// <summary>
        /// UI显示构建器类，提供链式API来配置和显示UI
        /// </summary>
        public class UIShowBuilder
        {
            int _id;              // UI ID
            IUIParam _param;      // 显示参数
            bool _isAnim;         // 是否播放动画

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
                    _id = Ins.ConverterID(typeof(T));
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
