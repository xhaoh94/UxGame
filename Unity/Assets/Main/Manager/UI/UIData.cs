#if UNITY_EDITOR
#endif
using System;
using System.Collections.Generic;

namespace Ux
{
    //如果有动态生成的界面 可以继承此接口后 通过UIMgr注册即可
    public interface IUIData
    {
        string ID { get; }
        Type CType { get; }
        string[] Pkgs { get; }
        string[] Lazyloads { get; }
        List<string> Children { get; }
        IUITabData TabData { get; }

        string GetBottomID();
        string GetTopID();
    }

    public interface IUITabData
    {
        /// <summary>
        /// 父界面ID
        /// </summary>
        string PID { get; }

        string Title { get; }
        int TagId { get; }
    }

    public class UIData : IUIData
    {
        public UIData(string id, Type type, string[] pkgs, string[] lazyloads, IUITabData tabData = null)
        {
            ID = id;
            CType = type;
            Pkgs = pkgs ?? new string[] { };
            Lazyloads = lazyloads ?? new string[] { };
            TabData = tabData;
            Children = new List<string>();
        }

        public virtual string ID { get; }
        public virtual Type CType { get; }
        public virtual string[] Pkgs { get; }
        public virtual string[] Lazyloads { get; }
        public virtual List<string> Children { get; }
        public virtual IUITabData TabData { get; }

        public virtual string GetBottomID()
        {
            IUIData data = this;
            while (data != null)
            {
                if (data.TabData == null) break;
                if (string.IsNullOrEmpty(data.TabData.PID)) break;
                data = UIMgr.Instance.GetUIData(data.TabData.PID);
            }

            if (data == null)
            {
                return ID;
            }

            return data.ID;
        }

        public virtual string GetTopID()
        {
            IUIData data = this;
            while (true)
            {
                if (data.Children == null || data.Children.Count == 0) break;
                bool first = true;
                foreach (var child in data.Children)
                {
                    var temData = UIMgr.Instance.GetUIData(child);
                    if (temData == null) continue;
                    if (first)
                    {
                        first = false;
                        data = temData;
                        continue;
                    }

                    if (temData.TabData == null) continue;
                    if (temData.TabData.TagId == 0) continue;
                    if (!TagMgr.Instance.Check(temData.TabData.TagId)) continue;
                    data = temData;
                    break;
                }
            }

            return data.ID;
        }
    }

    public sealed class UITabData : IUITabData
    {
        public UITabData(string pId, string title, int tagId = 0)
        {
            PID = pId;
            Title = title;
            TagId = tagId;
        }

        public string PID { get; }
        public int TagId { get; }
        public string Title { get; }
    }
}