﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using static Ux.Editor.Build.UI.ComponentData;
using static Ux.Editor.Build.UI.UIMemberData;

namespace Ux.Editor.Build.UI
{    
    public partial class UICodeGenWindow
    {
        public static bool Export()
        {
            Log.Debug("---------------------------------------->开始生成UI代码文件<---------------------------------------");
            UICodeGenSettingData.Load();
            UIEditorTools.CheckExt();
            FairyGUIEditor.EditorToolSet.LoadPackages();
            List<FairyGUI.UIPackage> pkgs = FairyGUI.UIPackage.GetPackages();
            foreach (var pkg in pkgs)
            {
                if (!OnExport(pkg))
                {
                    return false;
                }
            }
            Log.Debug("---------------------------------------->完成生成UI代码文件<---------------------------------------");
            return true;
        }        
        static bool OnExport(FairyGUI.UIPackage pkg)
        {
            var pis = UIEditorTools.GetPackageItems(pkg);
            foreach (var pi in pis)
            {
                var com = UIEditorTools.GetOrAddGComBy(pi);
                if (!OnExport(com))
                {
                    return false;
                }
            }
            return true;
        }

        static bool OnExport(FairyGUI.GComponent com)
        {
            var comData = UICodeGenSettingData.GetOrAddComponentData(com);
            if (!comData.isExport) return true;
            if (comData.IsExNone) return true;
            var ext = comData.Extend;
            var ns = comData.GetNs();
            var clsName = string.IsNullOrEmpty(comData.cls) ? com.packageItem.name : comData.cls;
            var path = comData.GetPath();
            if (path == string.Empty)
            {
                Log.Error("导出目录不能为空");
                return false;
            }
            var write = new CodeGenWrite();
            write.Writeln(@"//自动生成的代码，请勿修改!!!");
            write.Writeln("using FairyGUI;");
            write.Writeln($"namespace {ns}");
            write.StartBlock();

            Action clsFn = () =>
            {
                switch (ext)
                {
                    case UIExtendComponent.UIModel:
                    case UIExtendComponent.RTModel:
                    case UIExtendComponent.ItemRenderer:
                        write.Writeln($"public partial class {clsName} : {ext}");
                        break;
                    default:
                        write.Writeln($"public partial class {clsName} : UI{ext}");
                        break;
                }
                write.StartBlock();
            };

            var members = comData.GetMembers();
            Action memberVarFn = () =>
            {
                foreach (var member in members)
                {
                    if (!member.isCreateVar) continue;
                    write.Writeln($"protected {member.customType} {member.name};");
                }
            };

            HashSet<string> ignores = new HashSet<string>();
            bool Func(List<CustomData> listData)
            {
                bool b = false;
                foreach (var temData in listData)
                {
                    if (string.IsNullOrEmpty(temData.Name)) continue;
                    write.Writeln($"protected override {temData.Type} {temData.Key} => {temData.Name};");
                    ignores.Add(temData.Name);
                    b = true;
                }
                return b;
            }
            //特性
            switch (ext)
            {
                case UIExtendPanel.View:
                case UIExtendPanel.Window:
                case UIExtendPanel.TabView:
                case UIExtendPanel.MessageBox:
                case UIExtendPanel.Tip:
                    var pkgs = UIEditorTools.GetDependenciesPkg(com);
                    if (pkgs != null && pkgs.Count > 0)
                    {
                        string pkgStr = string.Empty;
                        foreach (var pkg in pkgs)
                        {
                            if (!string.IsNullOrEmpty(pkgStr))
                            {
                                pkgStr += ",";
                            }
                            pkgStr += $"\"{pkg}\"";
                        }
                        write.Writeln($"[Package({pkgStr})]");
                    }

                    var lazyloads = UIClassifySettingData.GetLazyloadsByKeys(pkgs);
                    if (lazyloads != null && lazyloads.Count > 0)
                    {
                        string lazyloadStr = string.Empty;
                        foreach (var lazyload in lazyloads)
                        {
                            if (!string.IsNullOrEmpty(lazyloadStr))
                            {
                                lazyloadStr += ",";
                            }
                            lazyloadStr += $"\"{lazyload}\"";
                        }
                        write.Writeln($"[Lazyload({lazyloadStr})]");
                    }
                    break;
                case UIExtendComponent.ItemRenderer:
                    write.Writeln($"[ItemUrl(\"{com.resourceURL}\")]");
                    break;
            }
            //类
            clsFn();
            //资源包
            switch (ext)
            {
                case UIExtendPanel.View:
                case UIExtendPanel.Window:
                case UIExtendPanel.TabView:
                case UIExtendPanel.MessageBox:
                case UIExtendPanel.Tip:
                    write.Writeln($"protected override string PkgName => \"{com.packageItem.owner.name}\";");
                    write.Writeln($"protected override string ResName => \"{com.packageItem.name}\";");
                    break;
            }
            //重写字段
            switch (ext)
            {
                case UIExtendPanel.MessageBox:
                    Func(comData.MessageBoxData);
                    break;
                case UIExtendPanel.Tip:
                    Func(comData.TipData);
                    break;
                case UIExtendComponent.TabFrame:
                    Func(comData.TabViewData);
                    break;
                case UIExtendComponent.UIModel:
                case UIExtendComponent.RTModel:
                    Func(comData.ModelData);
                    break;
            }
            //变量字段
            memberVarFn();
            //组件构造方法
            switch (ext)
            {
                case UIExtendPanel.View:
                case UIExtendPanel.Window:
                case UIExtendPanel.TabView:
                case UIExtendPanel.MessageBox:
                case UIExtendPanel.Tip:                
                case UIExtendComponent.ItemRenderer:
                    break;
                default:
                    write.Writeln($"public {clsName}(GObject gObject,UIObject parent): base(gObject, parent) {{ }}");
                    //write.StartBlock();
                    //write.Writeln($"Init(gObject,parent);");
                    //write.Writeln($"parent?.Components?.Add(this);");
                    //write.EndBlock();
                    break;
            }

            UIMemberData frame = null;
            List<UIMemberData> btns = new List<UIMemberData>();
            List<UIMemberData> dbtns = new List<UIMemberData>();
            List<UIMemberData> longbtns = new List<UIMemberData>();
            List<UIMemberData> lists = new List<UIMemberData>();
            write.Writeln($"protected override void CreateChildren()");
            write.StartBlock();
            if (members.Count > 0)
            {
                write.Writeln("try");
                write.StartBlock();
                if (ext is UIExtendPanel.Window || ext is UIExtendPanel.MessageBox)
                {
                    write.Writeln("var gCom = ObjAs<Window>().contentPane;");
                }
                else
                {
                    write.Writeln("var gCom = ObjAs<GComponent>();");
                }
                foreach (var member in members)
                {
                    if (!member.isCreateIns) continue;

                    if (member.customType != member.defaultType)
                    {
                        if (member.IsTabFrame()) frame = member;
                        write.Writeln($"{member.name} = new {member.customType}(gCom.GetChildAt({member.index}), this);");
                    }
                    else
                    {
                        switch (member.defaultType)
                        {
                            case nameof(FairyGUI.Controller):
                                write.Writeln($"{member.name} = gCom.GetControllerAt({member.index});");
                                break;
                            case nameof(FairyGUI.Transition):
                                write.Writeln($"{member.name} = gCom.GetTransitionAt({member.index});");
                                break;
                            default:
                                write.Writeln($"{member.name} = ({member.defaultType})gCom.GetChildAt({member.index});");
                                break;
                        }
                    }
                    if (member.evtType == "无") continue;
                    if (ignores.Contains(member.name)) continue;
                    switch (member.defaultType)
                    {
                        case nameof(FairyGUI.GButton):
                            switch (member.evtType)
                            {
                                case "单击":
                                    btns.Add(member);
                                    break;
                                case "多击":
                                    dbtns.Add(member);
                                    break;
                                case "长按":
                                    longbtns.Add(member);
                                    break;
                            }

                            break;
                        case nameof(FairyGUI.GList):
                            lists.Add(member);
                            break;
                    }
                }
                write.EndBlock();
                write.Writeln("catch (System.Exception e)");
                write.StartBlock();
                write.Writeln(" Log.Error(e);");
                write.EndBlock();
            }
            write.EndBlock();
            if (btns.Count > 0 || dbtns.Count > 0 ||
                    longbtns.Count > 0 || lists.Count > 0)
            {
                write.Writeln($"protected override void OnAddEvent()");
                write.StartBlock();
                foreach (var btn in btns)
                {
                    var fnName = $"_On{char.ToUpper(btn.name[0])}{btn.name.Substring(1)}Click";
                    write.Writeln($"AddClick({btn.name},{fnName});");
                }
                foreach (var btn in dbtns)
                {
                    MemberEvtDouble dContent;
                    if (string.IsNullOrEmpty(btn.evtParam))
                    {
                        dContent = new MemberEvtDouble();
                        dContent.dCnt = 2;
                        dContent.dGapTime = 0.2f;
                    }
                    else
                    {
                        dContent = JsonConvert.DeserializeObject<MemberEvtDouble>(btn.evtParam);
                    }

                    var fnName = $"_On{char.ToUpper(btn.name[0])}{btn.name.Substring(1)}MultipleClick";
                    write.Writeln($"AddMultipleClick({btn.name},{fnName}, {dContent.dCnt}, {dContent.dGapTime}f);");
                }
                foreach (var btn in longbtns)
                {
                    MemberEvtLong lContent;
                    if (string.IsNullOrEmpty(btn.evtParam))
                    {
                        lContent = new MemberEvtLong();
                        lContent.lFirst = -1;
                        lContent.lGapTime = 0.2f;
                        lContent.lCnt = 0;
                        lContent.lRadius = 50f;
                    }
                    else
                    {
                        lContent = JsonConvert.DeserializeObject<MemberEvtLong>(btn.evtParam);
                    }

                    var fnName = $"_On{char.ToUpper(btn.name[0])}{btn.name.Substring(1)}LongPress";
                    write.Writeln($"AddLongPress({btn.name},{lContent.lFirst}f, {fnName}, " +
                                           $"{lContent.lGapTime}f, {lContent.lCnt}, {lContent.lRadius});");
                }
                foreach (var list in lists)
                {
                    var fnName = $"_On{char.ToUpper(list.name[0])}{list.name.Substring(1)}Item";
                    write.Writeln($"AddItemClick({list.name},{fnName}Click);");
                }
                write.EndBlock();

                foreach (var btn in btns)
                {
                    var fnName = $"On{char.ToUpper(btn.name[0])}{btn.name.Substring(1)}Click";
                    write.Writeln($"void _{fnName}(EventContext e)");
                    write.StartBlock();
                    write.Writeln($"{fnName}(e);");
                    write.EndBlock();
                    write.Writeln($"partial void {fnName}(EventContext e);");
                }
                foreach (var btn in dbtns)
                {
                    var fnName = $"On{char.ToUpper(btn.name[0])}{btn.name.Substring(1)}MultipleClick";
                    write.Writeln($"void _{fnName}(EventContext e)");
                    write.StartBlock();
                    write.Writeln($"{fnName}(e);");
                    write.EndBlock();
                    write.Writeln($"partial void {fnName}(EventContext e);");
                }
                foreach (var btn in longbtns)
                {
                    var fnName = $"On{char.ToUpper(btn.name[0])}{btn.name.Substring(1)}LongPress";
                    write.Writeln($"bool _{fnName}()");
                    write.StartBlock();
                    write.Writeln("bool b = false;");
                    write.Writeln($"{fnName}(ref b);");
                    write.Writeln("return b;");
                    write.EndBlock();
                    write.Writeln($"partial void {fnName}(ref bool isBreak);");
                }
                foreach (var list in lists)
                {
                    var fnName = $"On{char.ToUpper(list.name[0])}{list.name.Substring(1)}Item";
                    write.Writeln($"void _{fnName}Click(IItemRenderer item)");
                    write.StartBlock();
                    write.Writeln($"{fnName}Click(item);");
                    write.EndBlock();
                    write.Writeln($"partial void {fnName}Click(IItemRenderer item);");
                }
            }

            if (frame != null)
            {
                write.Writeln($"public override void AddChild(UITabView child)");
                write.StartBlock();
                write.Writeln($"{frame.name}?.AddChild(child);");
                write.EndBlock();
                write.Writeln($"protected void RefreshTab(int selectIndex = 0, bool scrollItToView = true)");
                write.StartBlock();
                write.Writeln($"{frame.name}?.Refresh(selectIndex,scrollItToView);");
                write.EndBlock();
                write.Writeln($"protected IUI GetCurrentTab()");
                write.StartBlock();
                write.Writeln($"return {frame.name}?.SelectItem;");
                write.EndBlock();
                write.Writeln($"public void SetItemRenderer<T>() where T : ItemRenderer");
                write.StartBlock();
                write.Writeln($"{frame.name}?.SetItemRenderer<T>();");
                write.EndBlock();
                write.Writeln($"public void SetItemProvider(System.Func<int, System.Type> itemTypeFunc)");
                write.StartBlock();
                write.Writeln($"{frame.name}?.SetItemProvider(itemTypeFunc);");
                write.EndBlock();
            }


            write.EndBlock();
            write.EndBlock();

            var temPath = $"{path}/{com.packageItem.owner.name}/";
            write.Export(temPath, clsName);
            return true;
        }
    }
}