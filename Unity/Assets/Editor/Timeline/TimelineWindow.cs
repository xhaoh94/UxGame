using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Timeline
{
    public static class Timeline
    {
        public static VisualElement ClipContent { get; set; }
        public static System.Action UpdateMarkerPos { get; set; }
        public static System.Func<int, float> GetPositionByFrame { get; set; }
        public static System.Func<int> GetFrameByMousePosition {  get; set; }
        public static System.Action<int> MarkerMove { get; set; }
        public static TimelineAsset Asset { get; set; }
        public static System.Action SaveAssets { get; set; }
        public static System.Action RefreshEntity { get; set; }
    }
    public class TLEntity : Entity
    {
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Object.DestroyImmediate(Viewer.gameObject);
        }
    }
    public partial class TimelineWindow : EditorWindow
    {
        static TimelineWindow wnd;
        [MenuItem("UxGame/时间轴")]
        public static void ShowExample()
        {
            wnd = GetWindow<TimelineWindow>();
            wnd.titleContent = new GUIContent("时间轴");
        }

        static string Path = "Assets/Data/Res/Timeline";

        public TimelineAsset asset => ofTimeline.value as TimelineAsset;
        TLEntity entity;

        bool _isPlaying;

        public void CreateGUI()
        {
            CreateChildren();
            root.style.flexGrow = 1f;
            rootVisualElement.Add(root);            

            ofEntity.objectType = typeof(GameObject);
            ofTimeline.objectType = typeof(TimelineAsset);


            createView.style.display = DisplayStyle.None;

            inputPath.SetValueWithoutNotify(Path);

            _OnOfEntityChanged(ChangeEvent<Object>.GetPooled(null, SettingTools.GetPlayerPrefs<GameObject>("timeline_entity")));
            _OnOfTimelineChanged(ChangeEvent<Object>.GetPooled(null, SettingTools.GetPlayerPrefs<TimelineAsset>("timeline_asset")));

            Timeline.SaveAssets = SaveAssets;
            Timeline.RefreshEntity = RefreshEntity;
            Timeline.MarkerMove = SetFrame;
        }


        private void OnDestroy()
        {
            if (entity != null)
            {
                entity.Destroy();
                entity = null;
            }
        }

        partial void _OnOfEntityChanged(ChangeEvent<Object> e)
        {
            entity?.Destroy();
            entity = null;
            ofEntity.SetValueWithoutNotify(e.newValue);
            if (e.newValue is GameObject obj)
            {
                if (obj == null)
                {
                    return;
                }
                var model = Instantiate(obj);

                entity?.Destroy();
                entity = Entity.Create<TLEntity>();
                entity.Link(model);
                entity.Add<TimelineComponent>();

                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long _))
                {
                    SettingTools.SavePlayerPrefs("timeline_entity", guid);
                }
                RefreshEntity();
            }
        }

        partial void _OnOfTimelineChanged(ChangeEvent<Object> e)
        {
            ofTimeline.SetValueWithoutNotify(e.newValue);
            if (e.newValue is TimelineAsset asset)
            {
                if (asset == null) { return; }
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long _))
                {
                    SettingTools.SavePlayerPrefs("timeline_asset", guid);
                }
                RefreshEntity();
                trackView.RefreshView();
            }
        }

        partial void _OnBtnCreateClick()
        {
            if (createView.style.display == DisplayStyle.None)
            {
                createView.style.display = DisplayStyle.Flex;
            }
            else
            {
                createView.style.display = DisplayStyle.None;
            }
        }

        partial void _OnInputPathChanged(ChangeEvent<string> e)
        {
            var temPath = EditorUtility.OpenFolderPanel("请选择生成路径", Path, "");
            if (temPath.Length == 0)
            {
                return;
            }

            if (!Directory.Exists(temPath))
            {
                EditorUtility.DisplayDialog("错误", "路径不存在!", "ok");
                return;
            }
            inputPath.SetValueWithoutNotify(temPath);
        }
        partial void _OnBtnOkClick()
        {
            if (string.IsNullOrEmpty(inputName.text))
            {
                Log.Error("名字不能为空");
                return;
            }
            var assetName = $"{inputPath.text}/{inputName.text}.asset";
            if (File.Exists(assetName))
            {
                Log.Error("重复创建相同的TimelineAsset");
                return;
            }

            createView.style.display = DisplayStyle.None;
            var asset = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(asset, assetName);
            ofTimeline.SetValueWithoutNotify(asset);
        }

        void SaveAssets()
        {
            if (asset != null)
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
        }
        void RefreshEntity()
        {
            if (asset == null) return;
            entity?.Get<TimelineComponent>()?.Play(asset);
        }
        public void SetFrame(int frame)
        {
            var time = TimelineMgr.Ins.FrameConvertTime(frame);
            entity?.Get<TimelineComponent>()?.Evaluate(time);
        }
    }

}

