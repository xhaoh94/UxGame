using System.Collections.Generic;
using System.Linq;

namespace Ux
{
    public class ResLazyload
    {
        readonly HashSet<string> _loadedAssets = new HashSet<string>();
        public ResLazyload()
        {
        }

        public Downloader GetDownloaderByTags(string[] tags)
        {
            var list = tags.Where(tag => !_loadedAssets.Contains(tag)).ToList();
            if (list.Count <= 0) return null;            
            var download = new Downloader(list.ToArray());
            if (download.TotalDownloadCount != 0) return download;
            foreach (var tag in list)
            {
                _loadedAssets.Add(tag);
            }
            return null;
        }

    }
}
