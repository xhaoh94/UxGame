using System;

namespace Ux
{
    public class PatchCreateDownloader : PatchStateNode
    {
        protected override void OnEnter()
        {
            base.OnEnter();
            CreateDownloader();
        }
        void CreateDownloader()
        {
            Log.Debug("创建补丁下载器.");
            var tags = new string[] { "builtin", "preload" };
            PatchMgr.Downloader = new Downloader(tags);
            if (PatchMgr.Downloader.TotalDownloadCount > 0)
            {
                Log.Debug($"一共发现了{PatchMgr.Downloader.TotalDownloadCount}个资源需要更新下载。");
                OnFoundUpdateFiles();
            }
            else
            {
                Log.Debug("没有发现需要下载的资源");                
                PatchMgr.Enter<PatchDone>();
            }
        }

        void OnFoundUpdateFiles()
        {
            string totalSizeMB = PatchMgr.Downloader.TotalSizeMB.ToString("f1");
            int totalCnt = PatchMgr.Downloader.TotalDownloadCount;

            Action callback = () =>
            {                
                PatchMgr.Ins.Enter<PatchDownloadWebFiles>();
            };
            PatchMgr.View.ShowMessageBox($"下载更新{totalCnt}文件，总大小{totalSizeMB}MB", "更新", callback);
        }
    }
}