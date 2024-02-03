using System;
using UnityEngine;
namespace Ux
{
    public class PatchDownloadWebFiles : PatchStateNode
    {
        protected override void OnEnter()
        {
            base.OnEnter();
            BeginDownload();
        }
        void OnDownloadProgressCallback(int totalDownloadCount, int currentDownloadCount, long totalDownloadSizeBytes, long currentDownloadSizeBytes)
        {
            DownloadProgressUpdate msg = new DownloadProgressUpdate();
            msg.TotalDownloadCount = totalDownloadCount;
            msg.CurrentDownloadCount = currentDownloadCount;
            msg.TotalDownloadSizeBytes = totalDownloadSizeBytes;
            msg.CurrentDownloadSizeBytes = currentDownloadSizeBytes;
            PatchMgr.View.OnDownloadProgressUpdate(msg);
        }
        void OnDownloadErrorCallback(string fileName, string error)
        {
            Log.Error($"文件下载失败:{fileName},error:{error}");
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
            PatchMgr.Downloader.OnDownloadProgressCallback = OnDownloadProgressCallback;
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
    public struct DownloadProgressUpdate
    {
        public int TotalDownloadCount;
        public int CurrentDownloadCount;
        public long TotalDownloadSizeBytes;
        public long CurrentDownloadSizeBytes;
    }
}