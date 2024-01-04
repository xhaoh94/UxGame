using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using static UI.Editor.ComponentData;
using static UI.Editor.UIMemberData;

namespace UI.Editor
{
    public class WriteData
    {
        string content;
        bool newLine;
        public WriteData(string _content)
        {
            content = _content;
        }
        public WriteData()
        {
            content = string.Empty;
        }
        public void Writeln()
        {
            content += "\n";
            newLine = true;
        }
        public void Writeln(string add, bool isBlock = true)
        {
            if (isBlock)
            {
                foreach (var b in block)
                {
                    content += b;
                }
            }
            content += add + "\n";
            newLine = true;
        }
        public void Write(string add, bool isBlock = true)
        {
            if (isBlock)
            {
                foreach (var b in block)
                {
                    content += b;
                }
            }
            content += add;
        }

        List<string> block = new List<string>();
        public void StartBlock()
        {
            Writeln("{", newLine);
            block.Add("\t");
        }
        public void EndBlock(bool isLn = true)
        {
            block.RemoveAt(0);
            if (isLn)
                Writeln("}");
            else
                Write("}");
        }

        public void Export(string path, string fileName)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path += $"{fileName}.cs";
            if (!File.Exists(path))
            {
                File.CreateText(path).Dispose();
            }
            File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        }
    }
    public partial class UICodeGenWindow
    {
        public static void Export()
        {
            Log.Debug("---------------------------------------->开始生成UI代码文件<---------------------------------------");
            UICodeGenSettingData.Load();
            UIEditorTools.CheckExt();
            FairyGUIEditor.EditorToolSet.LoadPackages();
            List<FairyGUI.UIPackage> pkgs = FairyGUI.UIPackage.GetPackages();
            foreach (var pkg in pkgs)
            {
                OnExport(pkg);
            }
            Log.Debug("---------------------------------------->完成生成UI代码文件<---------------------------------------");
        }
        void OnBtnGenClick()
        {
            if (selectItem != null)
            {
                if (UIEditorTools.GetGComBy(selectItem.pi, out var com))
                {
                    OnExport(com);
                }
                Log.Debug("UI代码生成");
            }
        }

        void OnBtnGenAllClick()
        {
            var genPath = UICodeGenSettingData.CodeGenPath;
            if (Directory.Exists(genPath))
            {
                Directory.Delete(genPath, true);
            }
            Export();
        }
        static void OnExport(FairyGUI.UIPackage pkg)
        {
            var pis = UIEditorTools.GetPackageItems(pkg);
            foreach (var pi in pis)
            {
                var com = UIEditorTools.GetOrAddGComBy(pi);
                OnExport(com);
            }
        }

        static void OnExport(FairyGUI.GComponent com)
        {
            var comData = UICodeGenSettingData.GetOrAddComponentData(com);
            if (!comData.isExport) return;
            if (comData.IsExNone) return;
            var ext = comData.Extend;
            var ns = comData.GetNs();
            var clsName = string.IsNullOrEmpty(comData.cls) ? com.packageItem.name : comData.cls;
            var path = comData.GetPath();
            if (path == string.Empty)
            {
                Log.Error("导出目录不能为空");
                return;
            }
            var write = new WriteData();
            write.Writeln(@"//自动生成的代码，请勿修改!!!");
            write.Writeln("using FairyGUI;");
            write.Writeln($"namespace {ns}");
            write.StartBlock();

            Action clsFn = () =>
            {
                write.Writeln($"public partial class {clsName} : UI{ext}");
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
                foreach (var temData in comData.DialogData)
                {
                    if (string.IsNullOrEmpty(temData.Name)) continue;
                    write.Writeln($"protected override {temData.Type} {temData.Key} => {temData.Name};");
                    ignores.Add(temData.Name);
                    b = true;
                }
                return b;
            }

            switch (ext)
            {
                case UIExtendPanel.View:
                case UIExtendPanel.Window:
                case UIExtendPanel.TabView:
                case UIExtendPanel.Dialog:
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

                    var lazyloads = UIClassifySettingData.GetLazyloadsByKeys(UIClassifyWindow.ResClassifySettings, pkgs);
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
                    clsFn();
                    write.Writeln($"protected override string PkgName => \"{com.packageItem.owner.name}\";");
                    write.Writeln($"protected override string ResName => \"{com.packageItem.name}\";");
                    if (ext is UIExtendPanel.Dialog)
                    {
                        Func(comData.DialogData);
                    }
                    write.Writeln();
                    memberVarFn();
                    break;
                case UIExtendComponent.TabFrame:
                    clsFn();
                    if (Func(comData.TabViewData))
                    {
                        write.Writeln();
                    }
                    memberVarFn();
                    write.Writeln($"public {clsName}(GObject gObject,UIObject parent)");
                    write.StartBlock();
                    write.Writeln($"Init(gObject,parent);");
                    write.Writeln($"parent?.Components?.Add(this);");
                    write.EndBlock();
                    break;
                case UIExtendComponent.TabBtn:
                    clsFn();
                    memberVarFn();
                    break;
                default:
                    clsFn();
                    memberVarFn();
                    write.Writeln($"public {clsName}(GObject gObject,UIObject parent)");
                    write.StartBlock();
                    write.Writeln($"Init(gObject,parent);");
                    write.Writeln($"parent?.Components?.Add(this);");
                    write.EndBlock();
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
                if (ext is UIExtendPanel.Window || ext is UIExtendPanel.Dialog)
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
                        if (member.IsTabContent()) frame = member;
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
                    write.Writeln($"{list.name}.itemRenderer = {fnName}Renderer;");
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
                    write.Writeln($"void _{fnName}Click(EventContext e)");
                    write.StartBlock();
                    write.Writeln($"{fnName}Click(e);");
                    write.EndBlock();
                    write.Writeln($"partial void {fnName}Click(EventContext e);");

                    write.Writeln($"void _{fnName}Renderer(int index, GObject item)");
                    write.StartBlock();
                    write.Writeln($"{fnName}Renderer(index, item);");
                    write.EndBlock();
                    write.Writeln($"partial void {fnName}Renderer(int index, GObject item);");
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
                write.Writeln($"protected ITabView GetCurrentTab()");
                write.StartBlock();
                write.Writeln($"return {frame.name}?.SelectItem;");
                write.EndBlock();
                write.Writeln($"protected void SetTabRenderer<T>() where T : UITabBtn");
                write.StartBlock();
                write.Writeln($"{frame.name}?.SetTabRenderer<T>();");
                write.EndBlock();
            }


            write.EndBlock();
            write.EndBlock();

            var temPath = $"{path}/{com.packageItem.owner.name}/";
            write.Export(temPath, clsName);
        }


    }
}