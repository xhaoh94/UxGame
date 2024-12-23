﻿namespace Ux
{
    public class PatchStateMachine : StateMachine
    {
        IStateNode patchState;
        protected override void OnEnter(IStateNode node)
        {
            if (patchState != null)
            {
                Exit(patchState);
            }
            patchState = node;
        }
    }
    public class PatchMgr : Singleton<PatchMgr>
    {
        private bool _isRun = false;
        public bool IsDone { get; private set; }
        public PatchView View { get; private set; }
        public Downloader Downloader { get; set; }
        /// <summary>
        /// 状态机
        /// </summary>
        PatchStateMachine machine;
        /// <summary>
        /// 开启初始化流程
        /// </summary>
        public void Run()
        {
            if (_isRun == false)
            {
                IsDone = false;
                _isRun = true;
                View = PatchView.Show();
                machine = StateMachine.Create<PatchStateMachine>();
                machine.AddNode(new PatchInit());
                machine.AddNode(new PatchUpdateStaticVersion());
                machine.AddNode(new PatchUpdateManifest());
                machine.AddNode(new PatchCreateDownloader());
                machine.AddNode(new PatchDownloadWebFiles());
                machine.AddNode(new PatchDone());
                Enter<PatchInit>();
            }
            else
            {
                Log.Warning("补丁更新已经正在进行中!");
            }
        }
        public void Done()
        {
            _isRun = false;
            machine.Release();
            machine = null;
            PatchView.Hide();
            View = null;
            IsDone = true;
        }

        public void Enter<TNode>() where TNode : PatchStateNode
        {
            machine.Enter<TNode>();
        }
    }
}