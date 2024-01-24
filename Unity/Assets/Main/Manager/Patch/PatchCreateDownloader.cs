using System;

namespace Ux
{
    public class PatchCreateDownloader : PatchStateNode
    {
        protected override void OnEnter(object args)
        {
            CreateDownloader();
        }
        void CreateDownloader()
        {
            Log.Debug("创建补丁下载器.");
            var tags = new string[] { "builtin", "preload" };
            var downloader = new Downloader(tags);
            if (downloader.TotalDownloadCount > 0)
            {
                Log.Debug($"一共发现了{downloader.TotalDownloadCount}个资源需要更新下载。");
                OnFoundUpdateFiles(downloader);
            }
            else
            {
                Log.Debug("没有发现需要下载的资源");
                PatchMgr.Enter<PatchDone>();
            }
        }

        void OnFoundUpdateFiles(Downloader downloader)
        {
            string totalSizeMB = downloader.TotalSizeMB.ToString("f1");
            int totalCnt = downloader.TotalDownloadCount;

            Action callback = () =>
            {
                PatchMgr.Ins.Enter<PatchDownloadWebFiles>(downloader);
            };
            PatchMgr.View.ShowMessageBox($"下载更新{totalCnt}文件，总大小{totalSizeMB}MB", "更新", callback);
        }
    }
}