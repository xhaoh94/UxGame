using System.Collections.Generic;
using System;

namespace Ux
{
    public partial class UIMgr
    {
        List<string> _GetDependenciesLazyload(int id)
        {
            if (id == 0) return null;
            if (!_idLazyloads.TryGetValue(id, out var lazyloads))
            {
                var data = GetUIData(id);
                if (data == null)
                {
                    _idLazyloads.Add(id, null);
                    return null;
                }

                lazyloads = new List<string>();
                while (data != null)
                {
                    if (data.Lazyloads != null)
                    {
                        foreach (var lazyload in data.Lazyloads)
                        {
                            if (!lazyloads.Contains(lazyload))
                            {
                                lazyloads.Add(lazyload);
                            }
                        }
                    }

                    if (data.TabData == null)
                    {
                        break;
                    }

                    if (data.TabData.PID == 0)
                    {
                        break;
                    }

                    data = GetUIData(data.TabData.PID);
                }

                _idLazyloads.Add(id, lazyloads);
            }

            return lazyloads;
        }

        bool _CheckDownload(int id, IUIParam param, bool isAnim)
        {
            if (_idDownloader.TryGetValue(id, out var download))
            {
                if (download.IsDone)
                {
                    _idDownloader.Remove(id);
                    return false;
                }

                return true;
            }

            var tags = _GetDependenciesLazyload(id);
            if (tags == null || tags.Count == 0) return false;
            download = ResMgr.Lazyload.GetDownloaderByTags(tags);
            if (download == null) return false;
            Log.Debug($"一共发现了{download.TotalDownloadCount}个资源需要更新下载。");


            Dialog.Get()
            .WithTitle("下载")
            .WithContent($"一共发现了{download.TotalDownloadCount}个资源需要更新下载。")
            .WithBtn1("下载", () =>
            {
                 _idDownloader.Add(id, download);
                 download.BeginDownload(_DownloadComplete, new DownloadData(id, param, isAnim));
            });
            return true;
        }

        void _DownloadComplete(bool succeed, object args)
        {
            if (succeed)
            {
                if (args is DownloadData data)
                {
                    Create(data.UIID).SetParam(data.Param).SetAnim(data.IsAnim).Show();                    
                }
            }
            else
            {
                Log.Error("下载懒加载资源失败");
            }
        }
    }
}