using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ux
{
    public static class AssemblyEx
    {
        public static void Initialize(this Assembly assembly)
        {
            Log.Debug($"{assembly.FullName} Initialize");
            var mols = new List<ModuleMgr.ModuleParse>();
            var tags = new List<TagMgr.TagParse>();
            var uis = new List<UIMgr.UIParse>();
            var conditions = new List<ConditionMgr.ConditionParse>();
            foreach (Type type in assembly.GetTypes())
            {
                var module = type.GetAttribute<ModuleAttribute>();
                if (module != null)
                {
                    mols.Add(new ModuleMgr.ModuleParse(type, module.priority));
                }

                var tag = type.GetAttribute<TagAttribute>();
                if (tag != null)
                {
                    tags.Add(new TagMgr.TagParse(type));
                }

                var ui = type.GetAttribute<UIAttribute>();
                if (ui != null)
                {
                    uis.Add(new UIMgr.UIParse(type, ui.id, ui.tabData));
                }

                var condition = type.GetAttribute<ConditionAttribute>();
                if (condition != null)
                {
                    conditions.Add(new ConditionMgr.ConditionParse(type, condition.conditionID));
                }
            }

            ModuleMgr.Ins.Add(mols);
            TagMgr.Ins.Add(tags);
            UIMgr.Ins.Add(uis);
            ConditionMgr.Ins.Add(conditions);
        }
    }
}