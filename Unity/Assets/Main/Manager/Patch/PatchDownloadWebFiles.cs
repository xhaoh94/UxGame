using System;
using UnityEngine;
namespace Ux
{
    public class PatchDownloadWebFiles : PatchStateNode
    {
        protected override void OnEnter(object args)
        {
            BeginDownload((Downloader)args);
        }
        protected override void OnUpdate()
        {
        }
        protected override void OnExit()
        {
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
            PatchMgr.OnWebFileDownloadFailed(fileName, error);
        }

        private void BeginDownload(Downloader downloader)
        {
            // 注册下载回调
            downloader.OnDownloadErrorCallback = OnDownloadErrorCallback;
            downloader.OnDownloadProgressCallback = OnDownloadProgressCallback;
            downloader.BeginDownload(DownloadComplete);
        }
        private void DownloadComplete(bool succeed)
        {
            if (succeed)
            {
                PatchMgr.Enter<PatchDone>();
            }
            else
            {
                PatchMgr.DownloadCompleteFailed();
            }
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