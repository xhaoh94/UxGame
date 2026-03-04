using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 高性能二叉堆 (Binary Heap / Priority Queue)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BinaryHeap<T>
    {
        private readonly List<T> _list;
        private readonly Func<T, T, int> _compare;

        public BinaryHeap(Func<T, T, int> compare)
        {
            _compare = compare;
            _list = new List<T>();
        }

        public int Count => _list.Count;

        /// <summary>
        /// 加入一个元素到堆中
        /// </summary>
        public void Push(T t)
        {
            _list.Add(t);
            _Up(_list.Count - 1);
        }

        /// <summary>
        /// 弹出堆顶元素
        /// </summary>
        public T Pop()
        {
            if (_list.Count == 0) return default(T);

            var top = _list[0];
            var lastIndex = _list.Count - 1;
            
            // 将最后一个元素移到堆顶
            _list[0] = _list[lastIndex];
            _list.RemoveAt(lastIndex);

            if (_list.Count > 0)
            {
                _Down(0);
            }

            return top;
        }

        /// <summary>
        /// 当堆内元素的值发生改变，可能破坏堆结构时，主动触发更新其在堆中的位置
        /// </summary>
        public void Update(T t)
        {
            // 对于引用类型，我们需要找到它当前在堆中的索引（O(N)操作）
            int index = _list.IndexOf(t);
            if (index == -1) return;

            // 无论是变大还是变小，都尝试 Up 和 Down。只有符合条件的才会被移动
            _Up(index);
            _Down(index);
        }

        /// <summary>
        /// 判断堆中是否包含某个元素
        /// </summary>
        public bool Contains(T t)
        {
            return _list.Contains(t);
        }

        public void Clear()
        {
            _list.Clear();
        }

        void _Swap(int idx1, int idx2)
        {
            var tem = _list[idx1];
            _list[idx1] = _list[idx2];
            _list[idx2] = tem;
        }

        void _Up(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (_compare.Invoke(_list[index], _list[parentIndex]) > 0)
                {
                    _Swap(index, parentIndex);
                    index = parentIndex;
                }
                else
                {
                    break;
                }
            }
        }

        void _Down(int index)
        {
            int count = _list.Count;
            while (index < count / 2)
            {
                int left = 2 * index + 1;
                int right = left + 1;
                int targetIndex = index;

                if (left < count && _compare.Invoke(_list[targetIndex], _list[left]) < 0)
                {
                    targetIndex = left;
                }

                if (right < count && _compare.Invoke(_list[targetIndex], _list[right]) < 0)
                {
                    targetIndex = right;
                }

                if (targetIndex != index)
                {
                    _Swap(index, targetIndex);
                    index = targetIndex;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
