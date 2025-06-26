using System;
using UnityEngine;
using YooAsset;
namespace Ux
{
    public class PatchDownloadWebFiles : PatchStateNode
    {
        protected override void OnEnter()
        {
            base.OnEnter();
            BeginDownload();
        }
        void OnDownloadUpdateDataCallback(DownloadUpdateData data)
        {
            PatchMgr.View.OnDownloadUpdateData(data);
        }
        void OnDownloadErrorCallback(DownloadErrorData data)
        {
            Log.Error($"文件下载失败:{data.FileName},error:{data.ErrorInfo}");
            Action callback = () =>
            {
                Application.Quit();
            };
            PatchMgr.View.ShowMessageBox($"更新失败,可能磁盘空间不足！", "确定", callback);
        }

        private void BeginDownload()
        {
            // 注册下载回调
            PatchMgr.Downloader.OnDownloadErrorCallback = OnDownloadErrorCallback;
            PatchMgr.Downloader.OnDownloadUpdateDataCallback = OnDownloadUpdateDataCallback;
            PatchMgr.Downloader.BeginDownload(DownloadComplete);
        }
        private void DownloadComplete(bool succeed)
        {
            if (succeed)
            {                
                PatchMgr.Enter<PatchDone>();
            }
            else
            {
                DownloadCompleteFailed();
            }
        }
        private void DownloadCompleteFailed()
        {
            Action callback = () =>
            {
                Application.Quit();
            };
            PatchMgr.View.ShowMessageBox("资源下载失败，请检查是否磁盘空间不足", "确定", callback);
        }
    }
}