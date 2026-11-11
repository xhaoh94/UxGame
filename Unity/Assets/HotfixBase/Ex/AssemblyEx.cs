using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ux
{
    public static class AssemblyEx
    {    
        public static void Initialize(this Assembly assembly)
        {
            Log.Debug("{0} Initialize", assembly.FullName);

            var types = GetTypes(assembly);
            if (types.Length == 0) return;

            // 注册项通常只占类型总数的百分之几，按小容量初始化即可，
            // 主要是避免 List 在完全没命中时也反复扩容。
            var mols = new List<ModuleMgr.ModuleParse>();
            var tags = new List<TagMgr.TagParse>();
            var uis = new List<UIMgr.UIParse>();
            var conditions = new List<ConditionMgr.ConditionParse>();

            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var attrs = type.GetCustomAttributes(false);
                for (int k = 0; k < attrs.Length; k++)
                {
                    switch (attrs[k])
                    {
                        case ModuleAttribute module:
                            mols.Add(new ModuleMgr.ModuleParse(type, module.priority));
                            break;
                        case TagAttribute:
                            tags.Add(new TagMgr.TagParse(type));
                            break;
                        case UIAttribute ui:
                            uis.Add(new UIMgr.UIParse(type, ui.id, CreateTabData(ui.parent)));
                            break;
                        case ConditionAttribute condition:
                            conditions.Add(new ConditionMgr.ConditionParse(type, condition.conditionID));
                            break;
                    }
                }
            }

            ModuleMgr.Ins.Add(mols);
            TagMgr.Ins.Add(tags);
            UIMgr.Ins.Add(uis);
            ConditionMgr.Ins.Add(conditions);
        }

        /// <summary>
        /// 把「父界面类型」这个声明转成运行时需要的父界面 ID。
        /// 放在这里而不是特性构造函数里，特性就成了纯声明：
        /// 读元数据（编辑器工具、生成器）不会触发单例创建。
        /// </summary>
        private static IUITabData CreateTabData(Type parent)
        {
            return parent == null ? null : new UITabData(UIMgr.Ins.GetTypeId(parent));
        }

        /// <summary>
        /// 单个类型加载失败时不要中断整个注册流程：取能加载的部分继续。
        /// </summary>
        private static Type[] GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                var types = new List<Type>(e.Types.Length);
                for (int i = 0; i < e.Types.Length; i++)
                {
                    if (e.Types[i] != null) types.Add(e.Types[i]);
                }
                Log.Error("{0} 有 {1} 个类型加载失败，已跳过", assembly.FullName, e.Types.Length - types.Count);
                return types.ToArray();
            }
        }
    }
}
