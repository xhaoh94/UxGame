using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Timeline
{
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
        public enum PlayMode
        {
            Loop,
            Once,
        }
        public static TimelineWindow wnd;
        [MenuItem("UxGame/工具/时间轴", false, 521)]
        public static void ShowExample()
        {
            wnd = GetWindow<TimelineWindow>();
            wnd.titleContent = new GUIContent("时间轴");
        }
        static string Path = "Assets/Data/Res/Timeline";

        double _lastTime;
        float _playTime;
        TLEntity _entity;
        List<int> _frameSelects = new List<int>() { 30, 60 };

        public void CreateGUI()
        {
            IsPlaying = false;
            SaveAssets = _SaveAssets;
            RefreshEntity = _RefreshEntity;
            MarkerMove = _MarkerMove;
            ResetActionMap();

            CreateChildren();
            root.style.flexGrow = 1f;
            rootVisualElement.Add(root);
            clipView.Init();

            ofEntity.objectType = typeof(GameObject);
            ofTimeline.objectType = typeof(TimelineAsset);

            var framePopupField = new PopupField<int>(_frameSelects, 0);
            framePopupField.label = "帧率";
            framePopupField.RegisterValueChangedCallback(evt =>
            {
                if (Asset.SetFrameRate(evt.newValue))
                {
                    TimelineMgr.Ins.FrameRate = evt.newValue;
                    SaveAssets();
                    RefreshClip();
                }
            });
            framePopupField.value = (int)TimelineMgr.Ins.FrameRate;
            framePopupField.labelElement.style.minWidth = 30;
            frameContent.Add(framePopupField);

            playMode.Init(PlayMode.Once);
            playMode.labelElement.style.minWidth = 30;

            createView.style.display = DisplayStyle.None;

            inputPath.SetValueWithoutNotify(Path);

            _OnOfEntityChanged(ChangeEvent<Object>.GetPooled(null, SettingTools.GetPlayerPrefs<GameObject>("timeline_entity")));
            _OnOfTimelineChanged(ChangeEvent<Object>.GetPooled(null, SettingTools.GetPlayerPrefs<TimelineAsset>("timeline_asset")));
            //clipView.SetNowFrame(1, TimelineMgr.Ins.FrameRate);
        }

        bool keyCtrl = false;
        bool keyS = false;
        bool save = false;
        private void OnGUI()
        {
            switch (Event.current.type)
            {
                case UnityEngine.EventType.KeyDown:
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.LeftControl:
                            keyCtrl = true;
                            break;
                        case KeyCode.S:
                            keyS = true;
                            break;
                    }
                    break;
                case UnityEngine.EventType.KeyUp:
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.LeftControl:
                            keyCtrl = false;
                            save = false;
                            break;
                        case KeyCode.S:
                            keyS = false;
                            save = false;
                            break;
                    }
                    break;
            }

            if (keyCtrl && keyS && !save)
            {
                Log.Debug("保存");
                save = true;
                SaveAssets();
            }
        }

        void OnPlay()
        {
            if (IsPlaying)
            {
                var deltaTime = (float)(EditorApplication.timeSinceStartup - _lastTime);
                _lastTime = EditorApplication.timeSinceStartup;
                _playTime += deltaTime;
                var frame = TimelineMgr.Ins.TimeConverFrame(_playTime);
                clipView.SetNowFrame(frame);
                if (Timeline.Current.IsDone)
                {
                    switch (playMode.value)
                    {
                        case PlayMode.Once:
                            _OnBtnPauseClick();
                            break;
                        case PlayMode.Loop:
                            _ResetPlay();
                            break;
                    }
                }
            }
        }
        void _ResetPlay()
        {
            _playTime = 0;
            _lastTime = EditorApplication.timeSinceStartup;
            clipView.SetNowFrame(0);
        }

        private void OnDestroy()
        {
            if (IsPlaying)
            {
                IsPlaying = false;
                UnityEditor.EditorApplication.update -= OnPlay;
            }
            if (_entity != null)
            {
                _entity.Destroy();
                _entity = null;
            }
        }
        partial void _OnBtnLastFrameClick()
        {
            if (!IsValid()) return;
            if (clipView.CurFrame > 0)
            {
                clipView.SetNowFrame(clipView.CurFrame - 1);
            }
        }
        partial void _OnBtnNextFrameClick()
        {
            if (!IsValid()) return;
            clipView.SetNowFrame(clipView.CurFrame + 1);
        }

        partial void _OnBtnPlayClick()
        {
            if (!IsValid()) return;
            _ResetPlay();
            UnityEditor.EditorApplication.update += OnPlay;
            IsPlaying = true;
        }
        partial void _OnBtnPauseClick()
        {
            if (!IsPlaying) return;
            UnityEditor.EditorApplication.update -= OnPlay;
            IsPlaying = false;
        }

        partial void _OnOfEntityChanged(ChangeEvent<Object> e)
        {
            _entity?.Destroy();
            _entity = null;
            ofEntity.SetValueWithoutNotify(e.newValue);
            if (e.newValue is GameObject obj)
            {
                if (obj == null)
                {
                    return;
                }
                var model = Instantiate(obj);

                _entity?.Destroy();
                _entity = Entity.Create<TLEntity>();
                _entity.Link(model);
                Timeline = _entity.Add<TimelineComponent>();

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
                Asset = asset;
                if (Asset.SetFrameRate(TimelineMgr.Ins.FrameRate))
                {
                    SaveAssets();
                }
                RefreshEntity();
                RefreshView();
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

        void _SaveAssets()
        {
            if (Asset == null) return;
            EditorUtility.SetDirty(Asset);
            AssetDatabase.SaveAssets();
        }
        void _RefreshEntity()
        {
            if (Asset == null) return;
            if (Timeline == null) return;
            Timeline.Play(Asset);
        }
        void _MarkerMove(int frame,int oldFrame)
        {
            if (Timeline == null) return;
            Timeline.Evaluate(frame, oldFrame);
        }
    }

}

