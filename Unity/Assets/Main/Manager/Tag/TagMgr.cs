using FairyGUI;
using System;
using System.Collections.Generic;

namespace Ux
{
    public class TagMgr : Singleton<TagMgr>
    {
        public struct TagParse
        {
            public TagParse(Type type)
            {
                this.type = type;
                tagId = type.FullName.ToHash();
            }

            public Type type { get; }

            public int tagId { get; }
        }

        private readonly Dictionary<int, TagBase> _keyToMap = new Dictionary<int, TagBase>();
        private readonly Dictionary<int, Type> _keyToType = new Dictionary<int, Type>();

        private Dictionary<GObject, TagBase> _displayToTag;
        private Dictionary<TagBase, List<GObject>> _tagToDisplay;


        private Dictionary<GTextField, TagBase> _textToTag;
        private Dictionary<TagBase, List<GTextField>> _tagToText;

        public void Add(List<TagParse> tags)
        {
            foreach (var tag in tags)
            {
                if (_keyToType.ContainsKey(tag.tagId))
                {
                    Log.Error("重负注册单例红点tagId:{1}", tag.tagId);
                    continue;
                }

                _keyToType.Add(tag.tagId, tag.type);
            }
        }

        public void Release()
        {
            foreach (var kv in _keyToMap)
            {
                kv.Value.Clear();
            }

            _keyToMap.Clear();
        }

        public bool Check(int tId)
        {
            var tag = GetTag(tId);
            return tag != null && tag.IsRed();
        }

        public T GetTag<T>() where T : TagBase
        {
            return GetTag<T>(typeof(T).FullName.ToHash());
        }

        public TagBase GetTag(int tId)
        {
            return GetTag<TagBase>(tId);
        }

        public T GetTag<T>(int tId) where T : TagBase
        {
            if (_keyToMap.TryGetValue(tId, out var tag)) return (T)tag;
            if (_keyToType.TryGetValue(tId, out var tt))
            {
                tag = (TagBase)Activator.CreateInstance(tt);
                tag.Init(tId, null);
                _keyToMap.Add(tId, tag);
            }
            else
            {
                return null;
            }

            return (T)tag;
        }

        public void On(TagBase tag, GObject gObj)
        {
            if (gObj == null) return;
            this._displayToTag ??= new Dictionary<GObject, TagBase>();
            this._tagToDisplay ??= new Dictionary<TagBase, List<GObject>>();
            if (_displayToTag.TryGetValue(gObj, out var tempTag) && tempTag == tag)
            {
                return;
            }

            Off(gObj);
            if (!_tagToDisplay.TryGetValue(tag, out var objList))
            {
                objList = new List<GObject>();
                _tagToDisplay.Add(tag, objList);
            }

            objList.Add(gObj);
            gObj.visible = tag.IsRed();
            _displayToTag.Add(gObj, tag);
        }

        public void On(TagBase tag, GTextField text)
        {
            if (text == null) return;
            this._textToTag ??= new Dictionary<GTextField, TagBase>();
            this._tagToText ??= new Dictionary<TagBase, List<GTextField>>();
            if (_textToTag.TryGetValue(text, out var tempTag) && tempTag == tag)
            {
                return;
            }

            Off(text);
            if (!_tagToText.TryGetValue(tag, out var texts))
            {
                texts = new List<GTextField>();
                _tagToText.Add(tag, texts);
            }

            texts.Add(text);
            text.visible = tag.IsRed();
            if (text.visible) text.text = tag.RedNum().ToString();
            _textToTag.Add(text, tag);
        }

        public void On<T>(GObject gObj) where T : TagBase
        {
            On(typeof(T).FullName.ToHash(), gObj);
        }

        public void On(int tId, GObject gObj)
        {
            if (gObj == null) return;
            var tag = GetTag(tId);
            On(tag, gObj);
        }

        public void On<T>(GTextField text) where T : TagBase
        {
            On(typeof(T).FullName.ToHash(), text);
        }

        public void On(int tId, GTextField text)
        {
            if (text == null) return;
            var tag = GetTag(tId);
            On(tag, text);
        }

        public void Off(GTextField text)
        {
            if (text == null) return;
            if (_textToTag == null) return;
            if (!_textToTag.TryGetValue(text, out var tag))
            {
                return;
            }

            if (_tagToText.TryGetValue(tag, out var texts))
            {
                texts.Remove(text);
                text.visible = false;
            }

            _textToTag.Remove(text);
        }

        public void Off(GObject gObj)
        {
            if (gObj == null) return;
            if (_displayToTag == null) return;
            if (!_displayToTag.TryGetValue(gObj, out var tag))
            {
                return;
            }

            if (_tagToDisplay.TryGetValue(tag, out var list))
            {
                gObj.visible = false;
                list.Remove(gObj);
            }

            _displayToTag.Remove(gObj);
        }

        public void Off(TagBase tag)
        {
            if (_tagToDisplay.TryGetValue(tag, out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var gObj = list[i];
                    if (gObj == null)
                    {
                        list.RemoveAt(i);
                        continue;
                    }

                    if (gObj.isDisposed)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        gObj.visible = false;
                    }

                    this._displayToTag.Remove(gObj);
                }

                _tagToDisplay.Remove(tag);
            }

            if (_tagToText.TryGetValue(tag, out var texts))
            {
                for (int i = texts.Count - 1; i >= 0; i--)
                {
                    var text = texts[i];
                    if (text == null)
                    {
                        texts.RemoveAt(i);
                        continue;
                    }

                    if (text.isDisposed)
                    {
                        texts.RemoveAt(i);
                    }
                    else
                    {
                        text.visible = false;
                    }

                    this._textToTag.Remove(text);
                }

                _tagToText.Remove(tag);
            }
        }

        public void UpdateStatusByTag(TagBase tag)
        {
            if (!_tagToDisplay.TryGetValue(tag, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var obj = list[i];
                if (obj == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (obj.isDisposed)
                {
                    _displayToTag.Remove(obj);
                    list.RemoveAt(i);
                    continue;
                }

                obj.visible = tag.IsRed();
            }
        }

        public void UpdateNumByTag(TagBase tag)
        {
            if (!_tagToText.TryGetValue(tag, out var list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var text = list[i];
                if (text == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (text.isDisposed)
                {
                    list.RemoveAt(i);
                    _textToTag.Remove(text);
                }
                else
                {
                    text.visible = tag.IsRed();
                    if (text.visible) text.text = tag.RedNum().ToString();
                }
            }
        }
    }
}