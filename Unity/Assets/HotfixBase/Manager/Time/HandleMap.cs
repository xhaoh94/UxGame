using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Ux
{
    
    public partial class TimeMgr
    {
#if UNITY_EDITOR
        /// <summary>定时器签名，用于重复注册检测</summary>
        public struct TimerSignature : System.IEquatable<TimerSignature>
        {
            public int ActionHash;
            public int TagHash;
            public TimeMgr.TimeType TimeType;

            public bool Equals(TimerSignature other)
            {
                return ActionHash == other.ActionHash &&
                       TagHash == other.TagHash &&
                       TimeType == other.TimeType;
            }

            public override bool Equals(object obj)
            {
                return obj is TimerSignature other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + ActionHash;
                    hash = hash * 31 + TagHash;
                    hash = hash * 31 + (int)TimeType;
                    return hash;
                }
            }
        }
#endif

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
            readonly Dictionary<int, List<long>> _tagkeys = new Dictionary<int, List<long>>();

#if UNITY_EDITOR
            // 签名相关字典：用于重复检测和删除
            readonly Dictionary<TimerSignature, long> _signToKey = new Dictionary<TimerSignature, long>();
            readonly Dictionary<long, TimerSignature> _keyToSign = new Dictionary<long, TimerSignature>();

            /// <summary>注册签名映射</summary>
            public void RegisterSignature(TimerSignature sign, long key)
            {
                _signToKey[sign] = key;
                _keyToSign[key] = sign;
            }

            /// <summary>通过签名获取 key</summary>
            public bool TryGetKeyBySignature(TimerSignature sign, out long key)
            {
                return _signToKey.TryGetValue(sign, out key);
            }
#endif

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
                _signToKey.Clear();
                _keyToSign.Clear();
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

#if UNITY_EDITOR
                        // 清理签名映射
                        if (_keyToSign.TryGetValue(key, out var sign))
                        {
                            _keyToSign.Remove(key);
                            _signToKey.Remove(sign);
                        }
#endif

                        var tag = handle.Tag;
                        if (tag != null)
                        {
                            var hashCode = RuntimeHelpers.GetHashCode(tag);
                            if (_tagkeys.TryGetValue(hashCode, out var keys))
                            {
                                keys.Remove(key);
                                if (keys.Count == 0) _tagkeys.Remove(hashCode);
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

                if (_needSort)
                {
                    _handles.Sort((a, b) => b.Compare(a));
                    _needSort = false;
                }

#if UNITY_EDITOR
                if (__isEvent)
                {
                    __Debugger_Event();
                }
#endif


                while (_waitAdds.Count > 0)
                {
                    var handle = _waitAdds[0];
                    _waitAdds.RemoveAt(0);
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
                    Sort(handle);
                    _keyHandle.Add(handle.Key, handle);

                    var tag = handle.Tag;
                    if (tag != null)
                    {
                        int hashCode = RuntimeHelpers.GetHashCode(tag);
                        if (!_tagkeys.TryGetValue(hashCode, out var keys))
                        {
                            keys = new List<long>();
                            _tagkeys.Add(hashCode, keys);
                        }

                        keys.Add(handle.Key);
                    }
                }

                OnRun();
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

            private void Sort(IHandle handle)
            {
                var startIndex = 0;
                var endIndex = _handles.Count;

                while (startIndex < endIndex)
                {
                    var index = startIndex + ((endIndex - startIndex) >> 1);
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
                int hashCode = RuntimeHelpers.GetHashCode(tag);
                if (!_tagkeys.TryGetValue(hashCode, out var keys)) return;
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
                if (_waitDels.Contains(handle)) return;
                handle.Status = Status.WaitDel;
                _waitDels.Add(handle);
            }
        }
    }
}