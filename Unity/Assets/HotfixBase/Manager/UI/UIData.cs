using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    //如果有动态生成的界面 可以继承此接口后 通过UIMgr注册即可
    public interface IUIData
    {
        int ID { get; }
        string Name { get; }
        Type CType { get; }
        string[] Pkgs { get; }
        string[] Lazyloads { get; }
        List<int> Children { get; }
        IUITabData TabData { get; }

        /// <summary>
        /// 获取父类面板链
        /// </summary>
        /// <returns></returns>
        List<int> GetParentIDs();
        /// <summary>
        /// 获取最底下的父类面板
        /// </summary>
        /// <returns></returns>
        int GetParentID();
        /// <summary>
        /// 获取最上层的子类面板
        /// </summary>
        /// <returns></returns>
        int GetChildID();
    }

    public interface IUITabData
    {
        /// <summary>
        /// 父界面ID
        /// </summary>
        int PID { get; }
        object Title { get; }
        Type TagType { get; }
#if UNITY_EDITOR
        string PName { get; }
        string TitleStr { get; }
#endif
        void Init(IUIData data);
        bool IsRed();
    }

    public class UIData : IUIData
    {
        public UIData(int id, Type type, IUITabData tabData = null)
        {
            ID = id;
            CType = type;
            TabData = tabData;
            Children = new List<int>();

            var pkgsAttr = type.GetAttribute<PackageAttribute>();
            Pkgs = pkgsAttr?.pkgs;

            var resAttr = type.GetAttribute<LazyloadAttribute>();
            Lazyloads = resAttr?.lazyloads;

            tabData?.Init(this);
        }
        public virtual int ID { get; }
        public string Name => $"{CType.FullName}_{ID}";
        public virtual Type CType { get; }
        public virtual string[] Pkgs { get; }
        public virtual string[] Lazyloads { get; }
        public virtual List<int> Children { get; }
        public virtual IUITabData TabData { get; }

        public virtual List<int> GetParentIDs()
        {
            List<int> ids = null;
            IUIData data = this;
            while (data != null)
            {
                if (data.TabData == null) break;
                if (data.TabData.PID == 0) break;
                ids ??= new List<int>();
                ids.Add(data.TabData.PID);
                data = UIMgr.Ins.GetUIData(data.TabData.PID);
            }
            return ids;
        }
        public virtual int GetParentID()
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
        public virtual int GetChildID()
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
                    if (!temData.TabData.IsRed()) continue;
                    data = temData;
                    break;
                }
            }

            return data.ID;
        }
    }

    public class UITabData : IUITabData
    {
        public UITabData(int pId)
        {
            PID = pId;
        }
        public void Init(IUIData data)
        {
            var type = data.CType;
            if (type != null)
            {
                var tagAttr = type.GetAttribute<BindTagAttribute>();
                if (tagAttr != null)
                {
                    TagType = tagAttr.TagType;
                }
                var titleAttr = type.GetAttribute<TabTitleAttribute>();
                if (titleAttr != null)
                {
                    Title = titleAttr.Title;
                }
            }
        }
        public int PID { get; }
#if UNITY_EDITOR
        public string PName
        {
            get
            {
                if (PID > 0)
                {
                    var data = UIMgr.Ins.GetUIData(PID);
                    if (data != null)
                    {
                        return data.Name;
                    }
                }
                return PID.ToString();
            }
        }
        public string TitleStr
        {
            get
            {
                if (Title is string) return Title.ToString();
                if (Title == default) return string.Empty;
                return Newtonsoft.Json.JsonConvert.SerializeObject(Title);
            }
        }
#endif
        public Type TagType { get; protected set; }
        public object Title { get; protected set; }

        public virtual bool IsRed()
        {
            if (TagType != null)
            {
                return TagMgr.Ins.IsRed(TagType);
            }
            return false;
        }
    }

    public partial class UIMgr
    {
        private readonly Dictionary<int, IUIData> _idUIData = new Dictionary<int, IUIData>();

        //动态创建的UI数据
        private readonly HashSet<int> _dymUIData = new HashSet<int>();

        #region UIData

        /// <summary>
        /// 添加UIData，一般用于动态创建UI界面
        /// </summary>
        /// <param name="data"></param>
        public void AddUIData(IUIData data)
        {
            if (IsHasUIData(data.ID))
            {
                Log.Error("重复注册UI面板:{0}", data.Name);
                return;
            }
            _idUIData.Add(data.ID, data);
            _dymUIData.Add(data.ID);
#if UNITY_EDITOR
            __Debugger_UI_Event();
#endif
        }

        /// <summary>
        /// 移除UIData，一般用于动态创建界面的销毁
        /// </summary>
        /// <param name="id"></param>
        public void RemoveUIData(int id)
        {
            if (!_idUIData.Remove(id)) return;
            _dymUIData.Remove(id);
#if UNITY_EDITOR
            __Debugger_UI_Event();
#endif
        }

        /// <summary>
        /// 获取已注册的UIData
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IUIData GetUIData(int id)
        {
            if (_idUIData.TryGetValue(id, out var data))
            {
                return data;
            }
#if UNITY_EDITOR
            if (Ins._idTypeName.TryGetValue(id, out var tName))
            {
                Log.Error($"获取不到UIData[{tName}],请检查是否有注册UI");
            }
            else
            {
                Log.Error($"获取不到UIData[{id}],请检查是否有注册UI");
            }
#endif
            return null;
        }

        public bool IsHasUIData(int id)
        {
            return _idUIData.ContainsKey(id);
        }
        #endregion
    }
}