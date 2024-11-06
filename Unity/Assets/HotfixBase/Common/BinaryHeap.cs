using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// ¶þ²æ¶Ñ
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BinaryHeap<T>
    {
        public List<T> _list;
  
        Func<T, T, int> _compare;
        public BinaryHeap(Func<T, T, int> compare)
        {
            _compare = compare;
            _list = new List<T>();
        }
        public int Count => _list.Count;
        public void Sort()
        {
            //for (int i = Count / 2 - 1; i >= 0; i--)
            //{
            //    _Down(i, Count);
            //}
            //Log.Info(_list);
            for (int i = Count - 1; i >= 0; i--)
            {
                _Swap(i,0);
                _Down(0,i);
            }
        }
        public void Push(T t,bool isSort = false)
        {
            _list.Add(t);
            _Up(_list.Count - 1);
            if (isSort&&_list.Count > 1)
            {
                Sort();
            }
        }
        public void Clear()
        {
            _list.Clear();
        }
        public T Pop()
        {
            if (_list.Count > 0)
            {
                var top = _list[0];
                var downIndex = _list.Count - 1;
                _list[0] = _list[downIndex];
                _list.RemoveAt(downIndex);
                _Down(0, Count);
                return top;
            }
            return default(T);
        }
        void _Swap(int idx1, int idx2)
        {
            var tem = _list[idx1];
            _list[idx1] = _list[idx2];
            _list[idx2] = tem;
        }
        void _Up(int index)
        {
            var parentIndex = (index + 1) / 2 - 1;
            if (parentIndex >= 0 && _compare.Invoke(_list[index], _list[parentIndex]) > 0)
            {
                _Swap(index, parentIndex);
                _Up(parentIndex);
            }
        }
        void _Down(int index,int count)
        {
            var left = 2 * index + 1;
            var right = left + 1;
            var temIndex = index;
            if (left < count && _compare.Invoke(_list[temIndex], _list[left]) < 0)
            {
                temIndex = left;
            }
            if (right < count && _compare.Invoke(_list[temIndex], _list[right]) < 0)
            {
                temIndex = right;
            }
            if (temIndex != index)
            {
                _Swap(index, temIndex);
                _Down(temIndex, count);
            }
        }
    }
}
