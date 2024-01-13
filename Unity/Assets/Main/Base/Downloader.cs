using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YooAsset;
using static YooAsset.DownloaderOperation;

namespace Ux
{
    public class Downloader
    {
        readonly List<DownloaderOperation> _handles = new List<DownloaderOperation>();
        public Downloader(string[] tags)
        {
            ResMgr.Ins.ForEachPackage(x =>
            {                
                _handles.Add(x.Package.CreateResourceDownloader(tags, Global.DownloadingMaxNum, Global.FailedTryAgain));
            });
        }

        /// <summary>
        /// 当下载进度发生变化
        /// </summary>
        public OnDownloadProgress OnDownloadProgressCallback { set; get; }

        /// <summary>
        /// 当某个文件下载失败
        /// </summary>
        public OnDownloadError OnDownloadErrorCallback { set; get; }

        /// <summary>
        /// 下载文件总数量
        /// </summary>
        public int TotalDownloadCount
        {
            get
            {
                int totalDownloadCount = 0;
                foreach (var handle in _handles)
                {
                    totalDownloadCount += handle.TotalDownloadCount;
                }
                return totalDownloadCount;
            }
        }
        /// <summary>
        /// 下载文件的总大小
        /// </summary>
        public long TotalDownloadBytes
        {
            get
            {
                long totalDownloadBytes = 0;
                foreach (var handle in _handles)
                {
                    totalDownloadBytes += handle.TotalDownloadBytes;
                }
                return totalDownloadBytes;
            }
        }
        /// 当前已经完成的下载总数量
        /// </summary>
        public int CurrentDownloadCount
        {
            get
            {
                int currentDownloadCount = 0;
                foreach (var handle in _handles)
                {
                    currentDownloadCount += handle.CurrentDownloadCount;
                }
                return currentDownloadCount;
            }
        }

        /// <summary>
        /// 当前已经完成的下载总大小
        /// </summary>
        public long CurrentDownloadBytes
        {
            get
            {
                long currentDownloadBytes = 0;
                foreach (var handle in _handles)
                {
                    currentDownloadBytes += handle.CurrentDownloadBytes;
                }
                return currentDownloadBytes;
            }
        }
        /// <summary>
        /// 下载文件的总大小
        /// </summary>
        public float TotalSizeMB
        {
            get
            {
                float sizeMB = TotalDownloadBytes / 1048576f;
                sizeMB = Mathf.Clamp(sizeMB, 0.1f, float.MaxValue);
                return sizeMB;
            }
        }

        /// <summary>
        /// 是否已经开始下载
        /// </summary>
        public bool IsBeginDownload { get; private set; } = false;

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsDone
        {
            get
            {
                if (!IsBeginDownload) return false;
                foreach (var handle in _handles)
                {
                    if (!handle.IsDone) return false;
                }
                return true;
            }
        }

        private static string GetRegularPath(string path)
        {
            return path.Replace('\\', '/').Replace("\\", "/"); //替换为Linux路径格式
        }

        /// <summary>
        /// 检查磁盘空间
        /// </summary>
        /// <returns></returns>
        public bool CheckDiskSpace()
        {
            if (_handles == null) return false;
            // 检测磁盘空间是否不足
            bool b = true;
            var total = TotalDownloadBytes;
#if UNITY_EDITOR
            string directory = Path.GetDirectoryName(UnityEngine.Application.dataPath);
            string projectPath = GetRegularPath(directory);
            string tempPath = $"{projectPath}/_$temp_check_disk_space.temp";
#else
            string tempPath = $"{UnityEngine.Application.persistentDataPath}/_$temp_check_disk_space.temp";
#endif
            try
            {
                using (var tempStream = File.Create(tempPath))
                {
                    while (total > 0)
                    {
                        if (total > int.MaxValue)
                        {
                            tempStream.Write(new byte[int.MaxValue], 0, int.MaxValue);
                            total -= int.MaxValue;
                        }
                        else
                        {
                            tempStream.Write(new byte[total], 0, (int)total);
                            break;
                        }
                    }
                    tempStream.Flush();
                    tempStream.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex.ToString());
                b = false;
            }
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            return b;
        }

        /// <summary>
        /// 暂停下载
        /// </summary>
        public void PauseDownload()
        {
            foreach (var handle in _handles)
            {
                handle.PauseDownload();
            }
        }
        /// <summary>
        /// 恢复下载
        /// </summary>
        public void ResumeDownload()
        {
            foreach (var handle in _handles)
            {
                handle.ResumeDownload();
            }
        }

        /// <summary>
		/// 取消下载
		/// </summary>
		public void CancelDownload()
        {
            foreach (var handle in _handles)
            {
                handle.CancelDownload();
            }
        }
        public void BeginDownload(Action<bool> end = null)
        {
            if (IsBeginDownload) return;
            // 注意：需要在下载前检测磁盘空间不足
            if (!CheckDiskSpace())
            {
                end?.Invoke(false);
                return;
            }
            _Download(end).Forget();
        }
        public void BeginDownload(Action<bool, object> end, object args)
        {
            // 注意：需要在下载前检测磁盘空间不足
            if (!CheckDiskSpace())
            {
                end?.Invoke(false, args);
                return;
            }
            _Download(end, args).Forget();
        }
        async UniTaskVoid _Download(Action<bool> end)
        {
            var succeed = await _Download();
            end?.Invoke(succeed);
        }
        async UniTaskVoid _Download(Action<bool, object> end, object args)
        {
            var succeed = await _Download();
            end?.Invoke(succeed, args);
        }
        async UniTask<bool> _Download()
        {
            if (_handles == null) return false;
            IsBeginDownload = true;
            foreach (var handle in _handles)
            {
                handle.OnDownloadErrorCallback = OnDownloadErrorCallback;
                handle.OnDownloadProgressCallback = _OnDownloadProgressCallback;
                handle.BeginDownload();
                await handle.ToUniTask();
                // 检测下载结果
                if (handle.Status != EOperationStatus.Succeed)
                    return false;
            }
            return true;
        }

        void _OnDownloadProgressCallback(int totalDownloadCount, int currentDownloadCount, long totalDownloadSizeBytes, long currentDownloadSizeBytes)
        {
            OnDownloadProgressCallback?.Invoke(TotalDownloadCount, CurrentDownloadCount, TotalDownloadBytes, CurrentDownloadBytes);
        }

    }
}