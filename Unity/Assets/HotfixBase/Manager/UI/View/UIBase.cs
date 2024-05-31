using FairyGUI;
using System;
using System.Threading;
using UnityEngine;
using static Ux.UIMgr;

namespace Ux
{
    public abstract class UIBase : UIObject, IUI
    {
#if UNITY_EDITOR
        static Transform __ui_cache_content;
#endif

        protected abstract string PkgName { get; }
        protected abstract string ResName { get; }
        public virtual bool IsDestroy => true;
        public virtual UIType Type => UIType.Normal;
        public virtual UIBlur Blur => UIBlur.Normal;

        CancellationTokenSource _showToken;
        CancellationTokenSource _hideToken;

        private CallBackData? _cbData;
        public virtual void InitData(IUIData data, CallBackData initData)
        {
            Data = data;
            _cbData = initData;
            Init(CreateObject());
            OnHideCallBack += _Hide;
            OnShowCallBack += _Show;
        }

        public virtual void AddChild(UITabView child) { }

        protected virtual GObject CreateObject()
        {
            string pkg = PkgName;
            string res = ResName;
            if (string.IsNullOrEmpty(pkg) || string.IsNullOrEmpty(res))
            {
                Log.Fatal("没有指定pkgName或是resName");
            }
            var gobj = UIPackage.CreateObject(pkg, res);
#if UNITY_EDITOR
            if (__ui_cache_content == null)
            {
                __ui_cache_content = new GameObject($"[UI]").transform;
                __ui_cache_content.SetParent(UnityPool.PoolContent);
            }
            gobj.displayObject.home = __ui_cache_content;
#endif
            return gobj;
        }
        public IFilter Filter
        {
            get
            {
                if (GObject == null) return null;
                return GObject.filter;
            }
            set
            {
                if (GObject == null) return;
                GObject.filter = value;
            }

        }
        public bool Visable
        {
            get
            {
                return GObject.visible;
            }
            set
            {
                if (GObject.visible != value)
                {
                    GObject.visible = value;
                }
            }
        }
        public int ID => Data.ID;
        public string Name => Data.Name;
        public IUIData Data { get; private set; }

        protected void HideSelf()
        {
            Hide(true);
        }
        public virtual void Hide(bool isAnim = true)
        {
            if (_cbData != null)
            {
                _cbData.Value.backCb?.Invoke(ID, isAnim);
            }
            else
            {
                UIMgr.Ins.Hide(ID, isAnim);
            }
        }

        protected void MakeFullScreen()
        {
            ObjAs<GComponent>()?.MakeFullScreen();
        }
        protected virtual void AddToStage() { }
        protected virtual void RemoveToStage()
        {
            GObject?.RemoveFromParent();
        }
        protected virtual void OnLayout() { }

        void IUI.DoShow(bool isAnim, int id, IUIParam param, bool isStack)
        {
            switch (State)
            {
                case UIState.Show:
                case UIState.ShowAnim:
                    if (id == ID && param != null)
                    {
                        ToOverwrite(param);
                    }
                    _Show(id, param, isStack);
                    return;
                case UIState.HideAnim:
                    _ReleaseHideToken();
                    break;
                case UIState.Hide:
                    AddToStage();
                    OnLayout();
                    break;
            }
            _ReleaseShowToken();
            if (isAnim && ShowAnim != null)
            {
                _showToken = new CancellationTokenSource();
            }
            ToShow(isAnim, id, param, isStack, _showToken);
        }
        private void _Show(int id, IUIParam param, bool isStack)
        {
            if (_showToken != null)
            {
                _showToken = null;
            }
            if (id == ID && _cbData != null)
            {
                _cbData.Value.showCb?.Invoke(this, param, isStack);
            }
        }

        void IUI.DoHide(bool isAnim, bool isStack)
        {            
            if (_cbData != null)
            {
                if (_cbData.Value.stackCb.Invoke(this, isStack))
                {
                    return;
                }
            }

            switch (State)
            {
                case UIState.Hide:
                case UIState.HideAnim:
                    return;
                case UIState.ShowAnim:
                    _ReleaseShowToken();
                    break;
            }

            _ReleaseHideToken();
            if (isAnim && HideAnim != null)
            {
                _hideToken = new CancellationTokenSource();
            }            
            ToHide(isAnim, isStack, _hideToken);
        }

