using System;
using UnityEngine;
using YooAsset;
namespace Ux
{
    public class PatchMgr : Singleton<PatchMgr>
    {
        private bool _isRun = false;
        public bool IsDone { get; private set; }
        public PatchView View { get; private set; }
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

                //machine.AddNode(new PatchPatchInit());
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
                    View = PatchView.Show();
                    machine.Enter<PatchUpdateStaticVersion>();
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
            if (View != null)
            {
                UnityEngine.Object.Destroy(View.gameObject);
            }
            IsDone = true;
        }

        public void Enter<TNode>(object args = null) where TNode : PatchStateNode
        {
            machine.Enter<TNode>(args);
        }

        public void OnStaticVersionUpdateFailed()
        {
            Action callback = () =>
            {
                PatchMgr.Ins.Enter<PatchUpdateStaticVersion>();
            };
            View.ShowTip($"获取资源版本失败，请检测网络状态。", "确定", callback);
        }

        public void OnPatchManifestUpdateFailed()
        {
            Action callback = () =>
            {
                PatchMgr.Ins.Enter<PatchUpdateManifest>();
            };
            View.ShowTip($"获取补丁清单失败，请检测网络状态。", "确定", callback);
        }

        public void OnWebFileDownloadFailed(string fileName, string error)
        {
            Action callback = () =>
            {
                Application.Quit();
            };
            View.ShowTip($"更新失败,可能磁盘空间不足！", "确定", callback);
        }
    }
}