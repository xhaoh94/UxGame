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

        protected void AddChild(int tId)
        {
            _childs ??= new List<TagBase>();
            if (_childs.FindIndex(x => x.TagId == tId) >= 0) return;
            var tag = TagMgr.Instance.GetTag(tId);
            if (tag == null) return;
            _AddTag(tag);
        }

        protected void AddChild<T>() where T : TagBase
        {
            AddChild(typeof(T).FullName.ToHash());
        }

        public void AddChild<T>(int tId, object args) where T : TagBase
        {
            _childs ??= new List<TagBase>();
            if (_childs.FindIndex(x => x.TagId == tId) >= 0) return;
            var tt = typeof(T);
            var tag = (TagBase)Activator.CreateInstance(tt);
            tag.Init(tId, args);
            _AddTag(tag);
        }

        public void RemoveChild(int tId)
        {
            if (_childs == null) return;
            var index = _childs.FindIndex(x => x.TagId == tId);
            if (index < 0) return;
            _childs[index]._parents?.Remove(this);
            _childs.RemoveAt(index);
            _DoMessage();
        }

        public void RemoveAll()
        {
            if (_childs == null) return;
            foreach (var tag in _childs)
            {
                tag._parents?.Remove(this);
            }

            _childs.Clear();
            _DoMessage();
        }

        public override void Clear()
        {
            base.Clear();
            if (_childs == null) return;
            foreach (var tag in _childs)
            {
                tag.Clear();
            }

            _childs.Clear();
        }


        private void _AddTag(TagBase tag)
        {
            _childs.Add(tag);
            tag._parents ??= new List<TagBase>();
            if (!tag._parents.Contains(this))
            {
                tag._parents.Add(this);
            }
        }


        protected override bool OnCheck()
        {
            return _childs != null && _childs.Select(child => child.IsRed()).Any(b => b);
        }

        public TagBase Find(int tId)
        {
            return Find<TagBase>(tId);
        }

        public T Find<T>() where T : TagBase
        {
            return Find<T>(typeof(T).FullName.ToHash());
        }

        public T Find<T>(int tId) where T : TagBase
        {
            if (_childs == null) return null;
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