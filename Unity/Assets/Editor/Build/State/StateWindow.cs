using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Ux.Editor.Build.UI;
namespace Ux.Editor.Build.State
{
    public partial class StateWindow : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        [MenuItem("UxGame/构建/状态机", false, 520)]
        public static void ShowExample()
        {
            StateWindow wnd = GetWindow<StateWindow>();
            wnd.titleContent = new GUIContent("StateWindow");
        }

        private StateSettingData Setting;

        Button _btnExport;



        public void CreateGUI()
        {
            try
            {
                LoadConfig();
                VisualElement root = rootVisualElement;
                m_VisualTreeAsset.CloneTree(root);

                OnCreateGroup();
                OnCreateListView();
                OnCreateView();
                OnCreateCondition();

                _btnExport = root.Q<Button>("btnExport");
                _btnExport.clicked += OnBtnCreate;


                OnUpdateListView();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        void ExportItem(StateSettingData.StateData data)
        {
            if (data == null) return;
            if (string.IsNullOrEmpty(data.ClsName)) return;
            var write = new CodeGenWrite();
            write.Writeln(@"//自动生成的代码，请勿修改!!!");
            write.Writeln("using System.Collections.Generic;");
            write.Writeln($"namespace {Setting.ns}");
            write.StartBlock();
            var resName = string.Empty;
            switch (data.ViewType)
            {
                case StateViewType.None:
                    write.Writeln($"public partial class {data.ClsName} : {nameof(UnitStateBase)}");
                    break;
                case StateViewType.Anim:
                    write.Writeln($"public partial class {data.ClsName} : {nameof(UnitStateAnim)}");
                    resName = Path.GetFileNameWithoutExtension(data.AnimName);
                    break;
                case StateViewType.Timeline:
                    write.Writeln($"public partial class {data.ClsName} : {nameof(UnitStateTimeLine)}");
                    resName = Path.GetFileNameWithoutExtension(data.TimeLineName);
                    break;
            }
            write.StartBlock();
            if (data.Pri != 0)
            {
                write.Writeln($"public override int Priority => {data.Pri};");
            }
            if (data.IsMute)
            {
                write.Writeln($"public override bool IsMute => true;");
            }
            write.Writeln($"public override string Name => \"{data.StateName}\";");
            if (!string.IsNullOrEmpty(resName))
            {
                write.Writeln($"public override string ResName => \"{resName}\";");
            }
            write.Writeln("protected override void InitConditions()");
            write.StartBlock();
            write.Writeln("Conditions = new List<StateConditionBase>()");
            write.StartBlock();
            foreach (var condition in data.Conditions)
            {
                switch (condition.Type)
                {
                    case StateConditionBase.Type.State:
                        if (condition.stateType == StateConditionBase.State.Any)
                        {
                            write.Writeln($"CreateCondition(nameof({nameof(StateCondition)}),StateConditionBase.State.Any, null),");
                        }
                        else
                        {
                            write.Writeln($"CreateCondition(nameof({nameof(StateCondition)}),StateConditionBase.State.{condition.stateType}, new HashSet<string>");
                            write.StartBlock();
                            foreach (var state in condition.states)
                            {
                                write.Writeln($"\"{state}\",");
                            }
                            write.EndBlock(false);
                            write.Writeln("),", false);
                        }
                        break;
                    case StateConditionBase.Type.TempBoolVar:
                        write.Writeln($"CreateCondition(nameof({nameof(TemBoolVarCondition)}),\"{condition.key}\"),");
                        break;
                    case StateConditionBase.Type.Action_Keyboard:
                        write.Writeln($"CreateCondition(nameof({nameof(ActionKeyboardCondition)}),UnityEngine.InputSystem.Key.{condition.keyType}, StateConditionBase.Trigger.{condition.triggerType}),");
                        break;
                    case StateConditionBase.Type.Action_Input:
                        write.Writeln($"CreateCondition(nameof({nameof(ActionInputCondition)}),StateConditionBase.Input.{condition.inputType}, StateConditionBase.Trigger.{condition.triggerType}),");
                        break;
                    case StateConditionBase.Type.Custom:
                        if (string.IsNullOrEmpty(condition.customValue))
                        {
                            write.Writeln($"CreateCondition(\"{condition.customName}\"),");
                        }
                        else
                        {
                            write.Writeln($"CreateCondition(\"{condition.customName}\",\"{condition.customValue}\"),");
                        }
                        break;
                }

            }

            write.EndBlock(false);
            write.Writeln(";", false);
            write.EndBlock();
            write.EndBlock();
            write.EndBlock();
            write.Export($"{Setting.path}/", data.ClsName);
        }
        void OnBtnCreate()
        {
            var write = new CodeGenWrite();
            write.Writeln(@"//自动生成的代码，请勿修改!!!");
            write.Writeln("using System;");
            write.Writeln("using System.Collections.Generic;");
            write.Writeln($"namespace {Setting.ns}");
            write.StartBlock();
            write.Writeln("public static partial class StateMgrEx");
            write.StartBlock();
            write.Writeln("readonly static Dictionary<string, HashSet<Type>> _stateGroup = new Dictionary<string, HashSet<Type>>()");
            write.StartBlock();
            var temData = new List<StateSettingData.StateData>();
            foreach (var group in Setting.groups)
            {
                write.Write($"{{ \"{group}\",new HashSet<Type>() {{");
                temData.Clear();
                temData.AddRange(Setting.StateSettings);
                temData.Sort((a, b) =>
                {
                    if (a.Pri == b.Pri)
                    {
                        return temData.IndexOf(a) - temData.IndexOf(b);
                    }
                    return b.Pri - a.Pri;
                });
                foreach (var item in temData)
                {
                    if (item.Group.Contains(group))
                    {
                        write.Write($" typeof({item.ClsName}),", false);
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
            write.Export($"{Setting.path}/", "StateMgrEx");

            foreach (var item in Setting.StateSettings)
            {
                ExportItem(item);
            }

            EditorUtility.DisplayDialog("提示", "导出成功", "确定");
        }



        StateSettingData.StateData SelectItem
        {
            get
            {
                var selectItem = _listView.selectedItem as StateSettingData.StateData;
                return selectItem;
            }
        }

        private void OnDestroy()
        {
            SaveConfig();
            AssetDatabase.Refresh();
        }

        #region 初始化   
        void LoadConfig()
        {
            Setting = SettingTools.GetSingletonAssets<StateSettingData>("Assets/Setting/Build/State");
        }
        void SaveConfig()
        {
            Setting?.SaveFile();
        }

        #endregion
    }

}
