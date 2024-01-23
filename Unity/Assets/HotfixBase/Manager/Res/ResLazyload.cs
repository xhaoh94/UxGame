using System.Collections.Generic;
using System.Linq;

namespace Ux
{
    public partial class ResMgr
    {
        public static readonly ResLazyload Lazyload = new ResLazyload();
    }
    public class ResLazyload
    {
        readonly HashSet<string> _loadedAssets = new HashSet<string>();
        public ResLazyload()
        {
        }

        public Downloader GetDownloaderByTags(IList<string> tags)
        {
            var list = tags.Where(tag => !_loadedAssets.Contains(tag)).ToArray();
            if (list.Length <= 0) return null;
            var download = new Downloader(list);
            if (download.TotalDownloadCount != 0) return download;
            foreach (var tag in list)
            {
                _loadedAssets.Add(tag);
            }
            return null;
        }

    }
}