        private void _Hide()
        {
            if (_hideToken != null)
            {
                _hideToken = null;
            }
            RemoveToStage();
            Filter = null;
            _cbData?.hideCb?.Invoke(this);
        }
        void IUI.Dispose()
        {
            ToDispose(true);
        }
        protected override void OnDispose()
        {
            _ReleaseShowToken();
            _ReleaseHideToken();
            Data = null;
            _cbData = null;
        }
        void _ReleaseShowToken()
        {
            if (_showToken != null)
            {
                _showToken.Cancel();
                _showToken = null;
            }
        }
        void _ReleaseHideToken()
        {
            if (_hideToken != null)
            {
                _hideToken.Cancel();
                _hideToken = null;
            }
        }
        protected void SetLayout(UILayout layout, bool restraint = true)
        {
            if (GObject == null) return;
            GObject parent = GObject.parent ?? UIMgr.Ins.GetLayer(UILayer.Root);
            switch (layout)
            {
                case UILayout.Left_Top:
                    GObject.SetPosition(0, 0, 0);
                    if (restraint)
                    {
                        AddRelation(RelationType.Left_Left);
                        AddRelation(RelationType.Top_Top);
                    }

                    break;
                case UILayout.Left_Middle:
                    GObject.SetPosition(0, (parent.height - GObject.height) / 2, 0);
                    if (restraint)
                    {
                        AddRelation(RelationType.Left_Left);
                        AddRelation(RelationType.Middle_Middle);
                    }

                    break;
                case UILayout.Left_Bottom:
                    GObject.SetPosition(0, parent.height - GObject.height, 0);
                    if (restraint)
                    {
                        AddRelation(RelationType.Left_Left);
                        AddRelation(RelationType.Bottom_Bottom);
                    }

                    break;
                case UILayout.Center_Top:
                    GObject.SetPosition((parent.width - GObject.width) / 2, 0, 0);
                    if (restraint)
                    {
                        AddRelation(RelationType.Center_Center);
                        AddRelation(RelationType.Top_Top);
                    }

                    break;
                case UILayout.Center_Middle:
                    GObject.Center(restraint);
                    break;
                case UILayout.Center_Bottom:
                    GObject.SetPosition((parent.width - GObject.width) / 2, parent.height - GObject.height, 0);
                    if (restraint)
                    {
                        AddRelation(RelationType.Center_Center);
                        AddRelation(RelationType.Bottom_Bottom);
                    }

                    break;
                case UILayout.Right_Top:
                    GObject.SetPosition(parent.width - GObject.width, 0, 0);
                    if (restraint)
                    {
                        AddRelation(RelationType.Right_Right);
                        AddRelation(RelationType.Top_Top);
                    }

                    break;
                case UILayout.Right_Middle:
                    GObject.SetPosition(parent.width - GObject.width, (parent.height - GObject.height) / 2, 0);
                    if (restraint)
                    {
                        AddRelation(RelationType.Right_Right);
                        AddRelation(RelationType.Middle_Middle);
                    }

                    break;
                case UILayout.Right_Bottom:
                    GObject.SetPosition(parent.width - GObject.width, parent.height - GObject.height, 0);
                    if (restraint)
                    {
                        AddRelation(RelationType.Right_Right);
                        AddRelation(RelationType.Bottom_Bottom);
                    }

                    break;
                case UILayout.Size:
                    GObject.SetPosition(0, 0, 0);
                    GObject.SetSize(parent.width, parent.height);
                    if (restraint)
                    {
                        AddRelation(RelationType.Size);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(layout), layout, null);
            }
        }

        protected void AddRelation(RelationType relation)
        {
            if (GObject == null) return;
            GObject parent = GObject.parent ?? UIMgr.Ins.GetLayer(UILayer.Root);
            GObject.AddRelation(parent, relation);
        }

        protected void RemoveRelation(RelationType relation)
        {
            if (GObject == null) return;
            GObject parent = GObject.parent ?? UIMgr.Ins.GetLayer(UILayer.Root);
            GObject.RemoveRelation(parent, relation);
        }

    }
}