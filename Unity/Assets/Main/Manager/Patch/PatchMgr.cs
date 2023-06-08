using YooAsset;
namespace Ux
{
    public class PatchMgr : Singleton<PatchMgr>
    {
        private bool _isRun = false;
        public bool IsDone { get; private set; }
        /// <summary>
        /// 状态机
        /// </summary>
        StateMachine machine;
        /// <summary>
        /// 开启初始化流程
        /// </summary>
        public void Run(EPlayMode playMode)
        {
            if (_isRun == false)
            {
                IsDone = false;
                _isRun = true;
                machine = StateMachine.CreateByPool();
                // 注意：按照先后顺序添加流程节点
                machine.AddNode(new PatchPatchInit());
                machine.AddNode(new PatchUpdateStaticVersion());
                machine.AddNode(new PatchUpdateManifest());
                machine.AddNode(new PatchCreateDownloader());
                machine.AddNode(new PatchDownloadWebFiles());
                machine.AddNode(new PatchDone());
                if (playMode == EPlayMode.EditorSimulateMode)
                {
                    machine.Enter<PatchDone>();
                }
                else
                {
                    machine.Enter<PatchPatchInit>();
                }
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
            UIMgr.Ins.Hide<PatchView>();
            UIMgr.Ins.OnLowMemory();
            IsDone = true;
        }

        public void Enter<TNode>(object args = null) where TNode : PatchStateNode
        {
            machine.Enter<TNode>(args);
        }
    }
}