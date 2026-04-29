using System.Collections.Generic;

namespace Ux
{
    public partial class ResMgr
    {
        public static readonly ResLazyload Lazyload = new ResLazyload();
        public class ResLazyload
        {
            readonly HashSet<string> _loadedAssets = new HashSet<string>();

            public Downloader GetDownloaderByTags(IList<string> tags)
            {
                if (tags == null || tags.Count == 0) return null;

                int count = 0;
                for (int i = 0; i < tags.Count; i++)
                {
                    if (!_loadedAssets.Contains(tags[i]))
                    {
                        count++;
                    }
                }
                if (count == 0) return null;

                string[] list = new string[count];
                int index = 0;
                for (int i = 0; i < tags.Count; i++)
                {
                    var tag = tags[i];
                    if (!_loadedAssets.Contains(tag))
                    {
                        list[index++] = tag;
                    }
                }

                var download = new Downloader(list);
                if (download.TotalDownloadCount != 0) return download;
                for (int i = 0; i < list.Length; i++)
                {
                    _loadedAssets.Add(list[i]);
                }
                return null;
            }
        }
    }
}
