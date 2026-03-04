using System;
using System.Collections.Generic;

namespace Ux
{

    public partial class TimeMgr
    {
        public enum TimeType
        {
            Time,
            Frame,
            TimeStamp,
            Cron
        }

        public enum RunStatus
        {
            None,
            Error,
            Wait,
            Update,
            Done
        }

        public enum Status
        {
            Normal,
            WaitDel,
            Release
        }

        partial class HandleMap
        {
            readonly TimeType _timeType;
            public TimeType TimeType => _timeType;

            public HandleMap(TimeType timeType)
            {
                _timeType = timeType;
            }

            // 用作最小堆底层容器
            readonly List<IHandle> _handles = new List<IHandle>();
            readonly Dictionary<long, IHandle> _keyHandle = new Dictionary<long, IHandle>();
            readonly Dictionary<object, HashSet<long>> _tagkeys = new Dictionary<object, HashSet<long>>();

            readonly List<IHandle> _waitAdds = new List<IHandle>();
            readonly List<IHandle> _waitDels = new List<IHandle>();
            readonly List<IHandle> _waitUpdates = new List<IHandle>();

            public void Clear()
            {
                _handles.Clear();
                _keyHandle.Clear();
                _tagkeys.Clear();
                _waitAdds.Clear();
                _waitDels.Clear();
                _waitUpdates.Clear();
                _waitDels.Clear();
#if UNITY_EDITOR
                _descEditor.Clear();
#endif
            }

            public void Run()
            {
                if (_waitDels.Count > 0)
                {
                    for (int i = 0; i < _waitDels.Count; i++)
                    {
                        var handle = _waitDels[i];
                        RemoveFromDicts(handle);
                        // Status 已在 Remove 时被置为 WaitDel，在 Heap 弹栈时或重构时才会调用 Release 回收对象池
                    }
                    _waitDels.Clear();
                    PurgeDeleted(); // 集中清理无效定时器并重建堆
                }

                if (_waitAdds.Count > 0)
                {
                    for (int i = 0; i < _waitAdds.Count; i++)
                    {
                        var handle = _waitAdds[i];
                        AddToDicts(handle);
                        HeapPush(handle);
                    }
                    _waitAdds.Clear();
                }

#if UNITY_EDITOR
                if (__isEvent)
                {
                    __Debugger_Event();
                }
#endif
                if (_waitUpdates.Count > 0)
                {
                    for (int i = 0; i < _waitUpdates.Count; i++)
                    {
                        HeapPush(_waitUpdates[i]);
                    }
                    _waitUpdates.Clear();
                }

                OnRun();
            }
            void AddToDicts(IHandle handle)
            {
#if UNITY_EDITOR
                var exeDesc = handle.MethodName;
                if (!_descEditor.TryGetValue(exeDesc, out var temList))
                {
                    temList = new TimeList(exeDesc);
                    _descEditor.Add(exeDesc, temList);
                }

                temList.Add(handle);
                __Debugger_Event();
#endif                     
                _keyHandle[handle.Key] = handle;

                var tag = handle.Tag;
                if (tag != null)
                {
                    if (!_tagkeys.TryGetValue(tag, out var keys))
                    {
                        keys = new HashSet<long>();
                        _tagkeys.Add(tag, keys);
                    }
                    keys.Add(handle.Key);
                }
            }

            void RemoveFromDicts(IHandle handle)
            {
#if UNITY_EDITOR
                var exeDesc = handle.MethodName;
                if (_descEditor.TryGetValue(exeDesc, out var temList))
                {
                    if (temList.Remove(handle))
                    {
                        _descEditor.Remove(exeDesc);
                        __Debugger_Event();
                    }
                }
#endif
                _keyHandle.Remove(handle.Key);

                var tag = handle.Tag;
                if (tag != null)
                {
                    if (_tagkeys.TryGetValue(tag, out var keys))
                    {
                        keys.Remove(handle.Key);
                        if (keys.Count == 0) _tagkeys.Remove(tag);
                    }
                }
            }

            // O(N) 一次性回收所有标记为 WaitDel 的内存，并原址重建最小堆
            void PurgeDeleted()
            {
                int validCount = 0;
                for (int i = 0; i < _handles.Count; i++)
                {
                    var h = _handles[i];
                    if (h.Status == Status.WaitDel)
                    {
                        h.Release();
                    }
                    else
                    {
                        _handles[validCount++] = h;
                    }
                }

                if (validCount < _handles.Count)
                {
                    _handles.RemoveRange(validCount, _handles.Count - validCount);
                    // O(N) 建堆
                    for (int i = _handles.Count / 2 - 1; i >= 0; i--)
                    {
                        HeapifyDown(i);
                    }
                }
            }

