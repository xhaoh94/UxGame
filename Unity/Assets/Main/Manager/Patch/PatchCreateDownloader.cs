using EventType = Ux.Main.EventType;
namespace Ux
{
    public class PatchCreateDownloader : PatchStateNode
    {
        protected override void OnEnter(object args)
        {
            CreateDownloader();
        }
        protected override void OnUpdate()
        {
        }
        protected override void OnExit()
        {
        }

        void CreateDownloader()
        {
            Log.Debug("创建补丁下载器.");
            var tags = new string[] { "builtin", "preload" };
            var downloader = new Downloader(tags);
            if (downloader.TotalDownloadCount > 0)
            {
                Log.Debug($"一共发现了{downloader.TotalDownloadCount}个资源需要更新下载。");                
                EventMgr.Ins.Send(EventType.FOUND_UPDATE_FILES, downloader);
            }
            else
            {
                Log.Debug("没有发现需要下载的资源");
                PatchMgr.Enter<PatchDone>();
            }
        }
    }
}