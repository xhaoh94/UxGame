using Pathfinding;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.State
{
    public partial class StateWindow : EditorWindow
    {

        [MenuItem("UxGame/工具/状态机", false, 520)]
        public static void ShowExample()
        {
            StateWindow wnd = GetWindow<StateWindow>();
            wnd.titleContent = new GUIContent("StateWindow");
        }
        static string AssetPath = "Assets/Data/Res/State";
        static string CodePath = "Assets/Hotfix/CodeGen/State";


        //private StateSettingData Setting;


        public void CreateGUI()
        {
            try
            {
                LoadConfig();
                CreateChildren();
                rootVisualElement.Add(root);

                OnCreateGroup();
                OnCreateListView();
                OnCreateCondition();

            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        void ExportItem(StateAsset data)
        {
            if (data == null) return;
            if (string.IsNullOrEmpty(data.stateName)) return;
            var write = new CodeGenWrite();
            write.Writeln(@"//自动生成的代码，请勿修改!!!");
            write.Writeln("using System.Collections.Generic;");
            write.Writeln($"namespace Ux");
            write.StartBlock();
            write.Writeln($"public partial class {data.stateName} : {nameof(UnitStateBase)}");
            write.StartBlock();
            write.Writeln($"public override string AssetName => \"{string.Format(PathHelper.Res.State, data.stateName)}\";");
            write.EndBlock();
            write.EndBlock();
            write.Export($"{CodePath}/", data.stateName);
        }
        partial void _OnBtnExportClick()
        {
            Directory.Delete(CodePath, true);
            var write = new CodeGenWrite();
            write.Writeln(@"//自动生成的代码，请勿修改!!!");
            write.Writeln("using System;");
            write.Writeln("using System.Collections.Generic;");
            write.Writeln($"namespace Ux");
            write.StartBlock();
            write.Writeln("public static partial class StateMgrEx");
            write.StartBlock();
            write.Writeln("readonly static Dictionary<string, HashSet<Type>> _stateGroup = new Dictionary<string, HashSet<Type>>()");
            write.StartBlock();
            var temData = new List<StateAsset>();
            foreach (var (group, assets) in groupAssets)
            {
                write.Write($"{{ \"{group}\",new() {{");
                temData.Clear();
                temData.AddRange(assets);
                temData.Sort((a, b) =>
                {
                    if (a.priority == b.priority)
                    {
                        return temData.IndexOf(a) - temData.IndexOf(b);
                    }
                    return b.priority - a.priority;
                });
                foreach (var item in temData)
                {
                    if (item.isMute == false)
                    {
                        write.Write($" typeof({item.stateName}),", false);
                    }
                }
                write.Writeln($"}}}},", false);
            }
            write.EndBlock(false);
            write.Writeln(";", false);
            write.Writeln("public static void InitGroup(this UnitStateMachine machine, string group, long OwnerID)");
            write.StartBlock();
            write.Writeln("if (string.IsNullOrEmpty(group)) return;");
            write.Writeln("if (_stateGroup.TryGetValue(group, out var states))");
            write.StartBlock();
            write.Writeln("int index = 0;");
            write.Writeln("foreach (var state in states)");
            write.StartBlock();
            write.Writeln("var item = Activator.CreateInstance(state) as IUnitState;");
            write.Writeln("item.Set(OwnerID);");
            write.Writeln("machine.AddNode(item);");
            write.Writeln("StateMgr.Ins.AddState(item, index == states.Count - 1);");
            write.Writeln("index++;");
            write.EndBlock();
            write.EndBlock();
            write.EndBlock();
            write.EndBlock();
            write.EndBlock();
            write.Export($"{CodePath}/", "StateMgrEx");

            foreach (var (group, assets) in groupAssets)
            {
                foreach (var asset in assets)
                {
                    if (asset.isMute == false)
                    {
                        ExportItem(asset);
                    }
                }
            }

            EditorUtility.DisplayDialog("提示", "导出成功", "确定");
        }

        private void OnDestroy()
        {
            SaveConfig();
            AssetDatabase.Refresh();
        }

        #region 初始化   
        void LoadConfig()
        {
            //获取指定路径下面的所有资源文件  
            var listData = SettingTools.GetAssets<StateAsset>(AssetPath);
            foreach (var item in listData)
            {
                var group = item.group;
                if (string.IsNullOrEmpty(group))
                {
                    group = "未知";
                }
                if (!groupAssets.TryGetValue(group, out var temList))
                {
                    temList = new List<StateAsset>();
                    groupAssets.Add(group, temList);
                }
                showGroups.Add(group);
                temList.Add(item);
            }
        }
        void SaveConfig()
        {
            EditorUtility.SetDirty(Asset);
            AssetDatabase.SaveAssets();            
        }

        #endregion
    }

}
