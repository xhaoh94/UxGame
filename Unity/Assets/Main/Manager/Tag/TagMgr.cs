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
            }

            public Type type { get; }
        }

        private readonly Dictionary<Type, TagBase> _keyToMap = new Dictionary<Type, TagBase>();
        private readonly HashSet<Type> _keyToType = new HashSet<Type>();

        private Dictionary<GObject, TagBase> _displayToTag;
        private Dictionary<TagBase, List<GObject>> _tagToDisplay;


        private Dictionary<GTextField, TagBase> _textToTag;
        private Dictionary<TagBase, List<GTextField>> _tagToText;

        public void Add(List<TagParse> tags)
        {
            foreach (var tag in tags)
            {
                var tagId = tag.type.FullName.ToHash();
                if (_keyToType.Contains(tag.type))
                {
                    Log.Error("重复注册单例红点:{1}", tag.type.FullName);
                    continue;
                }
                _keyToType.Add(tag.type);
            }
        }

        public void Release()
        {
            foreach (var kv in _keyToMap)
            {
                kv.Value.Release();
            }

            _keyToMap.Clear();
        }
        /// <summary>
        /// 是否红点，这个只适用单例类型判断，动态创建的红点需要通过GetTag获得单例红点后，通过Find获取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool IsRed<T>() where T : TagBase
        {
            return IsRed(typeof(T));
        }
        /// <summary>
        /// 是否红点，这个只适用单例类型判断，动态创建的红点需要通过GetTag获得单例红点后，通过Find获取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool IsRed(Type type)
        {
            var tag = GetTag(type);
            return tag != null && tag.IsRed();
        }
        /// <summary>
        /// 获取单例红点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetTag<T>() where T : TagBase
        {
            return _GetTag<T>(typeof(T));
        }
        /// <summary>
        /// 获取单例红点
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TagBase GetTag(Type type)
        {
            return _GetTag<TagBase>(type);
        }

        T _GetTag<T>(Type type) where T : TagBase
        {
            if (_keyToMap.TryGetValue(type, out var tag)) return (T)tag;
            if (!_keyToType.Contains(type))
            {
                return null;
            }
            tag = (TagBase)Pool.Get(type);
            tag.Init(type);
            _keyToMap.Add(type, tag);
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
            if (gObj == null) return;
            var tag = GetTag<T>();
            On(tag, gObj);
        }

        public void On<T>(GTextField text) where T : TagBase
        {
            if (text == null) return;
            var tag = GetTag<T>();
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

        public void ___UpdateStatusByTag(TagBase tag)
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

        public void ___UpdateNumByTag(TagBase tag)
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