            #region Min-Heap Implementation

            void HeapPush(IHandle handle)
            {
                _handles.Add(handle);
                HeapifyUp(_handles.Count - 1);
            }

            IHandle HeapPop()
            {
                if (_handles.Count == 0) return null;
                var root = _handles[0];
                var last = _handles[_handles.Count - 1];
                _handles.RemoveAt(_handles.Count - 1);
                
                if (_handles.Count > 0)
                {
                    _handles[0] = last;
                    HeapifyDown(0);
                }
                return root;
            }

            void HeapifyUp(int index)
            {
                var item = _handles[index];
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (item.Compare(_handles[parent]) >= 0) break;
                    _handles[index] = _handles[parent];
                    index = parent;
                }
                _handles[index] = item;
            }

            void HeapifyDown(int index)
            {
                int count = _handles.Count;
                var item = _handles[index];
                while (index < count / 2)
                {
                    int left = 2 * index + 1;
                    int right = left + 1;
                    int smallest = left;
                    
                    if (right < count && _handles[right].Compare(_handles[left]) < 0)
                    {
                        smallest = right;
                    }
                    
                    if (item.Compare(_handles[smallest]) <= 0) break;
                    
                    _handles[index] = _handles[smallest];
                    index = smallest;
                }
                _handles[index] = item;
            }

            #endregion

            void OnRun()
            {
#if UNITY_EDITOR
                __isEvent = false;
#endif
                if (_handles.Count == 0) return;

                // 使用 _waitUpdates 列表来暂存本帧更新的定时器，下一帧再压回堆中，避免无限循环
                while (_handles.Count > 0)
                {
                    var handler = _handles[0];

                    if (handler.Status == Status.WaitDel)
                    {
                        HeapPop();
                        handler.Release();
                        continue;
                    }
                    var status = handler.Run();

                    if (status == RunStatus.Wait)
                    {
                        // 最小堆顶都还没到时间，后面的肯定也没到
                        break;
                    }

                    HeapPop();

                    if (status == RunStatus.Update)
                    {
#if UNITY_EDITOR
                        __isEvent = true;
#endif
                        // 状态变为 Update 表示时间修改了需要循环执行，暂存起来下一帧再加入堆
                        _waitUpdates.Add(handler);
                    }
                    else if (status == RunStatus.Done || status == RunStatus.None)
                    {
#if UNITY_EDITOR
                        __isEvent = true;
#endif
                        RemoveFromDicts(handler);
                        handler.Release();
                    }
                }
            }

            public void Add(IHandle handle)
            {
                if (_waitAdds.Contains(handle)) return;
                handle.Status = Status.Normal;
                _waitAdds.Add(handle);
            }

            public IHandle Get(long key)
            {
                if (_keyHandle.TryGetValue(key, out var handle) && handle.Status == Status.Normal)
                {
                    return handle;
                }
                return _waitAdds.Find(x => x.Key == key);
            }

            public void RemoveAll(object tag)
            {
                if (tag == null) return;
                
                if (_waitAdds.Count > 0)
                {
                    for (int i = _waitAdds.Count - 1; i >= 0; i--)
                    {
                        var wa = _waitAdds[i];
                        if (wa.Tag == tag)
                        {
                            _waitAdds.RemoveAt(i);
                        }
                    }
                }
                
                if (!_tagkeys.TryGetValue(tag, out var keys)) return;
                // 转成数组防遍历报错
                foreach (var key in new List<long>(keys))
                {
                    Remove(key);
                }
            }

            public void Remove(long key)
            {
                if (!_keyHandle.TryGetValue(key, out var handle))
                {
                    if (_waitAdds.Count > 0)
                    {
                        var index = _waitAdds.FindIndex(x => x.Key == key);
                        if (index >= 0) _waitAdds.RemoveAt(index);
                    }
                    return;
                }
                Remove(handle);
            }

            void Remove(IHandle handle)
            {
                if (handle.Status == Status.WaitDel) return;
                handle.Status = Status.WaitDel;
                _waitDels.Add(handle);
            }
        }
    }
}