using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    partial class EventMgr
    {
        public void Off(int eType, FastMethodInfo action)
        {
            var key = GetKey(eType, action);
            RemoveByKey(key);
        }

        /// <summary>
        /// 删除对应的事件和注册方法
        /// </summary>
        /// <param name="eType"></param>
        /// <param name="action"></param>
        public void Off(int eType, object tag, Action action)
        {
            _Remove(eType, tag, action);
        }

        /// <summary>
        /// 删除所有注册此方法的事件
        /// </summary>
        /// <param name="action"></param>
        public void Off(Action action)
        {
            _Remove(action);
        }

        /// <summary>
        /// 删除对应的事件和注册方法
        /// </summary>
        /// <param name="eType"></param>
        /// <param name="action"></param>
        public void Off<A>(int eType, object tag, Action<A> action)
        {
            _Remove(eType, tag, action);
        }

        /// <summary>
        /// 删除所有注册此方法的事件
        /// </summary>
        /// <param name="action"></param>
        public void Off<A>(Action<A> action)
        {
            _Remove(action);
        }

        /// <summary>
        /// 删除对应的事件和注册方法
        /// </summary>
        /// <param name="eType"></param>
        /// <param name="action"></param>
        public void Off<A, B>(int eType, object tag, Action<A, B> action)
        {
            _Remove(eType, tag, action);
        }

        /// <summary>
        /// 删除所有注册此方法的事件
        /// </summary>
        /// <param name="action"></param>
        public void Off<A, B>(Action<A, B> action)
        {
            _Remove(action);
        }

        /// <summary>
        /// 删除对应的事件和注册方法
        /// </summary>
        /// <param name="eType"></param>
        /// <param name="action"></param>
        public void Off<A, B, C>(int eType, object tag, Action<A, B, C> action)
        {
            _Remove(eType, tag, action);
        }

        /// <summary>
        /// 删除所有注册此方法的事件
        /// </summary>
        /// <param name="action"></param>
        public void Off<A, B, C>(Action<A, B, C> action)
        {
            _Remove(action);
        }

        /// <summary>
        /// 删除所有注册此标签的事件
        /// </summary>
        public void OffTag(object tag)
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

            int hashCode = tag.GetHashCode();
            if (!_tagKeys.TryGetValue(hashCode, out var keys)) return;
            RemoveByKey(keys);
        }
    }
}
