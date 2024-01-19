using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Ux
{
    public partial class PatchView : MonoBehaviour
    {
        [SerializeField] Slider barHotfix;
        [SerializeField] Text txtStatus;

        [SerializeField] GameObject goTip;
        [SerializeField] Button btnClose;
        [SerializeField] Button btnHotfix;
        [SerializeField] Text txtBtnHotfix;
        [SerializeField] Text txtHotfix;

        public static PatchView Show()
        {
            var go = ResMgr.Ins.LoadAsset<GameObject>("PatchView", ResType.Main);
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
                es.AddComponent<BaseInput>();
                es.SetParent(go.transform);
            }
            return go.GetComponent<PatchView>();
        }

        private void Awake()
        {
            txtStatus.text = "游戏初始化";
            barHotfix.maxValue = 1f;
            barHotfix.minValue = 0f;
            barHotfix.gameObject.Visable(false);
            goTip.Visable(false);
        }

        public void OnStateChanged(string node)
        {
            switch (node)
            {
                case nameof(PatchUpdateStaticVersion):
                    txtStatus.text = "获取资源版本.";
                    break;
                case nameof(PatchUpdateManifest):
                    txtStatus.text = "获取补丁清单.";
                    break;
                case nameof(PatchCreateDownloader):
                    txtStatus.text = "获取更新文件.";
                    break;
                case nameof(PatchDownloadWebFiles):
                    txtStatus.text = "下载补丁清单.";
                    break;
                case nameof(PatchDone):
                    txtStatus.text = "下载完成.";
                    break;
            }
        }

        public void ShowTip(string content, string btnTitle, Action callback)
        {
            txtBtnHotfix.text = btnTitle;
            txtHotfix.text = content;
            goTip.Visable(true);
            btnHotfix.onClick.RemoveAllListeners();
            btnHotfix.onClick.AddListener(() =>
            {
                callback?.Invoke();
                goTip.Visable(false);
            });
        }

        public void OnFoundUpdateFiles(Downloader downloader)
        {
            string totalSizeMB = downloader.TotalSizeMB.ToString("f1");
            int totalCnt = downloader.TotalDownloadCount;

            ShowTip($"下载更新{totalCnt}文件，总大小{totalSizeMB}MB", "更新", () =>
            {
                PatchMgr.Ins.Enter<PatchDownloadWebFiles>(downloader);
            });
        }

        public void OnDownloadProgressUpdate(DownloadProgressUpdate message)
        {
            barHotfix.gameObject.Visable(true);
            barHotfix.value = (float)message.CurrentDownloadCount / message.TotalDownloadCount;
            string currentSizeMB = (message.CurrentDownloadSizeBytes / 1048576f).ToString("f1");
            string totalSizeMB = (message.TotalDownloadSizeBytes / 1048576f).ToString("f1");
            txtStatus.text = $"{message.CurrentDownloadCount}/{message.TotalDownloadCount} {currentSizeMB}MB/{totalSizeMB}MB";
        }


    }
}
