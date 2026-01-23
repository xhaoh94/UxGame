using System.Collections.Generic;
using System.Runtime.CompilerServices;

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

            readonly List<IHandle> _handles = new List<IHandle>();
            readonly Dictionary<long, IHandle> _keyHandle = new Dictionary<long, IHandle>();
            readonly Dictionary<object, HashSet<long>> _tagkeys = new Dictionary<object, HashSet<long>>();

            readonly List<IHandle> _waitAdds = new List<IHandle>();
            readonly List<IHandle> _waitDels = new List<IHandle>();

            public void Clear()
            {
                _handles.Clear();
                _keyHandle.Clear();
                _tagkeys.Clear();
                _waitAdds.Clear();
                _waitDels.Clear();
#if UNITY_EDITOR
                _descEditor.Clear();
#endif
            }

            bool _needSort;

            public void Run()
            {
                if (_waitDels.Count > 0)
                {
                    HashSet<IHandle> delSet = null;
                    if (_waitDels.Count > 5)
                    {
                        delSet = new HashSet<IHandle>(_waitDels);
                    }

                    for (int i = 0; i < _waitDels.Count; i++)
                    {
                        var handle = _waitDels[i];
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
                        var key = handle.Key;
                        _keyHandle.Remove(key);

                        var tag = handle.Tag;
                        if (tag != null)
                        {
                            if (_tagkeys.TryGetValue(tag, out var keys))
                            {
                                keys.Remove(key);
                                if (keys.Count == 0) _tagkeys.Remove(tag);
                            }
                        }

                        handle.Release();
                        if (delSet == null)
                        {
                            _handles.Remove(handle);
                        }
                    }

                    if (delSet != null)
                    {
                        _handles.RemoveAll(delSet.Contains);
                    }
                    _waitDels.Clear();
                }
                
                if (_waitAdds.Count > 0)
                {
                    // 优化：当新增数量较少时(<=10)，使用二分插入比全量Sort效率更高
                    if (_waitAdds.Count <= 10)
                    {
                        for (int i = 0; i < _waitAdds.Count; i++)
                        {
                            var handle = _waitAdds[i];
                            AddHandleToMap(handle);
                            Sort(handle);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < _waitAdds.Count; i++)
                        {
                            var handle = _waitAdds[i];
                            AddHandleToMap(handle);
                        }
                        _handles.AddRange(_waitAdds);
                        _needSort = true;
                    }
                    _waitAdds.Clear();
                }

                if (_needSort)
                {
                    _handles.Sort((a, b) => a.Compare(b));
                    _needSort = false;
                }

#if UNITY_EDITOR
                if (__isEvent)
                {
                    __Debugger_Event();
                }
#endif


                OnRun();
            }

            void AddHandleToMap(IHandle handle)
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
                _keyHandle.Add(handle.Key, handle);

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

            private void Sort(IHandle handle)
            {
                var startIndex = 0;
                var endIndex = _handles.Count;

                while (startIndex < endIndex)
                {
                    var index = startIndex + ((endIndex - startIndex) >> 1);
                    // 注意：此处必须使用与List.Sort一致的比较逻辑
                    // 之前List.Sort((a,b)=>a.Compare(b))是升序
                    // 此处handle.Compare(_handles[index]) > 0 表示handle > handles[index]，应插在后面
                    int compareResult = handle.Compare(_handles[index]);

                    if (compareResult == 0)
                    {
                        _handles.Insert(index, handle);
                        return;
                    }
                    else if (compareResult > 0)
                    {
                        startIndex = index + 1;
                    }
                    else
                    {
                        endIndex = index;
                    }
                }

                // 如果未找到合适位置，插入到 startIndex
                _handles.Insert(startIndex, handle);
            }

            void OnRun()
            {
#if UNITY_EDITOR
                __isEvent = false;
#endif
                if (_handles.Count <= 0) return;

                for (var i = 0; i < _handles.Count; i++)
                {
                    var handler = _handles[i];
                    switch (handler.Run())
                    {
                        case RunStatus.Wait:
                            return;
                        case RunStatus.Update:
#if UNITY_EDITOR
                            __isEvent = true;
#endif
                            _needSort = true;
                            break;
                        case RunStatus.Done:
                            Remove(handler);
                            break;
                        case RunStatus.None:
#if UNITY_EDITOR
                            __isEvent = true;
#endif
                            break;
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
                foreach (var key in keys)
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