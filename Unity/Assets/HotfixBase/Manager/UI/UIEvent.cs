using FairyGUI;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace Ux
{
    /// <summary>
    /// UI事件管理器类，负责管理UI组件的事件监听
    /// 包括普通事件、长按事件和多次点击事件
    /// 使用对象池管理，避免频繁创建和销毁
    /// </summary>
    public class UIEvent
    {
        private List<EventListener> _listeners;        // 组件事件监听器列表
        private Dictionary<GObject, UILongPressEventData> _longPressEvtList;  // 长按事件数据字典
        private Dictionary<GObject, UIMultipleClickEventData> _mulClickEvtList; // 多次点击事件数据字典

        /// <summary>
        /// 释放UI事件管理器，将其回收到对象池
        /// </summary>
        public void Release()
        {
            Pool.Push(this);
        }

        /// <summary>
        /// 移除所有事件监听
        /// 清理所有事件监听器、长按事件和多次点击事件
        /// </summary>
        public void RemoveEvents()
        {
            if (_listeners != null)
            {
                foreach (var listener in _listeners)
                {
                    listener.Clear();
                }
                _listeners.Clear();
            }
            if (_longPressEvtList != null)
            {
                foreach (var kv in _longPressEvtList)
                {
                    kv.Value.Clear();
                }
                _longPressEvtList.Clear();
            }

            if (_mulClickEvtList != null)
            {
                foreach (var kv in _mulClickEvtList)
                {
                    kv.Value.Clear();
                }
                _mulClickEvtList.Clear();
            }
        }

        /// <summary>
        /// 添加多次点击事件监听
        /// </summary>
        /// <param name="gObject">目标GObject</param>
        /// <param name="fn">事件委托，可以是EventCallback0或EventCallback1类型</param>
        /// <param name="clickCnt">点击次数，默认为2（双击）</param>
        /// <param name="gapTime">点击间隔时间，默认为0.3秒</param>
        public void AddMultipleClick(GObject gObject, Delegate fn, int clickCnt = 2, float gapTime = 0.3f)
        {
            _mulClickEvtList ??= new Dictionary<GObject, UIMultipleClickEventData>();
            if (_mulClickEvtList.ContainsKey(gObject)) return;
            var item = Pool.Get<UIMultipleClickEventData>();
            if (fn is EventCallback0 fn0)
                item.Init(gObject, fn0, clickCnt, gapTime);
            else if (fn is EventCallback1 fn1)
                item.Init(gObject, fn1, clickCnt, gapTime);
            else
                return;
            _mulClickEvtList.Add(gObject, item);
        }

        /// <summary>
        /// 添加普通事件监听
        /// </summary>
        /// <param name="listener">事件监听器</param>
        /// <param name="fn">事件委托，可以是EventCallback0或EventCallback1类型</param>
        public void AddEvent(EventListener listener, Delegate fn)
        {
            _listeners ??= new List<EventListener>();
            if (_listeners.Contains(listener)) return;
            if (fn is EventCallback1 fn1)
                listener.Add(fn1);
            else if (fn is EventCallback0 fn0)
                listener.Add(fn0);
            _listeners.Add(listener);
        }
        
        /// <summary>
        /// 添加长按事件监听
        /// </summary>
        /// <param name="gObject">目标GObject</param>
        /// <param name="fn">长按触发函数，返回true时停止长按</param>
        /// <param name="first">第一次触发时间（秒），-1表示不使用首次触发延迟</param>
        /// <param name="delay">后续触发间隔时间（秒）</param>
        /// <param name="loop">循环触发次数，0表示无限循环</param>
        /// <param name="holdRangeRadius">手指按住后，移动超出此半径范围则手势停止</param>
        public void AddLongPress(GObject gObject, Func<bool> fn, float first, float delay, int loop, int holdRangeRadius)
        {
            _longPressEvtList ??= new Dictionary<GObject, UILongPressEventData>();
            if (_longPressEvtList.ContainsKey(gObject)) return;
            var item = Pool.Get<UILongPressEventData>();
            item.Init(gObject, fn, first, delay, loop, holdRangeRadius);
            _longPressEvtList.Add(gObject, item);
        }
    }

    /// <summary>
    /// 多次点击事件数据类，用于处理双击、三击等多击事件
    /// 注意：注册了多次点击事件后，即使是单击也会受到gapTime延时触发
    /// </summary>
    public class UIMultipleClickEventData
    {
        private GObject _target;          // 目标GObject
        private EventCallback1 _fn1;      // 带参数的事件回调函数
        private EventCallback0 _fn0;      // 无参数的事件回调函数
        private float _gapTime;           // 点击间隔时间
        private long _timeKey;            // 定时器键值
        private int _touchId;             // 触摸ID
        private float _beginTime;         // 触摸开始时间
        private float _firstTime;         // 第一次点击时间
        private int _clickCnt;            // 需要点击的总次数
        private int _nowkCnt;             // 当前已点击次数

        /// <summary>
        /// 初始化无参数回调的多次点击事件
        /// </summary>
        /// <param name="gObject">目标GObject</param>
        /// <param name="fn0">无参数事件回调</param>
        /// <param name="clickCnt">需要点击的总次数</param>
        /// <param name="gapTime">点击间隔时间</param>
        public void Init(GObject gObject, EventCallback0 fn0, int clickCnt, float gapTime)
        {
            _target = gObject;
            _fn0 = fn0;
            _clickCnt = clickCnt;
            _nowkCnt = 1;
            _gapTime = gapTime;
            _target.onTouchBegin.Add(OnTouchBegin);
            _target.onTouchEnd.Add(OnTouchEnd);
        }
        
        /// <summary>
        /// 初始化带参数回调的多次点击事件
        /// </summary>
        /// <param name="gObject">目标GObject</param>
        /// <param name="fn1">带参数事件回调</param>
        /// <param name="clickCnt">需要点击的总次数</param>
        /// <param name="gapTime">点击间隔时间</param>
        public void Init(GObject gObject, EventCallback1 fn1, int clickCnt, float gapTime)
        {
            _target = gObject;
            _fn1 = fn1;
            _clickCnt = clickCnt;
            _nowkCnt = 0;
            _gapTime = gapTime;
            _target.onTouchBegin.Add(OnTouchBegin);
            _target.onTouchEnd.Add(OnTouchEnd);
        }
        
        /// <summary>
        /// 清理事件监听，将对象回收到对象池
        /// </summary>
        public void Clear()
        {
            if (_target != null)
            {
                _target.onTouchBegin.Remove(OnTouchBegin);
                _target.onTouchEnd.Remove(OnTouchEnd);
            }

            _target = null;
            _fn0 = null;
            _fn1 = null;
            _ClearTime();
            Pool.Push(this);
        }
        
        /// <summary>
        /// 触摸开始事件处理
        /// </summary>
        private void OnTouchBegin(EventContext e)
        {
            _touchId = e.inputEvent.touchId;
            _ClearTime();
            // 超出多击时间，重置次数
            if (Time.unscaledTime - _firstTime > _gapTime)
            {
                _nowkCnt = 0;
            }
            _beginTime = Time.unscaledTime;
        }
        
        /// <summary>
        /// 模拟点击，用于处理单击超时的情况
        /// </summary>
        private void SimulationClick()
        {
            _timeKey = 0;
            _nowkCnt = 0;
            _target.onClick.Call();
        }
        
        /// <summary>
        /// 触摸结束事件处理
        /// </summary>
        private void OnTouchEnd(EventContext e)
        {
            if (e.inputEvent.touchId != _touchId) return;
            Stage.inst.CancelClick(e.inputEvent.touchId);
            _nowkCnt++;
            if (_nowkCnt == 1)
            {
                // 第一次点击，如果点击时间在间隔时间内，则设置定时器等待第二次点击
                if (Time.unscaledTime - _beginTime < _gapTime)
                {
                    _timeKey = TimeMgr.Ins.Timer(_gapTime, this, SimulationClick).Repeat(1).Build();
                    _firstTime = Time.unscaledTime;
                }
            }
            else
            {
                // 第二次及以后的点击，检查是否在间隔时间内
                if (Time.unscaledTime - _firstTime <= _gapTime)
                {
                    if (_nowkCnt == _clickCnt)
                    {
                        // 达到指定点击次数，触发回调
                        _fn0?.Invoke();
                        _fn1?.Invoke(e);
                        _touchId = -1;
                    }
                }
            }
        }
        
        /// <summary>
        /// 清理定时器
        /// </summary>
        private void _ClearTime()
        {
            if (_timeKey != 0)
            {
                TimeMgr.Ins.RemoveTimer(_timeKey);
                _timeKey = 0;
            }
        }
    }
    /// <summary>
    /// 长按事件数据类，用于处理长按手势
    /// </summary>
    public class UILongPressEventData
    {
        private GObject _target;           // 目标GObject
        private Func<bool> _fn;            // 长按触发函数
        private Vector2 _startPoint;       // 触摸开始位置
        private int _touchId;              // 触摸ID

        private float _first;              // 第一次触发时间
        private float _delay;              // 后续触发间隔
        private int _loopCnt;              // 循环触发次数
        private int _nowCnt;               // 当前已触发次数
        private long _timeKey;             // 定时器键值
        
        /// <summary>
        /// 手指按住后，移动超出此半径范围则手势停止。
        /// 用于检测用户是否在长按时移动手指超出允许范围
        /// </summary>
        private int _holdRangeRadius;
        
        /// <summary>
        /// 初始化长按事件
        /// </summary>
        /// <param name="gObject">目标GObject</param>
        /// <param name="fn">长按触发函数，返回true时停止长按</param>
        /// <param name="first">第一次触发时间（秒），-1表示不使用首次触发延迟</param>
        /// <param name="delay">后续触发间隔时间（秒）</param>
        /// <param name="loopCnt">循环触发次数，0表示无限循环</param>
        /// <param name="holdRangeRadius">手指按住后，移动超出此半径范围则手势停止</param>
        public void Init(GObject gObject, Func<bool> fn, float first, float delay, int loopCnt, int holdRangeRadius)
        {
            _touchId = -1;
            _target = gObject;
            _fn = fn;
            _delay = delay;
            _first = first;
            _nowCnt = 0;
            _loopCnt = loopCnt;
            _holdRangeRadius = holdRangeRadius;
            _target.onTouchBegin.Add(OnTouchBegin);
            _target.onTouchEnd.Add(OnTouchEnd);
        }

        /// <summary>
        /// 清理事件监听，将对象回收到对象池
        /// </summary>
        public void Clear()
        {
            if (_target != null)
            {
                _target.onTouchBegin.Remove(OnTouchBegin);
                _target.onTouchEnd.Remove(OnTouchEnd);
            }
            _target = null;
            _fn = null;
            _ClearTime();
            Pool.Push(this);
        }

        /// <summary>
        /// 执行长按逻辑
        /// 检查是否移动超出范围或触发函数返回true
        /// </summary>
        /// <returns>如果需要停止长按返回true，否则返回false</returns>
        private bool Run()
        {
            if (_fn == null) return false;
            Vector2 pt = _target.GlobalToLocal(Stage.inst.GetTouchPosition(_touchId));
            // 检查手指是否移动超出允许范围
            if (Mathf.Pow(pt.x - _startPoint.x, 2) + Mathf.Pow(pt.y - _startPoint.y, 2) > Mathf.Pow(_holdRangeRadius, 2))
            {
                return true;
            }
            // 检查触发函数是否返回true或已达到循环次数
            return _fn.Invoke() || (_loopCnt > 0 && _nowCnt >= _loopCnt);
        }

        /// <summary>
        /// 长按循环处理
        /// </summary>
        private void _Loop()
        {
            _nowCnt++;
            if (Run())
            {
                OnTouchEnd(null);
            }
        }
        
        /// <summary>
        /// 清理定时器
        /// </summary>
        private void _ClearTime()
        {
            if (_timeKey != 0)
            {
                TimeMgr.Ins.RemoveTimer(_timeKey);
                _timeKey = 0;
            }
            _touchId = -1;
        }
        
        /// <summary>
        /// 触摸开始事件处理
        /// </summary>
        private void OnTouchBegin(EventContext e)
        {
            if (_touchId != -1) return;
            if (_timeKey != 0) return;
            _startPoint = _target.GlobalToLocal(new Vector2(e.inputEvent.x, e.inputEvent.y));
            _touchId = e.inputEvent.touchId;
            _nowCnt = 0;
            _timeKey = TimeMgr.Ins.Timer(_delay, this, _Loop)
            .Loop()
            .FirstDelay(_first)
            .Build();
        }

        /// <summary>
        /// 触摸结束事件处理
        /// </summary>
        private void OnTouchEnd(EventContext e)
        {
            if (e != null && e.inputEvent.touchId != _touchId)
            {
                return;
            }
            if (_nowCnt > 0)
            {
                Stage.inst.CancelClick(_touchId);
            }
            _ClearTime();
        }
    }
}