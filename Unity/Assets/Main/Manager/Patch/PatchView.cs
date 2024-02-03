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

        [SerializeField] GameObject messageBox;
        [SerializeField] Button btnClose;
        [SerializeField] Button btnHotfix;
        [SerializeField] Text txtBtnHotfix;
        [SerializeField] Text txtHotfix;

        static GameObject view;
        public static PatchView Show()
        {            
            if (GameMain.Ins.PlayMode == EPlayMode.EditorSimulateMode)
            {
                return null;
            }
            view = Instantiate(Resources.Load<GameObject>("Patch/PatchView"));
            DontDestroyOnLoad(view);
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
            if (view != null)
            {
                UnityEngine.Object.Destroy(view);
                view = null;
            }
        }

        private void Awake()
        {
            txtStatus.text = "游戏初始化";
            barHotfix.maxValue = 1f;
            barHotfix.minValue = 0f;
            barHotfix.gameObject.Visable(false);
            messageBox.Visable(false);
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

        public void ShowMessageBox(string content, string btnTitle, Action callback)
        {
            txtBtnHotfix.text = btnTitle;
            txtHotfix.text = content;
            messageBox.Visable(true);
            btnHotfix.onClick.RemoveAllListeners();
            btnHotfix.onClick.AddListener(() =>
            {
                callback?.Invoke();
                messageBox.Visable(false);
            });
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(() =>
            {                
                messageBox.Visable(false);
                Application.Quit();
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
