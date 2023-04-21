using FairyGUI;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Ux
{
    public class UIEvent
    {
        private List<EventListener> _listeners;//组件事件        
        private Dictionary<int, UILongClickEventData> _longClickEvtList;//长按
        private Dictionary<int, UIDoubleClickEventData> _doubleClickEvtList;//多次点击

        public void Release()
        {
            Pool.Push(this);
        }

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
            if (_longClickEvtList != null)
            {
                foreach (var kv in _longClickEvtList)
                {
                    kv.Value.Clear();
                }
                _longClickEvtList.Clear();
            }

            if (_doubleClickEvtList != null)
            {
                foreach (var kv in _doubleClickEvtList)
                {
                    kv.Value.Clear();
                }
                _doubleClickEvtList.Clear();
            }
        }

        public void AddDoubleClick(GObject gObject, EventCallback0 fn0, int clickCnt = 2, float gapTime = 0.3f)
        {
            _doubleClickEvtList ??= new Dictionary<int, UIDoubleClickEventData>();
            if (_doubleClickEvtList.ContainsKey(gObject.GetHashCode())) return;
            var item = Pool.Get<UIDoubleClickEventData>();
            item.Init(gObject, fn0, clickCnt, gapTime);
            _doubleClickEvtList.Add(gObject.GetHashCode(), item);
        }
        public void AddDoubleClick(GObject gObject, EventCallback1 fn1, int clickCnt = 2, float gapTime = 0.3f)
        {
            _doubleClickEvtList ??= new Dictionary<int, UIDoubleClickEventData>();
            if (_doubleClickEvtList.ContainsKey(gObject.GetHashCode())) return;
            var item = Pool.Get<UIDoubleClickEventData>();
            item.Init(gObject, fn1, clickCnt, gapTime);
            _doubleClickEvtList.Add(gObject.GetHashCode(), item);
        }
        public void AddLongClick(GObject gObject, Func<bool> fn, float first, float delay, int loop, int holdRangeRadius)
        {
            _longClickEvtList ??= new Dictionary<int, UILongClickEventData>();
            if (_longClickEvtList.ContainsKey(gObject.GetHashCode())) return;
            var item = Pool.Get<UILongClickEventData>();
            item.Init(gObject, fn, first, delay, loop, holdRangeRadius);
            _longClickEvtList.Add(gObject.GetHashCode(), item);
        }
        public void AddEvent(EventListener listener, EventCallback1 fn)
        {
            _listeners ??= new List<EventListener>();
            if (_listeners.Contains(listener)) return;
            listener.Add(fn);
        }
        public void AddEvent(EventListener listener, EventCallback0 fn)
        {
            _listeners ??= new List<EventListener>();
            if (_listeners.Contains(listener)) return;
            listener.Add(fn);
        }
    }

    public class UIDoubleClickEventData
    {
        private GObject _target;
        private EventCallback1 _fn1;
        private EventCallback0 _fn0;
        private float _gapTime;
        private long _timeKey;
        private int _touchId;
        private float _beginTime;
        private float _firstTime;
        private int _clickCnt;
        private int _nowkCnt;

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
        private void OnTouchBegin(EventContext e)
        {
            _touchId = e.inputEvent.touchId;
            _ClearTime();
            if (Time.unscaledTime - _firstTime > _gapTime)//超出多击时间，重置次数
            {
                _nowkCnt = 0;
            }
            _beginTime = Time.unscaledTime;
        }
        //模拟点击
        private void SimulationClick()
        {
            _timeKey = 0;
            _nowkCnt = 0;
            _target.onClick.Call();
        }
        private void OnTouchEnd(EventContext e)
        {
            if (e.inputEvent.touchId != _touchId) return;
            Stage.inst.CancelClick(e.inputEvent.touchId);
            _nowkCnt++;
            if (_nowkCnt == 1)
            {
                if (Time.unscaledTime - _beginTime < _gapTime)
                {
                    _timeKey = TimeMgr.Instance.DoOnce(_gapTime, SimulationClick);
                    _firstTime = Time.unscaledTime;
                }
            }
            else
            {
                if (Time.unscaledTime - _firstTime <= _gapTime)
                {
                    if (_nowkCnt == _clickCnt)
                    {
                        _fn0?.Invoke();
                        _fn1?.Invoke(e);
                        _touchId = -1;
                    }
                }
            }
        }
        private void _ClearTime()
        {
            if (_timeKey != 0)
            {
                TimeMgr.Instance.RemoveKey(_timeKey);
                _timeKey = 0;
            }
        }
    }
    public class UILongClickEventData
    {
        private GObject _target;
        private Func<bool> _fn;
        private Vector2 _startPoint;
        private int _touchId;

        private float _first;
        private float _delay;
        private int _loopCnt;
        private int _nowCnt;
        private long _timeKey;
        /// <summary>
        /// 手指按住后，移动超出此半径范围则手势停止。
        /// </summary>
        private int _holdRangeRadius;
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

        private bool Run()
        {
            if (_fn == null) return false;
            Vector2 pt = _target.GlobalToLocal(Stage.inst.GetTouchPosition(_touchId));
            if (Mathf.Pow(pt.x - _startPoint.x, 2) + Mathf.Pow(pt.y - _startPoint.y, 2) > Mathf.Pow(_holdRangeRadius, 2))
            {
                return true;
            }
            return _fn.Invoke() || (_loopCnt > 0 && _nowCnt >= _loopCnt);
        }

        private void _Loop()
        {
            _nowCnt++;
            if (Run())
            {
                OnTouchEnd(null);
            }
        }
        private void _ClearTime()
        {
            if (_timeKey != 0)
            {
                TimeMgr.Instance.RemoveKey(_timeKey);
                _timeKey = 0;
            }
            _touchId = -1;
        }
        private void OnTouchBegin(EventContext e)
        {
            if (_touchId != -1) return;
            if (_timeKey != 0) return;
            _startPoint = _target.GlobalToLocal(new Vector2(e.inputEvent.x, e.inputEvent.y));
            _touchId = e.inputEvent.touchId;
            _nowCnt = 0;
            _timeKey = TimeMgr.Instance.DoLoop(_first, _delay, _Loop);
        }

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