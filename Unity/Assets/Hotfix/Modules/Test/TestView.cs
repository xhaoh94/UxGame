using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Ux.UI
{
    [UI]
    public partial class TestView
    {
        bool _runningUiChecklist;
        int _singleClickCount;
        int _doubleClickCount;
        int _longPressCount;
        int _topTipCount;

        protected override void OnInit()
        {
            base.OnInit();

            // Prefab 里配置了两个模板：
            //   TypeId 0 -> TestItem.prefab
            //   TypeId 1 -> Test22Item.prefab
            // 映射注册在 testList 实例上，其他列表可以有自己的映射。
            testList.SetItemRenderer<TestItem>(0);
            testList.SetItemRenderer<Test22Item>(1);

            // 按索引切换模板与 Renderer：偶数行 TestItem，奇数行 Test22Item。
            testList.SetItemProvider(index => index % 2 == 0 ? 0 : 1);
        }

        protected override void OnShow()
        {
            base.OnShow();

            var datas = new List<int>(20);
            for (var i = 0; i < 20; i++)
                datas.Add(i);
            testList.SetDatas(datas);
        }

        partial void OnBtnTestClick(UIEventContext e)
        {
            if (_runningUiChecklist)
            {
                Log.Warning("UI 验收正在执行中，请稍后");
                return;
            }    
            RunUiChecklist().Forget();
        }

        /// <summary>多页签：显示子页签会自动带出父面板 MultipleView。</summary>
        partial void OnBtnMultipleClick(UIEventContext e)
        {
            Log.Info("[Test] 多页签界面 Multiple2TabView");
            UIMgr.Ins.Show<Multiple2TabView>();
        }

        /// <summary>弹窗：普通窗口类型，带模糊背景。</summary>
        partial void OnBtnNoneWindowClick(UIEventContext e)
        {
            Log.Info("[Test] 弹窗 BagWindow");
            UIMgr.Ins.Show<BagWindow>();
        }

        /// <summary>置顶界面：TipView 是 UITip（Layer=Top），必须带 TipData 参数。</summary>
        partial void OnBtnNoneViewClick(UIEventContext e)
        {
            _topTipCount++;
            Log.Info($"[Test] 置顶界面 TipView x{_topTipCount}");
            var tip = new UITipFactory.TipData(null, null, $"置顶提示 {_topTipCount}");
            UIMgr.Ins.Show<TipView>(IUIParam.Create(tip));
        }

        /// <summary>单击：计数 + 工厂 Tip，验证 UIMgr.Tip 与 UITip 生命周期。</summary>
        partial void OnBtnSingleClick(UIEventContext e)
        {
            _singleClickCount++;
            Log.Info($"[Test] 单击 x{_singleClickCount}");
            UIMgr.Tip.Show($"单击 {_singleClickCount} 次");
        }

        /// <summary>双击：奇数打开、偶数关闭 BagWindow，验证双击节流与窗口缓存复用。</summary>
        partial void OnBtnDoubleMultipleClick(UIEventContext e)
        {
            _doubleClickCount++;
            var show = _doubleClickCount % 2 == 1;
            Log.Info($"[Test] 双击 x{_doubleClickCount} -> {(show ? "打开" : "关闭")} BagWindow");
            if (show) UIMgr.Ins.Show<BagWindow>();
            else UIMgr.Ins.Hide<BagWindow>();
        }

        /// <summary>长按：持续回调，每 10 次打印一次当前 UI 状态，避免刷屏。</summary>
        partial void OnBtnLongClickLongPress(ref bool isBreak)
        {
            _longPressCount++;
            if (_longPressCount % 10 != 0) return;
            Log.Info($"[Test] 长按持续 x{_longPressCount}");
            DumpUiState("[Test] 长按期间状态");
        }

        /// <summary>返回：关闭当前 TestView，验证 HideSelf 与缓存。</summary>
        partial void OnBtnBackClick(UIEventContext e)
        {
            Log.Info("[Test] 返回：关闭 TestView");
            HideSelf();
        }
        async UniTaskVoid RunUiTest()
        {
            Log.Info("xxxxxxxxxx111");
            UIMgr.Ins.Show<BagWindow>();
            await UIMgr.Ins.Show<BagWindow>().Task();
            Log.Info("xxxxxxxxxx222");
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
            await UIMgr.Ins.Show<BagWindow>().Task();
            await UniTask.DelayFrame(2);
            UIMgr.Ins.Hide<BagWindow>();
            await UniTask.DelayFrame(10);
            DumpUiState("[Case] 单界面显示/关闭结束");
        }

        async UniTask Case_Tab_B_Hide_B_Show_C()
        {
            Log.Info("[Case] Show(B) -> Hide(B) -> Show(C) 开始");
            UIMgr.Ins.Show<Multiple2TabView>();
            await UniTask.DelayFrame(1);
            UIMgr.Ins.Hide<Multiple2TabView>();
            await UniTask.DelayFrame(1);
            UIMgr.Ins.Show<Multiple3TabView>();
            await UniTask.DelayFrame(20);
            DumpUiState("[Case] Show(B) -> Hide(B) -> Show(C) 结束");
            UIMgr.Ins.Hide<Multiple3TabView>();
            await UniTask.DelayFrame(10);
        }

        async UniTask Case_Tab_FastShowHide_Twice()
        {
            Log.Info("[Case] 快速 Show/Hide Multiple2TabView 两次 开始");

            UIMgr.Ins.Show<Multiple2TabView>();
            UIMgr.Ins.Hide<Multiple2TabView>();
            await UniTask.DelayFrame(15);
            DumpUiState("[Case] 第一次快速开关结束");

            UIMgr.Ins.Show<Multiple2TabView>();
            UIMgr.Ins.Hide<Multiple2TabView>();
            await UniTask.DelayFrame(15);
            DumpUiState("[Case] 第二次快速开关结束");
        }

        async UniTask Case_Login_To_Main()
        {
            Log.Info("[Case] Login -> Main 开始");
            await UIMgr.Ins.Show<LoginView>().Task();
            await UniTask.DelayFrame(2);
            await UIMgr.Ins.Show<MainView>().Task();
            UIMgr.Ins.Hide<LoginView>();
            await UniTask.DelayFrame(20);
            DumpUiState("[Case] Login -> Main 结束");

            UIMgr.Ins.Hide<MainView>();
            await UniTask.DelayFrame(10);
        }

        async UniTask Case_Stack_Open_Close()
        {
            Log.Info("[Case] Stack 链路开始");
            await UIMgr.Ins.Show<Stack1View>().Task();
            await UniTask.DelayFrame(2);

            UIMgr.Ins.Show<Stack2View>();
            await UniTask.DelayFrame(10);

            UIMgr.Ins.Show<Stack3View>();
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

            // TipView 需要 TipData 参数，直接 Show 会报“Tip 参数类型错误”
            var tip1 = new UITipFactory.TipData(null, null, "缓存复用 1");
            await UIMgr.Ins.Show<TipView>(IUIParam.Create(tip1)).Task();
            await UniTask.DelayFrame(2);
            UIMgr.Ins.Hide<TipView>();
            await UniTask.DelayFrame(10);

            var tip2 = new UITipFactory.TipData(null, null, "缓存复用 2");
            await UIMgr.Ins.Show<TipView>(IUIParam.Create(tip2)).Task();
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
