using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    struct OverdueData<T>
    {
        public T Value { get; set; }
        public bool IsTimeout => TimeMgr.Ins.TotalTime >= _timeout;
        float _timeout;
        public OverdueData(T value)
        {
            Value = value;
            _timeout = 0;
        }
        public void UpdateTimeout(float timeout)
        {
            _timeout = TimeMgr.Ins.TotalTime + timeout;
        }

    }
    public class OverdueMap<K, T>
    {
        IDictionary<K, OverdueData<T>> _caches = new Dictionary<K, OverdueData<T>>();
        List<KeyValuePair<K, OverdueData<T>>> _timeouts = new List<KeyValuePair<K, OverdueData<T>>>();
        float _timeout;
        System.Action<T> _callBack;
        long _timeoutKey;
        public OverdueMap(float timeout, System.Action<T> callBack = null)
        {
            _timeout = timeout;
            _callBack = callBack;
        }
        public bool TryGetValue(K key, out T value)
        {
            if (_caches.TryGetValue(key, out var overdueData))
            {
                value = overdueData.Value;
                overdueData.UpdateTimeout(_timeout);
                return true;
            }
            value = default(T);
            return false;
        }
        public void Add(K key, T value)
        {
            if (_timeoutKey == 0 && Application.isPlaying)
            {
                //ÿ��һ��ʱ��������ڵĻ���
                _timeoutKey = TimeMgr.Ins.Timer(_timeout, this, _CheckTimeout).Loop().Build();
            }
            var data = new OverdueData<T>(value);
            data.UpdateTimeout(_timeout);
            _caches.Add(key, data);
        }

        public void Clear()
        {
            _caches.Clear();
            _timeouts.Clear();
            if (_timeoutKey > 0)
            {                
                TimeMgr.Ins.RemoveTimer(_timeoutKey);
                _timeoutKey = 0;
            }
        }

        void _CheckTimeout()
        {
            _timeouts.Clear();
            foreach (var kv in _caches)
            {
                if (kv.Value.IsTimeout)
                {
                    _timeouts.Add(kv);
                }
            }
            for (int i = _timeouts.Count - 1; i >= 0; i--)
            {
                var kv = _timeouts[i];
                _callBack?.Invoke(kv.Value.Value);
                _caches.Remove(kv.Key);
            }
        }
    }
}
