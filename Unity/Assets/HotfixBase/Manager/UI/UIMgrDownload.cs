using System.Collections.Generic;
using System;

namespace Ux
{
    public partial class UIMgr
    {
        /// <summary>
        /// 获取UI依赖的懒加载资源标签列表
        /// 会递归查找父UI的懒加载资源标签
        /// </summary>
        /// <param name="id">UI ID</param>
        /// <returns>懒加载资源标签列表</returns>
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
                var uniqueTags = Pool.Get<HashSet<string>>();
                // 递归遍历父UI链，收集所有懒加载标签
                while (data != null)
                {
                    if (data.Lazyloads != null)
                    {
                        foreach (var lazyload in data.Lazyloads)
                        {
                            if (uniqueTags.Add(lazyload))
                            {
                                lazyloads.Add(lazyload);
                            }
                        }
                    }

                    // 如果没有Tab数据或父ID为0，停止遍历
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

                uniqueTags.Clear();
                Pool.Push(uniqueTags);

                _idLazyloads.Add(id, lazyloads);
            }

            return lazyloads;
        }

        /// <summary>
        /// 检查是否需要下载懒加载资源
        /// 如果需要下载，显示下载对话框
        /// </summary>
        /// <param name="id">UI ID</param>
        /// <param name="param">UI参数</param>
        /// <param name="isAnim">是否播放动画</param>
        /// <returns>如果正在下载返回true，否则返回false</returns>
        bool _CheckDownload(int id, IUIParam param, bool isAnim)
        {
            // 检查是否已有正在进行的下载
            if (_idDownloader.TryGetValue(id, out var download))
            {
                if (download.IsDone)
                {
                    _idDownloader.Remove(id);
                    return false;
                }

                return true;
            }

            // 获取依赖的懒加载资源标签
            var tags = _GetDependenciesLazyload(id);
            if (tags == null || tags.Count == 0) return false;
            
            // 创建下载器
            download = ResMgr.Lazyload.GetDownloaderByTags(tags);
            if (download == null) return false;
            
            Log.Debug($"一共发现了{download.TotalDownloadCount}个资源需要更新下载。");

            // 显示下载对话框
            var dialog = Dialog.Create();
            dialog.SetTitle("下载");
            dialog.SetContent($"一共发现了{download.TotalDownloadCount}个资源需要更新下载。");
            dialog.SetBtn1Title("下载");
            dialog.SetBtn1( () =>
            {
                 _idDownloader.Add(id, download);
                 download.BeginDownload(_DownloadComplete, new DownloadData(id, param, isAnim));
            });
            dialog.Show();
            return true;
        }

        /// <summary>
        /// 下载完成回调
        /// </summary>
        /// <param name="succeed">是否下载成功</param>
        /// <param name="args">附加参数，包含DownloadData</param>
        void _DownloadComplete(bool succeed, object args)
        {
            if (succeed)
            {
                if (args is DownloadData data)
                {
                    // 下载成功后显示UI
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