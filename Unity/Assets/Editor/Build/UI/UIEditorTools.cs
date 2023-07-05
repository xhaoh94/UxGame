﻿using FairyGUI;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UI.Editor
{
    public class UIEditorTools
    {
        public static List<ObjectType> CustomTypeList = new List<ObjectType>() {
            ObjectType.Button,
            ObjectType.ProgressBar,
        };
        private static Dictionary<string, object> ExtTypeObject = new Dictionary<string, object>();
        private static Dictionary<string, FairyGUI.GComponent> key2com = new Dictionary<string, FairyGUI.GComponent>();

        public static List<string> CheckExt()
        {
            ExtTypeObject.Clear();
            ExtTypeObject.Add($"{UIExtends.None}", string.Empty);
            int i1 = 0;
            while (typeof(UIExtends).IsEnumDefined(i1))
            {
                var panelEx = (UIExtendPanel)i1;
                string key = $"{UIExtends.Panel}/{panelEx}";
                ExtTypeObject.Add(key, panelEx);
                i1++;
            }
            int i2 = 0;
            while (typeof(UIExtendComponent).IsEnumDefined(i2))
            {
                var comEx = (UIExtendComponent)i2;
                string key = $"{UIExtends.Component}/{comEx}";
                ExtTypeObject.Add(key, comEx);
                i2++;
            }

            int i3 = 0;
            while (typeof(UIExttendFGUI).IsEnumDefined(i3))
            {
                var fguiEx = (UIExttendFGUI)i3;
                string key = $"{UIExtends.FGUI}/{fguiEx}";
                ExtTypeObject.Add(key, fguiEx);
                i3++;
            }

            return ExtTypeObject.Keys.ToList();
        }

        public static bool GetExtObject(string key, out object obj)
        {
            if (ExtTypeObject.TryGetValue(key, out obj))
            {
                return true;
            }
            return false;
        }
        static FairyGUI.GComponent CreateObject(string pkg, string res)
        {
            var com = (FairyGUI.GComponent)FairyGUI.UIPackage.CreateObject(pkg, res);
            com.displayObject.gameObject.hideFlags = HideFlags.HideAndDontSave;
            return com;
        }
        public static GComponent GetOrAddGComBy(PackageItem pi)
        {
            var key = CreateKey(pi);
            if (!key2com.TryGetValue(key, out var com))
            {
                com = CreateObject(pi.owner.name, pi.name);
                key2com.Add(key, com);
            }
            return com;
        }
        public static GComponent GetOrAddGComBy(string pkg, string res)
        {
            var key = CreateKey(pkg, res);
            if (!key2com.TryGetValue(key, out var com))
            {
                com = CreateObject(pkg, res);
                key2com.Add(key, com);
            }
            return com;
        }
        public static bool GetGComBy(string pkg, string res, out GComponent com)
        {
            var key = CreateKey(pkg, res);
            if (!key2com.TryGetValue(key, out com))
            {
                return false;
            }
            return true;
        }
        public static bool GetGComBy(PackageItem pi, out GComponent com)
        {
            var key = CreateKey(pi);
            if (!key2com.TryGetValue(key, out com))
            {
                return false;
            }
            return true;
        }
        public static string CreateKey(PackageItem pi)
        {
            return CreateKey(pi.owner.name, pi.name);
        }
        public static string CreateKey(string pkg, string res)
        {
            return pkg + "_" + res;
        }


        public static List<PackageItem> GetPackageItems(UIPackage pkg)
        {
            var items = pkg.GetItems();
            List<PackageItem> itemDatas = new List<PackageItem>();
            foreach (var item in items)
            {
                if (item.type == PackageItemType.Component)
                {
                    if (item.exported)
                    {
                        itemDatas.Add(item);
                    }
                    else if (CustomTypeList.Contains(item.objectType))
                    {
                        itemDatas.Add(item);
                    }
                }
            }
            return itemDatas;
        }

        //获取依赖的包
        public static List<string> GetDependenciesPkg(GComponent com)
        {
            List<string> r = new List<string>();
            GetDependenciesPkg(com, r);
            return r;
        }
        static void GetDependenciesPkg(GComponent com, List<string> r)
        {
            if (!r.Contains(com.packageItem.owner.name))
            {
                r.Add(com.packageItem.owner.name);
            }
            for (var i = 0; i < com.numChildren; i++)
            {
                var child = com.GetChildAt(i);
                if (child.packageItem != null)
                {
                    var childCom = child.asCom;
                    if (childCom != null)
                    {
                        GetDependenciesPkg(childCom, r);
                    }
                    else
                    {
                        if (!r.Contains(child.packageItem.owner.name))
                        {
                            r.Add(child.packageItem.owner.name);
                        }
                    }
                }
            }
        }

        public static string CheckExt(FairyGUI.GComponent com)
        {

            string str = com.packageItem.name;

            string strMatch = @"(?<v>[\S]+)(?=TabView)";
            var match = Regex.IsMatch(str, strMatch);
            if (match) return $"{UIExtends.Panel}/{UIExtendPanel.TabView}";

            strMatch = @"(?<v>[\S]+)(?=View)";
            match = Regex.IsMatch(str, strMatch);
            if (match) return $"{UIExtends.Panel}/{UIExtendPanel.View}";

            strMatch = @"(?<v>[\S]+)(?=Window)";
            match = Regex.IsMatch(str, strMatch);
            if (match) return $"{UIExtends.Panel}/{UIExtendPanel.Window}";

            strMatch = @"(?<v>[\S]+)(?=Dialog)";
            match = Regex.IsMatch(str, strMatch);
            if (match) return $"{UIExtends.Panel}/{UIExtendPanel.Dialog}";

            strMatch = @"(?<v>[\S]+)(?=TabFrame)";
            match = Regex.IsMatch(str, strMatch);
            if (match) return $"{UIExtends.Component}/{UIExtendComponent.TabFrame}";

            strMatch = @"(?<v>[\S]+)(?=TabBtn)";
            match = Regex.IsMatch(str, strMatch);
            if (match) return $"{UIExtends.Component}/{UIExtendComponent.TabBtn}";

            if (com is GButton)
            {
                return $"{UIExtends.FGUI}/{UIExttendFGUI.Button}";
            }
            else if (com is GProgressBar)
            {
                return $"{UIExtends.FGUI}/{UIExttendFGUI.ProgressBar}";
            }
            else if (com is GComponent)
            {
                return $"{UIExtends.Component}/{UIExtendComponent.Object}";
            }
            return $"{UIExtends.None}";
        }

        public static void Clear()
        {
            ExtTypeObject.Clear();
        }
        public static void ClearFairyGUI()
        {
            foreach (var kv in key2com)
            {
                kv.Value.Dispose();
            }
            key2com.Clear();
        }
    }
}