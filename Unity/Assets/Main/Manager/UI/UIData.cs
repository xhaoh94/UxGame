#if UNITY_EDITOR
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    //如果有动态生成的界面 可以继承此接口后 通过UIMgr注册即可
    public interface IUIData
    {
        int ID { get; }
#if UNITY_EDITOR
        string IDStr { get; }
#endif
        Type CType { get; }
        string[] Pkgs { get; }
        string[] Lazyloads { get; }
        List<int> Children { get; }
        IUITabData TabData { get; }

        int GetBottomID();
        int GetTopID();
    }

    public interface IUITabData
    {
        /// <summary>
        /// 父界面ID
        /// </summary>
        int PID { get; }
#if UNITY_EDITOR
        string PIDStr { get; }
#endif
        string Title { get; }
        int TagId { get; }
    }

    public class UIData : IUIData
    {
        public UIData(int id, Type type, string[] pkgs, string[] lazyloads, IUITabData tabData = null)
        {
            ID = id;
            CType = type;
            Pkgs = pkgs ?? new string[] { };
            Lazyloads = lazyloads ?? new string[] { };
            TabData = tabData;
            Children = new List<int>();
        }
        public virtual int ID { get; }
#if UNITY_EDITOR
        public string IDStr => $"{CType.FullName}_{ID}";
#endif
        public virtual Type CType { get; }
        public virtual string[] Pkgs { get; }
        public virtual string[] Lazyloads { get; }
        public virtual List<int> Children { get; }
        public virtual IUITabData TabData { get; }

        public virtual int GetBottomID()
        {
            IUIData data = this;
            while (data != null)
            {
                if (data.TabData == null) break;
                if (data.TabData.PID == 0) break;
                data = UIMgr.Ins.GetUIData(data.TabData.PID);
            }

            return data == null ? ID : data.ID;
        }

        public virtual int GetTopID()
        {
            IUIData data = this;
            while (true)
            {
                if (data.Children == null || data.Children.Count == 0) break;
                bool first = true;
                foreach (var child in data.Children)
                {
                    var temData = UIMgr.Ins.GetUIData(child);
                    if (temData == null) continue;
                    if (first)
                    {
                        first = false;
                        data = temData;
                        continue;
                    }

                    if (temData.TabData == null) continue;
                    if (temData.TabData.TagId == 0) continue;
                    if (!TagMgr.Ins.Check(temData.TabData.TagId)) continue;
                    data = temData;
                    break;
                }
            }

            return data.ID;
        }
    }

    public sealed class UITabData : IUITabData
    {
        public UITabData(int pId, string title, int tagId = 0)
        {
            PID = pId;
            Title = title;
            TagId = tagId;
        }
        public int PID { get; }
        public string PIDStr
        {
            get
            {
                var data = UIMgr.Ins.GetUIData(PID);
                if (data != null)
                {
                    return data.IDStr;
                }
                return PID.ToString();
            }
        }
        public int TagId { get; }
        public string Title { get; }
    }
}