using System;
using System.Collections.Generic;

namespace Ux
{    
    public struct BoolValue
    {
        Action<bool> _onChanged;
        [EEViewer("标签")]
        HashSet<string> _tags;
        public BoolValue(Action<bool> onChanged)
        {
            _onChanged = onChanged;
            _tags = new HashSet<string>();
        }

        public void Set(string tag, bool v)
        {
            if (v)
            {
                if (_tags.Remove(tag) && _tags.Count == 0)
                {
                    _onChanged?.Invoke(true);
                }
            }
            else
            {
                if (_tags.Add(tag) && _tags.Count == 1)
                {
                    _onChanged?.Invoke(false);
                }
            }
        }
        public bool Value => _tags != null && _tags.Count == 0;
        public void Reset()
        {
            _tags.Clear();
        }
        public void Release()
        {
            Reset();
            _onChanged = null;
            _tags = null;
        }
    }
}