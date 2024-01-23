using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using YooAsset;

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

        static AssetHandle handle;
        static GameObject view;
        public static PatchView Show()
        {
            handle = YooMgr.Ins.GetPackage(YooType.Main).Package.LoadAssetSync("PatchView");
            view = handle.InstantiateSync();
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
                es.AddComponent<BaseInput>();
                es.SetParent(view.transform);
            }
            return view.GetComponent<PatchView>();
        }
        public static void Hide()
        {
            if (handle != null)
            {
                handle.Dispose();
                handle = null;
            }
            UnityEngine.Object.Destroy(view);
            view = null;
        }

        private void Awake()
        {
            txtStatus.text = "游戏初始化";
            barHotfix.maxValue = 1f;
            barHotfix.minValue = 0f;
            barHotfix.gameObject.Visable(false);
            goTip.Visable(false);
        }


        public void OnStatusChanged(string node)
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

        public void ShowTip(string content, string btnTitle, Action callback, Action closeCb = null)
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
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(() =>
            {
                closeCb?.Invoke();
                goTip.Visable(false);
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
