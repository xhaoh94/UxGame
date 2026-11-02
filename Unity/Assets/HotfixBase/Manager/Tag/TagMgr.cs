using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Ux
{
    public class TagMgr : Singleton<TagMgr>
    {
        public struct TagParse
        {
            public TagParse(Type type) { this.type = type; }
            public Type type { get; }
        }

        private readonly Dictionary<Type, TagBase> _keyToMap = new();
        private readonly HashSet<Type> _keyToType = new();
        private Dictionary<GameObject, TagBase> _displayToTag;
        private Dictionary<TagBase, List<GameObject>> _tagToDisplay;
        private Dictionary<TMP_Text, TagBase> _textToTag;
        private Dictionary<TagBase, List<TMP_Text>> _tagToText;

        public void Add(List<TagParse> tags)
        {
            foreach (var tag in tags)
            {
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
            foreach (var kv in _keyToMap) kv.Value.Release();
            _keyToMap.Clear();
            _displayToTag?.Clear();
            _tagToDisplay?.Clear();
            _textToTag?.Clear();
            _tagToText?.Clear();
        }

        public bool IsRed<T>() where T : TagBase => IsRed(typeof(T));
        public bool IsRed(Type type) => GetTag(type)?.IsRed() == true;
        public T GetTag<T>() where T : TagBase => GetTag(typeof(T)) as T;
        public TagBase GetTag(Type type)
        {
            if (_keyToMap.TryGetValue(type, out var tag)) return tag;
            if (!_keyToType.Contains(type)) return null;
            tag = Pool.Get<TagBase>(type);
            tag.Init(type);
            _keyToMap.Add(type, tag);
            return tag;
        }

        public void On(TagBase tag, GameObject target)
        {
            if (tag == null || target == null) return;
            _displayToTag ??= new();
            _tagToDisplay ??= new();
            if (_displayToTag.TryGetValue(target, out var old) && old == tag) return;
            Off(target);
            if (!_tagToDisplay.TryGetValue(tag, out var list))
                _tagToDisplay[tag] = list = new List<GameObject>();
            list.Add(target);
            target.SetActive(tag.IsRed());
            _displayToTag[target] = tag;
        }

        public void On(TagBase tag, TMP_Text text)
        {
            if (tag == null || text == null) return;
            _textToTag ??= new();
            _tagToText ??= new();
            if (_textToTag.TryGetValue(text, out var old) && old == tag) return;
            Off(text);
            if (!_tagToText.TryGetValue(tag, out var list))
                _tagToText[tag] = list = new List<TMP_Text>();
            list.Add(text);
            text.gameObject.SetActive(tag.IsRed());
            if (tag.IsRed()) text.text = tag.RedNum().ToString();
            _textToTag[text] = tag;
        }

        public void On<T>(GameObject target) where T : TagBase => On(GetTag<T>(), target);
        public void On<T>(TMP_Text text) where T : TagBase => On(GetTag<T>(), text);

        public void Off(GameObject target)
        {
            if (target == null || _displayToTag == null || !_displayToTag.TryGetValue(target, out var tag)) return;
            if (_tagToDisplay.TryGetValue(tag, out var list)) list.Remove(target);
            target.SetActive(false);
            _displayToTag.Remove(target);
        }

        public void Off(TMP_Text text)
        {
            if (text == null || _textToTag == null || !_textToTag.TryGetValue(text, out var tag)) return;
            if (_tagToText.TryGetValue(tag, out var list)) list.Remove(text);
            text.gameObject.SetActive(false);
            _textToTag.Remove(text);
        }

        public void Off(TagBase tag)
        {
            if (_tagToDisplay?.TryGetValue(tag, out var displays) == true)
            {
                foreach (var target in displays) if (target != null) target.SetActive(false);
                _tagToDisplay.Remove(tag);
            }
            if (_tagToText?.TryGetValue(tag, out var texts) == true)
            {
                foreach (var text in texts) if (text != null) text.gameObject.SetActive(false);
                _tagToText.Remove(tag);
            }
        }

        public void ___UpdateStatusByTag(TagBase tag)
        {
            if (_tagToDisplay?.TryGetValue(tag, out var list) != true) return;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null) list.RemoveAt(i);
                else list[i].SetActive(tag.IsRed());
            }
        }

        public void ___UpdateNumByTag(TagBase tag)
        {
            if (_tagToText?.TryGetValue(tag, out var list) != true) return;
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var text = list[i];
                if (text == null) list.RemoveAt(i);
                else
                {
                    text.gameObject.SetActive(tag.IsRed());
                    if (tag.IsRed()) text.text = tag.RedNum().ToString();
                }
            }
        }
    }
}
