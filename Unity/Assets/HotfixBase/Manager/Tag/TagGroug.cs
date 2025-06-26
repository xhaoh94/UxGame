using System;
using System.Collections.Generic;
using System.Linq;

namespace Ux
{
    public abstract class TagGroup : TagBase
    {
        protected List<TagBase> _childs;        

        protected override void OnInit()
        {
            base.OnInit();
            OnInitChildren();
        }

        protected abstract void OnInitChildren();

        /// <summary>
        /// 添加单例类型的红点子项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void AddChild<T>() where T : TagBase
        {
            _childs ??= new List<TagBase>();
            var tag = TagMgr.Ins.GetTag<T>();
            if (tag == null) return;
            if (_childs.Contains(tag)) return;
            _AddTag(tag);
        }
        /// <summary>
        /// 添加动态创建的红点子项
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tId"></param>
        /// <param name="args"></param>
        public void AddChild<T>(int tId, object args) where T : TagBase
        {
            tId = (int)IDGenerater.GenerateId(typeof(T).FullName.ToHash(), tId);
            _childs ??= new List<TagBase>();
            if (_childs.FindIndex(x => x.TagId == tId) >= 0)
            {
                Log.Error($"重复创建动态红点{typeof(T).FullName}-{tId}");
                return;
            }
            var tag = Pool.Get<T>();
            tag.Init(tId, args);
            _AddTag(tag);
        }

        protected void RemoveChild(int tId)
        {
            if (_childs == null) return;
            var index = _childs.FindIndex(x => x.TagId == tId);
            if (index < 0) return;
            var child = _childs[index];
            _childs.RemoveAt(index);
            child.RemoveParent(this);
            _DoMessage();
        }

        public void RemoveChild<T>(int tId) where T : TagBase
        {
            tId = (int)IDGenerater.GenerateId(typeof(T).FullName.ToHash(), tId);
            RemoveChild(tId);
        }
        public void RemoveChild<T>() where T : TagBase
        {
            RemoveChild(typeof(T).FullName.ToHash());
        }
        public void RemoveAll()
        {
            if (_childs == null) return;
            foreach (var tag in _childs)
            {
                tag.RemoveParent(this);
            }
            _childs.Clear();
            _DoMessage();
        }

        public override void Release()
        {
            base.Release();
            if (_childs == null) return;
            foreach (var tag in _childs)
            {
                tag.Release();
            }

            _childs.Clear();
        }


        private void _AddTag(TagBase tag)
        {
            _childs.Add(tag);
            tag.AddParent(this);
            _DoMessage();
        }


        protected override bool OnCheck()
        {
            return _childs != null && _childs.Select(child => child.IsRed()).Any(b => b);
        }

        public T Find<T>() where T : TagBase
        {
            return Find<T>(typeof(T).FullName.ToHash());
        }

        public T Find<T>(int tId) where T : TagBase
        {
            if (_childs == null) return null;
            tId = (int)IDGenerater.GenerateId(typeof(T).FullName.ToHash(), tId);
            var index = _childs.FindIndex(x => x.TagId == tId);
            if (index < 0)
                return null;
            return (T)_childs[index];
        }

        protected override int CheckRedNum()
        {
            return _childs == null
                ? 0
                : _childs.Select(child => child.RedNum()).Where(childNum => childNum > 0).Sum();
        }
    }
}