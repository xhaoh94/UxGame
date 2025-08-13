using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Build.UI
{
    [Serializable]
    public class PkgData
    {
        [HideInInspector]
        public string name;
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
        [Serializable]
        public struct CustomData
        {
            public string Key;
            public string Type;
            public string Name;
            public CustomData(string key, string type, string name)
            {
                Key = key;
                Type = type;
                Name = name;
            }

            public bool IsEquals(UIMemberData data)
            {
                return data.name == Name && (data.defaultType == Type || data.customType == Type);
            }
        }
        [HideInInspector]
        public string ID;
        public bool useGlobal;

        public string path;

        public bool ingoreDefault;

        public string pkgName;

        public string resName;

        public string ns;

        public string cls;

        public string ext;

        public bool isExport = true;

        [SerializeField]
        List<CustomData> _tabViewData;
        public List<CustomData> TabViewData
        {
            get
            {
                if (_tabViewData == null || _tabViewData.Count == 0)
                {
                    _tabViewData = new List<CustomData>()
                    {
                        new CustomData("__tabContent","GComponent",string.Empty),
                        new CustomData("__listTab","UIList",string.Empty),
                        new CustomData("__btnClose","UIButton",string.Empty)
                    };
                }
                return _tabViewData;
            }
        }

        [SerializeField]
        List<CustomData> _messageBoxData;
        public List<CustomData> MessageBoxData
        {
            get
            {
                if (_messageBoxData == null || _messageBoxData.Count == 0)
                {
                    _messageBoxData = new List<CustomData>()
                    {
                       new CustomData("__txtTitle","GTextField",string.Empty),
                       new CustomData("__txtContent","GTextField",string.Empty),
                       new CustomData("__btnClose","UIButton",string.Empty),
                       new CustomData("__btn1","UIButton",string.Empty),
                       new CustomData("__btn2","UIButton",string.Empty),
                       new CustomData("__checkbox","UIButton",string.Empty),
                       //new CustomData("__controller","Controller",string.Empty),
                    };
                }
                return _messageBoxData;
            }
        }

        [SerializeField]
        List<CustomData> _tipData;
        public List<CustomData> TipData
        {
            get
            {
                if (_tipData == null || _tipData.Count == 0)
                {
                    _tipData = new List<CustomData>()
                    {
                       new CustomData("__txtContent","GTextField",string.Empty),
                       new CustomData("__transition","Transition",string.Empty),
                    };
                }
                return _tipData;
            }
        }

        [SerializeField]
        List<CustomData> _modelData;
        public List<CustomData> ModelData
        {
            get
            {
                if (_modelData == null || _modelData.Count == 0)
                {
                    _modelData = new List<CustomData>()
                    {
                       new CustomData("__container","GGraph",string.Empty),
                    };
                }
                return _modelData;
            }
        }

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
        public bool IsExFGUI
        {
            get
            {
                return ext.Contains($"{UIExtends.FGUI}");
            }
        }
        public bool IsTabFrame
        {
            get
            {
                return ext == $"{UIExtends.Component}/{UIExtendComponent.TabFrame}";
            }
        }
        public bool IsMessageBox
        {
            get
            {
                return ext == $"{UIExtends.Panel}/{UIExtendPanel.MessageBox}";
            }
        }
        public bool IsTip
        {
            get
            {
                return ext == $"{UIExtends.Panel}/{UIExtendPanel.Tip}";
            }
        }
        public bool IsModel
        {
            get
            {
                return ext == $"{UIExtends.Component}/{UIExtendComponent.UIModel}" ||
                    ext == $"{UIExtends.Component}/{UIExtendComponent.RTModel}";
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

                var b = UIEditorTools.GTypes.TryGetValue(child.GetType(), out var temName);
                if (!b) b = child.packageItem != null && child.packageItem.exported;
                if (b)
                {
                    if (child.packageItem != null)
                    {
                        var set = UICodeGenSettingData.GetComponentData(child.asCom);
                        if (set == null || !set.IsExNone)
                        {
                            member.pkg = child.packageItem.owner.name;
                            member.res = child.packageItem.name;
                            if (set == null)
                            {
                                member.customType = string.IsNullOrEmpty(temName) ? child.packageItem.name : temName;
                                continue;
                            }

                            if (!set.IsExNone)
                            {
                                member.customType = set.isExport || string.IsNullOrEmpty(temName) ? set.cls : temName;
                                continue;
                            }
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(temName))
                        {
                            member.customType = temName;
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
        public bool IsTabFrame()
        {
            var com = UIEditorTools.GetOrAddGComBy(pkg, res);
            var data = UICodeGenSettingData.GetOrAddComponentData(com);
            if (data == null) return false;
            var ext = data.Extend;
            if (ext is UIExtendComponent.TabFrame)
            {
                return true;
            }
            return false;
        }

    }

    [CreateAssetMenu(fileName = "UICodeGenSettingData", menuName = "Ux/UI/Create CodeGenSettings")]
    public class UICodeGenSettingData : ScriptableObject
    {
        public string defaultNs = "Ux.UI";
        public string path = "Assets/Hotfix/CodeGen/UI";
        public bool ingoreDefault = true;
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
                ins = SettingTools.GetSingletonAssets<UICodeGenSettingData>("Assets/Settings/Build/UI");
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
            data.ID = $"{pkg}@{res}";
            data.useGlobal = true;
            data.path = CodeGenPath;
            data.ingoreDefault = IngoreDefault;
            data.pkgName = pkg;
            data.resName = res;
            data.ns = DefaultNs;
            data.cls = com.packageItem.name;
            data.ext = UIEditorTools.CheckExt(com);
            data.isExport = true;
            if (UIEditorTools.OTypes.Contains(com.packageItem.objectType))
            {
                if (com.packageItem.exported && data.IsExFGUI)
                {
                    data.isExport = false;
                }
            }
            return data;
        }

    }
}

