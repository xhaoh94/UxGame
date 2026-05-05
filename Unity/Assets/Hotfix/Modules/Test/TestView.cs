using Cysharp.Threading.Tasks;
using FairyGUI;
using System.Collections.Generic;
using UnityEngine;

namespace Ux.UI
{
    public partial class TestView
    {
        bool _runningUiChecklist;

        partial void OnBtnTestClick(EventContext e)
        {
            if (_runningUiChecklist)
            {
                Log.Warning("UI 验收正在执行中，请稍后");
                return;
            }

            RunUiChecklist().Forget();
        }

        async UniTaskVoid RunUiChecklist()
        {
            _runningUiChecklist = true;
            Log.Info("========== UI 手工验收开始 ==========");

            try
            {
                await Case_ShowHide_Single();
                await Case_Tab_B_Hide_B_Show_C();
                await Case_Tab_FastShowHide_Twice();
                await Case_Login_To_Main();
                await Case_Stack_Open_Close();
                await Case_CacheAndReuse();
            }
            catch (System.Exception ex)
            {
                Log.Error($"UI 验收中断: {ex}");
            }
            finally
            {
                Log.Info("========== UI 手工验收结束 ==========");
                _runningUiChecklist = false;
            }
        }

        async UniTask Case_ShowHide_Single()
        {
            Log.Info("[Case] 单界面显示/关闭开始");
            await UIMgr.Ins.Create().Show<BagWindow>().Task();
            await UniTask.DelayFrame(2);
            UIMgr.Ins.Hide<BagWindow>();
            await UniTask.DelayFrame(10);
            DumpUiState("[Case] 单界面显示/关闭结束");
        }

        async UniTask Case_Tab_B_Hide_B_Show_C()
        {
            Log.Info("[Case] Show(B) -> Hide(B) -> Show(C) 开始");
            UIMgr.Ins.Create().Show<Multiple2TabView>();
            await UniTask.DelayFrame(1);
            UIMgr.Ins.Hide<Multiple2TabView>();
            await UniTask.DelayFrame(1);
            UIMgr.Ins.Create().Show<Multiple3TabView>();
            await UniTask.DelayFrame(20);
            DumpUiState("[Case] Show(B) -> Hide(B) -> Show(C) 结束");
            UIMgr.Ins.Hide<Multiple3TabView>();
            await UniTask.DelayFrame(10);
        }

        async UniTask Case_Tab_FastShowHide_Twice()
        {
            Log.Info("[Case] 快速 Show/Hide Multiple2TabView 两次 开始");

            UIMgr.Ins.Create().Show<Multiple2TabView>();
            UIMgr.Ins.Hide<Multiple2TabView>();
            await UniTask.DelayFrame(15);
            DumpUiState("[Case] 第一次快速开关结束");

            UIMgr.Ins.Create().Show<Multiple2TabView>();
            UIMgr.Ins.Hide<Multiple2TabView>();
            await UniTask.DelayFrame(15);
            DumpUiState("[Case] 第二次快速开关结束");
        }

        async UniTask Case_Login_To_Main()
        {
            Log.Info("[Case] Login -> Main 开始");
            await UIMgr.Ins.Create().Show<LoginView>().Task();
            await UniTask.DelayFrame(2);
            await UIMgr.Ins.Create().Show<MainView>().Task();
            UIMgr.Ins.Hide<LoginView>();
            await UniTask.DelayFrame(20);
            DumpUiState("[Case] Login -> Main 结束");

            UIMgr.Ins.Hide<MainView>();
            await UniTask.DelayFrame(10);
        }

        async UniTask Case_Stack_Open_Close()
        {
            Log.Info("[Case] Stack 链路开始");
            await UIMgr.Ins.Create().Show<Stack1View>().Task();
            await UniTask.DelayFrame(2);

            UIMgr.Ins.Create().Show<Stack2View>();
            await UniTask.DelayFrame(10);

            UIMgr.Ins.Create().Show<Stack3View>();
            await UniTask.DelayFrame(10);

            UIMgr.Ins.Hide<Stack3View>();
            await UniTask.DelayFrame(10);

            UIMgr.Ins.Hide<Stack2View>();
            await UniTask.DelayFrame(10);

            UIMgr.Ins.Hide<Stack1View>();
            await UniTask.DelayFrame(10);

            DumpUiState("[Case] Stack 链路结束");
        }

        async UniTask Case_CacheAndReuse()
        {
            Log.Info("[Case] 缓存复用开始");
            await UIMgr.Ins.Create().Show<TipView>().Task();
            await UniTask.DelayFrame(2);
            UIMgr.Ins.Hide<TipView>();
            await UniTask.DelayFrame(10);

            await UIMgr.Ins.Create().Show<TipView>().Task();
            await UniTask.DelayFrame(10);

            DumpUiState("[Case] 缓存复用结束");
            UIMgr.Ins.Hide<TipView>();
            await UniTask.DelayFrame(10);
        }

        void DumpUiState(string title)
        {
            var dbg = (IUIMgrDebuggerAccess)UIMgr.Ins;

            Log.Info(title);
            Log.Info($"ShowedUI: {string.Join(", ", dbg.GetShowedUI())}");
            Log.Info($"ShowingUI: {string.Join(", ", dbg.GetShowingUI())}");
            Log.Info($"CacheUI: {string.Join(", ", dbg.GetCacheUI())}");
            Log.Info($"WaitDelUI: {string.Join(", ", dbg.GetWaitDelUI())}");

            var stacks = dbg.GetUIStacks();
            if (stacks == null || stacks.Count == 0)
            {
                Log.Info("UIStacks: <empty>");
                return;
            }

            var parts = new List<string>(stacks.Count);
            for (int i = 0; i < stacks.Count; i++)
            {
                var s = stacks[i];
                parts.Add($"[{i}] Parent={s.ParentID}, ID={s.ID}, Type={s.Type}");
            }
            Log.Info($"UIStacks: {string.Join(" | ", parts)}");
        }
    }
}
