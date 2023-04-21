using System;
using FairyGUI;
using UnityEngine;
using EventType = Ux.Main.EventType;

namespace Ux
{
    [UI]
    [Package("Patch")]
    public partial class PatchView : UIView
    {
        protected override UILayer Layer => UILayer.Normal;

        protected override string PkgName => "Patch";

        protected override string ResName => "PatchView";

        public override bool IsDestroy => true;

        GProgressBar bar;
        GTextField txt;

        protected override void OnShow(object param)
        {
            base.OnShow(param);
            txt.text = "游戏初始化";
            bar.max = 1f;
            bar.min = 0f;
            bar.visible = false;
        }
        [Main.Evt(EventType.PATCH_STATE_CHANGED)]
        void OnStateChanged(string node)
        {
            switch (node)
            {
                case nameof(PatchUpdateStaticVersion):
                    txt.text = "获取资源版本.";
                    break;
                case nameof(PatchUpdateManifest):
                    txt.text = "获取补丁清单.";
                    break;
                case nameof(PatchCreateDownloader):
                    txt.text = "获取更新文件.";
                    break;
                case nameof(PatchDownloadWebFiles):
                    txt.text = "下载补丁清单.";
                    break;
                case nameof(PatchDone):
                    txt.text = "下载完成.";
                    break;
            }
        }

        [Main.Evt(EventType.FOUND_UPDATE_FILES)]
        void OnFoundUpdateFiles(Downloader downloader)
        {
            Action callback = () =>
            {
                PatchMgr.Instance.Enter<PatchDownloadWebFiles>(downloader);
            };
            string totalSizeMB = downloader.TotalSizeMB.ToString("f1");
            int totalCnt = downloader.TotalDownloadCount;
            UIMgr.Dialog.SingleBtn("提示", $"下载更新{totalCnt}文件，总大小{totalSizeMB}MB", "确定", callback);
        }

        [Main.Evt(EventType.STATIC_VERSION_UPDATE_FAILED)]
        void OnStaticVersionUpdateFailed()
        {
            Action callback = () =>
            {
                PatchMgr.Instance.Enter<PatchUpdateStaticVersion>();
            };
            UIMgr.Dialog.SingleBtn("提示", $"获取资源版本失败，请检测网络状态。", "确定", callback);
        }

        [Main.Evt(EventType.PATCH_MANIFEST_UPDATE_FAILED)]
        void OnPatchManifestUpdateFailed()
        {
            Action callback = () =>
            {
                PatchMgr.Instance.Enter<PatchUpdateManifest>();
            };
            UIMgr.Dialog.SingleBtn("提示", $"获取补丁清单失败，请检测网络状态。", "确定", callback);
        }

        [Main.Evt(EventType.WEBFILE_DOWNLOAD_FAILED)]
        void OnWebFileDownloadFailed(string fileName, string error)
        {
            Action callback = () =>
            {
                Application.Quit();
            };
            UIMgr.Dialog.SingleBtn("提示", $"更新失败,可能磁盘空间不足！", "确定", callback);
        }

        [Main.Evt(EventType.DOWNLOAD_PROGRESS_UPDATE)]
        void OnDownloadProgressUpdate(DownloadProgressUpdate message)
        {
            if (!bar.visible) bar.visible = true;
            bar.value = (double)message.CurrentDownloadCount / message.TotalDownloadCount;
            string currentSizeMB = (message.CurrentDownloadSizeBytes / 1048576f).ToString("f1");
            string totalSizeMB = (message.TotalDownloadSizeBytes / 1048576f).ToString("f1");
            txt.text = $"{message.CurrentDownloadCount}/{message.TotalDownloadCount} {currentSizeMB}MB/{totalSizeMB}MB";
        }
    }
}
