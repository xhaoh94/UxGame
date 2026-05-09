using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// UI数据接口
    /// 如果有动态生成的界面，可以继承此接口后通过UIMgr注册即可
    /// 这个接口定义了UI面板的基本数据和关系信息
    /// </summary>
    public interface IUIData
    {
        /// <summary>
        /// UI的唯一标识符ID
        /// </summary>
        int ID { get; }
        
        /// <summary>
        /// UI的名称，通常是类型全名加上ID
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// UI的类型
        /// </summary>
        Type CType { get; }
        
        /// <summary>
        /// UI依赖的资源包名数组
        /// </summary>
        string[] Pkgs { get; }
        
        /// <summary>
        /// UI懒加载的资源标签数组
        /// </summary>
        string[] Lazyloads { get; }
        
        /// <summary>
        /// 子界面ID列表
        /// </summary>
        List<int> Children { get; }
        
        /// <summary>
        /// Tab页数据接口
        /// </summary>
        IUITabData TabData { get; }

        /// <summary>
        /// 获取父类面板链（从当前面板到最顶层父面板的所有父面板ID）
        /// </summary>
        /// <returns>父面板ID列表，按照从直接父级到最高级祖先的顺序排列</returns>
        List<int> GetParentIDs();
        
        /// <summary>
        /// 获取最顶层的根面板ID
        /// </summary>
        /// <returns>如果没有父面板则返回自身ID</returns>
        int GetParentID();

        /// <summary>
        /// 获取父链深度。
        /// 根界面深度为0，直接子界面为1，依次递增。
        /// </summary>
        int GetParentDepth();
        
        /// <summary>
        /// 获取最后的子类面板ID（用于Tab切换场景）
        /// </summary>
        /// <returns>最后层子面板的ID</returns>
        int GetChildID();
    }

    /// <summary>
    /// Tab页数据接口，用于处理Tab页相关的UI数据
    /// </summary>
    public interface IUITabData
    {
        /// <summary>
        /// 父界面ID
        /// </summary>
        int PID { get; }
        
        /// <summary>
        /// Tab页标题，可以是字符串或其他类型的对象
        /// </summary>
        object Title { get; }
        
        /// <summary>
        /// 标签类型，用于红点系统
        /// </summary>
        Type TagType { get; }
        
#if UNITY_EDITOR
        /// <summary>
        /// 父界面名称（仅在编辑器模式下使用）
        /// </summary>
        string PName { get; }
        
        /// <summary>
        /// 标题字符串表示（仅在编辑器模式下使用）
        /// </summary>
        string TitleStr { get; }
#endif
        
        /// <summary>
        /// 初始化Tab数据
        /// </summary>
        /// <param name="data">UI数据</param>
        void Init(IUIData data);
        
        /// <summary>
        /// 检查是否有红点标记
        /// </summary>
        /// <returns>如果有红点返回true，否则返回false</returns>
        bool IsRed();
    }

    /// <summary>
    /// UI数据实现类，包含UI面板的所有基本信息
    /// </summary>
    public class UIData : IUIData
    {
        public UIData(int id, Type type, IUITabData tabData = null)
        {
            ID = id;
            CType = type;
            Name = $"{CType.FullName}_{ID}";
            TabData = tabData;
            Children = new List<int>();

            // 从类型属性中获取资源包信息
            var pkgsAttr = type.GetAttribute<PackageAttribute>();
            Pkgs = pkgsAttr?.pkgs;

            // 从类型属性中获取懒加载资源信息
            var resAttr = type.GetAttribute<LazyloadAttribute>();
            Lazyloads = resAttr?.lazyloads;

            // 初始化Tab数据
            tabData?.Init(this);
        }
        
        /// <summary>
        /// UI的唯一标识符ID（只读属性）
        /// </summary>
        public virtual int ID { get; }
        
        /// <summary>
        /// UI的名称，格式为"类型全名_ID"（只读属性）
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// UI的类型（只读属性）
        /// </summary>
        public virtual Type CType { get; }
        
        /// <summary>
        /// UI依赖的资源包名数组（只读属性）
        /// </summary>
        public virtual string[] Pkgs { get; }
        
        /// <summary>
        /// UI懒加载的资源标签数组（只读属性）
        /// </summary>
        public virtual string[] Lazyloads { get; }
        
        /// <summary>
        /// 子界面ID列表（只读属性）
        /// </summary>
        public virtual List<int> Children { get; }
        
        /// <summary>
        /// Tab页数据接口（只读属性）
        /// </summary>
        public virtual IUITabData TabData { get; }

        /// <summary>
        /// 获取父类面板链（从当前面板到最顶层父面板的所有父面板ID）
        /// 遍历逻辑：从当前面板开始，沿着TabData.PID向上查找，直到找到没有父面板的面板为止
        /// </summary>
        /// <returns>父面板ID列表，按照从直接父级到最高级祖先的顺序排列</returns>
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

        public virtual int GetParentDepth()
        {
            var depth = 0;
            IUIData data = this;
            while (data != null)
            {
                if (data.TabData == null || data.TabData.PID == 0)
                {
                    break;
                }

                depth++;
                data = UIMgr.Ins.GetUIData(data.TabData.PID);
            }

            return depth;
        }
        
        public virtual int GetChildID()
        {
            IUIData data = this;
            while (true)
            {
                if (data.Children == null || data.Children.Count == 0) break;
                var nextData = UIMgr.Ins.GetUIData(data.Children[0]);
                if (nextData == null)
                {
                    break;
                }

                data = nextData;
            }

            return data.ID;
        }
    }

    /// <summary>
    /// Tab页数据实现类，用于处理Tab页相关的UI数据
    /// </summary>
    public class UITabData : IUITabData
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pId">父界面ID</param>
        public UITabData(int pId)
        {
            PID = pId;
        }
        
        /// <summary>
        /// 初始化Tab数据
        /// 从UI类型中提取标签类型和标题信息
        /// </summary>
        /// <param name="data">UI数据</param>
        public void Init(IUIData data)
        {
            var type = data.CType;
            if (type != null)
            {
                // 获取标签类型属性
                var tagAttr = type.GetAttribute<BindTagAttribute>();
                if (tagAttr != null)
                {
                    TagType = tagAttr.TagType;
                }
                
                // 获取标题属性
                var titleAttr = type.GetAttribute<TabTitleAttribute>();
                if (titleAttr != null)
                {
                    Title = titleAttr.Title;
                }
            }
        }
        
        /// <summary>
        /// 父界面ID（只读属性）
        /// </summary>
        public int PID { get; }
        
