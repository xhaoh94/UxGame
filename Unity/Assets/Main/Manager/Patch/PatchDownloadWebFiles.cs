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
            EventMgr.Ins.Send(MainEventType.DOWNLOAD_PROGRESS_UPDATE, msg);
        }
        void OnDownloadErrorCallback(string fileName, string error)
        {
            Log.Error($"文件下载失败:{fileName},error:{error}");
            EventMgr.Ins.Send(MainEventType.WEBFILE_DOWNLOAD_FAILED, fileName, error, this);
        }

        private void BeginDownload(Downloader downloader)
        {
            // 注册下载回调
            downloader.OnDownloadErrorCallback = OnDownloadErrorCallback;
            downloader.OnDownloadProgressCallback = OnDownloadProgressCallback;
            downloader.Download(DownloadComplete, OnDownloadFail);
        }
        private void DownloadComplete()
        {
            PatchMgr.Enter<PatchDone>();
        }
        private void OnDownloadFail()
        {
            Action callback = () =>
            {
                Application.Quit();
            };
            UIMgr.Dialog.SingleBtn("提示", $"磁盘空间不足", "确定", callback);
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