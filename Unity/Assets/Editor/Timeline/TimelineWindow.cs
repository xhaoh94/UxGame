using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace Ux.Editor.Timeline
{
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


        EntityEmpty entity;
        public TimelineAsset asset;
        GameObject model;
        public void SaveAssets()
        {
            if (asset != null)
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
        }

        public void CreateGUI()
        {
            CreateChildren();
            root.style.flexGrow = 1f;
            rootVisualElement.Add(root);      

            trackView.window = this;

            ofEntity.objectType = typeof(GameObject);
            ofTimeline.objectType = typeof(TimelineAsset);


            createView.style.display = DisplayStyle.None;

            inputPath.SetValueWithoutNotify(Path);

            var temPrefab = SettingTools.GetPlayerPrefs<GameObject>("timeline_entity");
            ofEntity.SetValueWithoutNotify(temPrefab);
            _SetModel(temPrefab);
            asset = SettingTools.GetPlayerPrefs<TimelineAsset>("timeline_asset");
            ofTimeline.SetValueWithoutNotify(asset);
            _SetAsset(asset);
        }

        private void OnDestroy()
        {
            if (entity != null)
            {
                entity.Destroy();
                entity = null;
            }

            if (model != null)
            {
                Object.DestroyImmediate(model);
                model = null;
            }
        }
        partial void _OnOfEntityChanged(ChangeEvent<Object> e)
        {            
            if (e.newValue is GameObject obj)
            {
                _SetModel(obj);
            }
            else
            {
                if (entity != null)
                {
                    entity.Destroy();
                    entity = null;
                }
                trackView.RefreshView();
            }
        }
        void _SetModel(GameObject obj)
        {
            if (model != null)
            {
                Object.DestroyImmediate(model);
            }
            model = Instantiate(obj);

            if (entity != null)
            {
                entity.Destroy();
            }
            entity = Entity.Create<EntityEmpty>();
            entity.AddComponent<TimelineComponent>().Init(model);

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long _))
            {
                SettingTools.SavePlayerPrefs("timeline_entity", guid);
            }
            RefreshEntity();
        }
        partial void _OnOfTimelineChanged(ChangeEvent<Object> e)
        {
            if (e.newValue is TimelineAsset asset && this.asset !=asset)
            {
                _SetAsset(asset);
            }
        }
        void _SetAsset(TimelineAsset asset)
        {
            this.asset = asset;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long _))
            {
                SettingTools.SavePlayerPrefs("timeline_asset", guid);
            }
            RefreshEntity();
            trackView.RefreshView();
        }

        public void RefreshEntity()
        {
            if (asset == null) return;
            entity.GetComponent<TimelineComponent>().SetTimeline(asset);
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

    }

}

