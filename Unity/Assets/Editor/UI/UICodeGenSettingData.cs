using FairyGUI;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UI.Editor
{
    [Serializable]
    public class PkgData
    {
        [HideInInspector]
        public string name;
        [LabelText("@name")]
        public List<ComponentData> components;

        public PkgData(string name)
        {
            this.name = name;
            this.components = new List<ComponentData>();
        }
        public void SetComponentData(ComponentData data)
        {
            var index = components.FindIndex(x => x.resName == data.resName);
            if (index >= 0)
            {
                components[index] = data;
            }
            else
            {
                components.Add(data);
            }
        }
        public ComponentData GetComponentData(string resName)
        {
            if (components == null) return null;
            var cData = components.Find(x => x.resName == resName);
            return cData;
        }
    }
    [Serializable]
    public class ComponentData
    {
        [HideInInspector]
        public string ID;

        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("使用全局配置")]
        public bool useGlobal;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("生成路径")]
        [HideIf("@useGlobal")]
        public string path;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("忽略没有命名组件")]
        [HideIf("@useGlobal")]
        public bool ingoreDefault;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("包名")]
        public string pkgName;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("组件名")]
        public string resName;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("命名空间")]
        [HideIf("@useGlobal")]
        public string ns;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("生成类名")]
        public string cls;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("继承类型")]
        public string ext;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("是否导出")]
        public bool isExport = true;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("Tab面板列表")]
        [ShowIf("@IsTabFrame")]
        public string gList;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("Tab面板容器")]
        [ShowIf("@IsTabFrame")]
        public string tabContent;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("Tab面板关闭按钮")]
        [ShowIf("@IsTabFrame")]
        public string btnClose;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("Dialog标题")]
        [ShowIf("@IsDialog")]
        public string dialogTitle;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("Dialog文本")]
        [ShowIf("@IsDialog")]
        public string dialogContent;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("Dialog关闭按钮")]
        [ShowIf("@IsDialog")]
        public string dialogBtnClose;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("Dialog按钮1")]
        [ShowIf("@IsDialog")]
        public string dialogBtn1;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("Dialog按钮2")]
        [ShowIf("@IsDialog")]
        public string dialogBtn2;
        [FoldoutGroup("@resName", expanded: false)]
        [LabelText("Dialog控制器")]
        [ShowIf("@IsDialog")]
        public string dialogController;   

        [HideInInspector]
        public List<UIMemberData> members = new List<UIMemberData>();
        public string GetPath()
        {
            return useGlobal ? UICodeGenSettingData.CodeGenPath : path;
        }
        public string GetNs()
        {
            return useGlobal ? UICodeGenSettingData.DefaultNs : ns;
        }
        public bool GetIngoreDefault()
        {
            return useGlobal ? UICodeGenSettingData.IngoreDefault : ingoreDefault;
        }
        public bool IsExNone
        {
            get
            {
                return ext == $"{UIExtends.None}";
            }
        }
        public bool IsTabFrame
        {
            get
            {
                return ext == $"{UIExtends.Component}/{UIExtendComponent.TabFrame}";
            }
        }
        public bool IsDialog
        {
            get
            {
                return ext == $"{UIExtends.Panel}/{UIExtendPanel.Dialog}";
            }
        }
        public object Extend
        {
            get
            {
                if (!UIEditorTools.GetExtObject(ext, out var obj))
                {
                    Log.Error("找不到对应的类型");
                    return null;
                }
                return obj;
            }
        }
        public List<UIMemberData> GetMembers()
        {
            var ingoreDefault = GetIngoreDefault();
            var r = new List<UIMemberData>();
            foreach (var member in members)
            {
                if (ingoreDefault && member.IsDefalutName())
                {
                    continue;
                }
                r.Add(member);
            }
            return r;
        }

        public void CheckMembers(FairyGUI.GComponent com)
        {
            var comData = this;
            List<UIMemberData> dels = members.ToList();
            for (var i = 0; i < com.numChildren; i++)
            {
                var child = com.GetChildAt(i);
                if (com is GButton gbtn)
                {
                    if (child.name == "title")
                        continue;
                    if (child.name == "icon")
                        continue;
                }

                var member = comData.members.Find(x => x.name == child.name);
                if (member == null)
                {
                    member = new UIMemberData();
                    if (child is GButton)
                    {
                        member.evtType = "单击";
                    }
                    else if (child is GList)
                    {
                        member.evtType = "列表点击";
                    }
                    comData.members.Add(member);
                }
                else
                {
                    dels.Remove(member);
                }
                member.comData = comData;
                member.name = child.name;
                member.index = i;
                member.defaultType = child.GetType().Name;

                if (child.packageItem != null && child is GComponent)
                {
                    if (child.packageItem.exported ||
                        UIEditorTools.CustomTypeList.Contains(child.packageItem.objectType))
                    {
                        var set = UICodeGenSettingData.GetComponentData(child.asCom);
                        if (set == null || !set.IsExNone)
                        {
                            member.customType = set != null ? set.cls : child.packageItem.name;
                            member.pkg = child.packageItem.owner.name;
                            member.res = child.packageItem.name;
                            continue;
                        }
                    }
                }
                member.customType = child.GetType().Name;
            }

            for (var i = 0; i < com.Controllers.Count; i++)
            {
                var contr = com.GetControllerAt(i);
                if (com is GButton gbtn)
                {
                    if (contr.name == "button")
                        continue;
                }
                var member = comData.members.Find(x => x.name == contr.name);
                if (member == null)
                {
                    member = new UIMemberData();
                    comData.members.Add(member);
                }
                else
                {
                    dels.Remove(member);
                }
                member.comData = comData;
                member.name = contr.name;
                member.index = i;
                member.defaultType = contr.GetType().Name;
                member.customType = contr.GetType().Name;
            }

            for (var i = 0; i < com.Transitions.Count; i++)
            {
                var trans = com.GetTransitionAt(i);
                var member = comData.members.Find(x => x.name == trans.name);
                if (member == null)
                {
                    member = new UIMemberData();
                    comData.members.Add(member);
                }
                else
                {
                    dels.Remove(member);
                }
                member.comData = comData;
                member.name = trans.name;
                member.index = i;
                member.defaultType = trans.GetType().Name;
                member.customType = trans.GetType().Name;
            }

            foreach (var del in dels)
            {
                comData.members.Remove(del);
            }
        }
    }
    [Serializable]
    public class UIMemberData
    {
        public struct MemberEvtDouble
        {
            public int dCnt;
            public float dGapTime;
        }
        public struct MemberEvtLong
        {
            public float lFirst;
            public float lGapTime;
            public int lCnt;
            public float lRadius;
        }
        public int index;
        public string name;
        public string defaultType;
        public string customType;
        public string pkg;
        public string res;
        /// <summary>
        /// 是否生成变量
        /// </summary>
        public bool isCreateVar = true;
        /// <summary>
        /// 是否生成实例
        /// </summary>
        public bool isCreateIns = true;
        /// <summary>
        /// 事件类型
        /// </summary>
        public string evtType = "无";
        public string evtParam = string.Empty;

        [HideInInspector]
        [NonSerialized]
        public ComponentData comData;
        public bool IsDefalutName()
        {
            switch (defaultType)
            {
                case nameof(Controller):
                    return Regex.IsMatch(name, "(^c)([0-9]+)$");
                case nameof(Transition):
                    return Regex.IsMatch(name, "(^t)([0-9]+)$");
            }
            return Regex.IsMatch(name, "(^n)([0-9]+)$");
        }
        public bool IsTabContent()
        {
            var com = UIEditorTools.GetOrAddGComBy(pkg, res);
            var data = UICodeGenSettingData.GetOrAddComponentData(com);
            var ext = data.Extend;
            if (ext is UIExtendComponent.TabFrame)
            {
                var members = data.GetMembers();
                var cnt = 2;
                foreach (var member in members)
                {
                    if (!member.isCreateVar) continue;
                    if (member.defaultType == nameof(GList) &&
                        member.name == data.gList) cnt--;
                    else if (member.defaultType == nameof(GComponent) &&
                        member.name == data.tabContent) cnt--;
                }
                return cnt <= 0;
            }
            return false;
        }

    }

    [CreateAssetMenu(fileName = "UICodeGenSettingData", menuName = "Ux/UI/Create CodeGenSettings")]
    public class UICodeGenSettingData : ScriptableObject
    {
        [LabelText("命名空间")]
        public string defaultNs = "Ux.UI";
        [LabelText("生成路径")]
        public string path = "Assets/Hotfix/Manager/UI/CodeGen";
        [LabelText("是否忽略没有命名组件")]
        public bool ingoreDefault = true;
        [LabelText("UI包")]
        public List<PkgData> pkgs;


        static UICodeGenSettingData ins;
        public static HashSet<string> freshComDataList = new HashSet<string>();

        #region 加载
        public static bool IsDirty { set; get; } = false;
        public static void Load()
        {
            if (ins == null)
            {
                IsDirty = false;
                ins = SettingTools.GetSingletonAssets<UICodeGenSettingData>("Assets/Editor/UI");
                freshComDataList.Clear();
            }
        }
        public static void Save()
        {
            if (IsDirty && ins != null)
            {
                EditorUtility.SetDirty(ins);
                AssetDatabase.SaveAssets();
                Log.Debug("Save UICodeGenSettingData ok");
            }
            ins = null;
        }
        public static void Check(HashSet<string> pkgNames)
        {
            if (ins == null) return;
            for (int i = ins.pkgs.Count - 1; i >= 0; i--)
            {
                var pkg = ins.pkgs[i];
                if (!pkgNames.Contains(pkg.name))
                {
                    ins.pkgs.RemoveAt(i);
                }
            }
        }
        #endregion
        public static string DefaultNs
        {
            get
            {
                if (ins == null)
                {
                    return string.Empty;
                }
                return ins.defaultNs;
            }
            set
            {
                if (ins == null)
                {
                    return;
                }
                ins.defaultNs = value;
                IsDirty = true;
            }
        }

        public static bool IngoreDefault
        {
            get
            {
                if (ins == null)
                {
                    return false;
                }
                return ins.ingoreDefault;
            }
            set
            {
                if (ins == null)
                {
                    return;
                }
                ins.ingoreDefault = value;
                IsDirty = true;
            }
        }
        public static string CodeGenPath
        {
            get
            {
                if (ins == null)
                {
                    return string.Empty;
                }
                return ins.path;
            }
            set
            {
                if (ins == null)
                {
                    return;
                }
                ins.path = value;
                IsDirty = true;
            }
        }
        public static void SetComponentData(ComponentData setData)
        {
            if (ins == null)
            {
                return;
            }
            if (ins.pkgs == null) ins.pkgs = new List<PkgData>();
            string pkg = setData.pkgName;
            string res = setData.resName;
            var pkgData = ins.pkgs.Find(x => x.name == pkg);
            if (pkgData == null)
            {
                pkgData = new PkgData(pkg);
                ins.pkgs.Add(pkgData);
            }
            pkgData.SetComponentData(setData);
            IsDirty = true;
        }
        public static ComponentData GetComponentData(FairyGUI.GComponent com)
        {
            if (com == null) return null;
            if (com.packageItem == null) return null;
            string pkg = com.packageItem.owner.name;
            string res = com.packageItem.name;

            if (ins.pkgs == null) ins.pkgs = new List<PkgData>();
            var pkgData = ins.pkgs.Find(x => x.name == pkg);
            if (pkgData != null)
            {
                var comData = pkgData.GetComponentData(res);
                if (comData != null && !UICodeGenSettingData.freshComDataList.Contains(comData.ID))
                {
                    UICodeGenSettingData.freshComDataList.Add(comData.ID);
                    comData.CheckMembers(com);
                }
                return comData;
            }
            return null;
        }
        public static ComponentData GetOrAddComponentData(FairyGUI.GComponent com)
        {
            if (com == null) return null;
            if (com.packageItem == null) return null;
            var comData = GetComponentData(com);
            if (comData == null)
            {
                comData = CreateDefaultData(com);
                UICodeGenSettingData.freshComDataList.Add(comData.ID);
                comData.CheckMembers(com);
            }
            return comData;
        }
        static ComponentData CreateDefaultData(FairyGUI.GComponent com)
        {
            string pkg = com.packageItem.owner.name;
            string res = com.packageItem.name;

            var data = new ComponentData();
            data.ID = Guid.NewGuid().ToString();
            data.useGlobal = true;
            data.path = CodeGenPath;
            data.ingoreDefault = IngoreDefault;
            data.pkgName = pkg;
            data.resName = res;
            data.ns = DefaultNs;
            data.cls = com.packageItem.name;
            data.ext = UIEditorTools.CheckExt(com);
            data.isExport = true;
            data.gList = string.Empty;
            data.tabContent = string.Empty;
            data.btnClose = string.Empty;
            data.dialogTitle= string.Empty;
            data.dialogContent = string.Empty;
            data.dialogBtnClose = string.Empty;
            data.dialogBtn1 = string.Empty;
            data.dialogBtn2 = string.Empty;
            data.dialogController = string.Empty;
            return data;
        }

    }
}