#if UNITY_EDITOR
        /// <summary>
        /// 父界面名称（仅在编辑器模式下使用）
        /// 如果父界面ID有效，返回父界面的名称，否则返回ID的字符串表示
        /// </summary>
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
        
        /// <summary>
        /// 标题字符串表示（仅在编辑器模式下使用）
        /// 将标题对象转换为字符串表示
        /// </summary>
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
        
        /// <summary>
        /// 标签类型，用于红点系统（受保护的可设置属性）
        /// </summary>
        public Type TagType { get; protected set; }
        
        /// <summary>
        /// Tab页标题，可以是字符串或其他类型的对象（受保护的可设置属性）
        /// </summary>
        public object Title { get; protected set; }

        /// <summary>
        /// 检查是否有红点标记
        /// 通过TagMgr检查指定标签类型是否有红点
        /// </summary>
        /// <returns>如果有红点返回true，否则返回false</returns>
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
        /// <summary>
        /// ID到UIData的映射字典
        /// key: UI的ID
        /// value: UI数据对象
        /// </summary>
        private readonly Dictionary<int, IUIData> _idUIData = new Dictionary<int, IUIData>();

        /// <summary>
        /// 动态创建的UI数据ID集合
        /// 用于跟踪运行时动态生成的UI，以便在释放时正确处理
        /// </summary>
        private readonly HashSet<int> _dymUIData = new HashSet<int>();

        #region UIData 相关方法

        /// <summary>
        /// 添加UIData，一般用于动态创建UI界面
        /// 注意：如果ID已存在，会记录错误日志
        /// </summary>
        /// <param name="data">要添加的UI数据</param>
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
        /// <param name="id">要移除的UI ID</param>
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
        /// 如果找不到对应的UIData，会记录错误日志（仅在编辑器模式下）
        /// </summary>
        /// <param name="id">要获取的UI ID</param>
        /// <returns>对应的UI数据，如果不存在则返回null</returns>
        public IUIData GetUIData(int id)
        {
            if (_idUIData.TryGetValue(id, out var data))
            {
                return data;
            }
#if UNITY_EDITOR
            // 编辑器模式下提供更详细的错误信息
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

        /// <summary>
        /// 检查是否存在指定ID的UIData
        /// </summary>
        /// <param name="id">要检查的UI ID</param>
        /// <returns>如果存在返回true，否则返回false</returns>
        public bool IsHasUIData(int id)
        {
            return _idUIData.ContainsKey(id);
        }
        #endregion
    }
}