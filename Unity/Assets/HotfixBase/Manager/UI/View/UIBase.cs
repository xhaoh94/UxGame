using Cysharp.Threading.Tasks;
using FairyGUI;
using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using static Ux.UIMgr;

namespace Ux
{
    public abstract class UIBase : UIObject, IUI, IUIAsync
    {
#if UNITY_EDITOR
        static Transform __ui_cache_content;
#endif

        protected abstract string PkgName { get; }
        protected abstract string ResName { get; }
        /// <summary>
        /// 关闭界面后，多久（秒）销毁：-1：不销毁，n:n秒后销毁。默�?0�?
        /// </summary>
        public virtual int HideDestroyTime => 60;
        public virtual UIType Type => UIType.Normal;
        public virtual UIBlur Blur => UIBlur.Normal;

        // 使用静态池复用CancellationTokenSource，减少GC
        CancellationTokenSource _showToken;
        CancellationTokenSource _hideToken;

        private CallbackData? _cbData;
        public virtual void InitData(IUIData data, CallbackData initData)
        {
            Data = data;
            _cbData = initData;
            Init(CreateObject());
            OnHideCallback += _Hide;
            OnShowCallback += _Show;
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
            get => GObject?.filter;
            set
            {
                if (GObject == null) return;
                GObject.filter = value;
            }
        }
        public bool Visible
        {
            get => GObject.visible;
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
            UIMgr.Ins.Hide(ID, isAnim);
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

        bool _async;
        Action _asyncComplete;
        void IUIAsync.Change(bool b)
        {
            _async = b;
            if (!b)
            {
                var temFn = _asyncComplete;
                _asyncComplete = null;
                temFn?.Invoke();
            }
        }

        UniTask IUI.DoShow(bool isAnim, int id, IUIParam param, bool checkStack)
        {
            var task = AutoResetUniTaskCompletionSource.Create();
            switch (State)
            {
                case UIState.Show:
                case UIState.ShowAnim:
                    if (id == ID)
                    {
                        (this as IUISetParam).SetParam(param);
                        ToOverwrite();
                    }
                    _Show(id, param, checkStack);
                    task.TrySetResult();
                    return task.Task;
                case UIState.Hide:
                    AddToStage();
                    OnLayout();
                    break;
            }

            if (_async)
            {
                _asyncComplete = _DoShow;
            }
            else
            {
                _DoShow();
            }
            _ReleaseHideToken();

            void _DoShow()
            {
                if (isAnim && ShowAnim != null)
                {
                    _showToken = CreateToken();
                }
                (this as IUISetParam).SetParam(param);
                ToShow(isAnim, id, checkStack, _showToken);
                task.TrySetResult();
            }
            return task.Task;
        }

        void _Show(int id, IUIParam param, bool checkStack)
        {
            if (_showToken != null)
            {
                ReturnTokenToPool(_showToken);
                _showToken = null;
            }

            if (_cbData != null)
            {
                _cbData.Value.showCb?.Invoke(this, param, checkStack);
            }

            EventMgr.Ins.Run(MainEventType.UI_SHOW, ID);
            EventMgr.Ins.Run(MainEventType.UI_SHOW, GetType());
        }

        void IUI.DoHide(bool isAnim, bool checkStack)
        {
            //子界面不需要检测栈，由最顶层父界面控�?
            switch (State)
            {
                case UIState.Hide:
                case UIState.HideAnim:
                    return;
            }

            if (_async)
            {
                _asyncComplete = _DoHide;
            }
            else
            {
                _DoHide();
            }
            _ReleaseShowToken();
            void _DoHide()
            {
                _hideToken = isAnim && HideAnim != null ? CreateToken() : null;
                ToHide(isAnim, checkStack, _hideToken);
            }
        }

        private void _Hide()
        {
            if (_hideToken != null)
            {
                ReturnTokenToPool(_hideToken);
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
            _async = false;
            _asyncComplete = null;
        }

        void _ReleaseShowToken()
        {
            if (_showToken != null)
            {
                ReturnTokenToPool(_showToken);
                _showToken = null;
            }
        }

        void _ReleaseHideToken()
        {
            if (_hideToken != null)
            {
                ReturnTokenToPool(_hideToken);
                _hideToken = null;
            }
        }
        /// <summary>
        /// 设置位置并添加关联关�?
        /// </summary>
        private void _SetPositionAndRelations(float x, float y, bool restraint, RelationType rel1, RelationType rel2 = 0)
        {
            GObject.SetPosition(x, y, 0);
            if (restraint)
            {
                if (rel1 != 0) AddRelation(rel1);
                if (rel2 != 0) AddRelation(rel2);
            }
        }

        protected void SetLayout(UILayout layout, bool restraint = true)
        {
            if (GObject == null) return;
            GObject parent = GObject.parent ?? UIMgr.Ins.GetLayer(UILayer.Root);
            switch (layout)
            {
                case UILayout.Left_Top:
                    _SetPositionAndRelations(0, 0, restraint, RelationType.Left_Left, RelationType.Top_Top);
                    break;
                case UILayout.Left_Middle:
                    _SetPositionAndRelations(0, (parent.height - GObject.height) / 2, restraint, RelationType.Left_Left, RelationType.Middle_Middle);
                    break;
                case UILayout.Left_Bottom:
                    _SetPositionAndRelations(0, parent.height - GObject.height, restraint, RelationType.Left_Left, RelationType.Bottom_Bottom);
                    break;
                case UILayout.Center_Top:
                    _SetPositionAndRelations((parent.width - GObject.width) / 2, 0, restraint, RelationType.Center_Center, RelationType.Top_Top);
                    break;
                case UILayout.Center_Middle:
                    GObject.Center(restraint);
                    break;
                case UILayout.Center_Bottom:
                    _SetPositionAndRelations((parent.width - GObject.width) / 2, parent.height - GObject.height, restraint, RelationType.Center_Center, RelationType.Bottom_Bottom);
                    break;
                case UILayout.Right_Top:
                    _SetPositionAndRelations(parent.width - GObject.width, 0, restraint, RelationType.Right_Right, RelationType.Top_Top);
                    break;
                case UILayout.Right_Middle:
                    _SetPositionAndRelations(parent.width - GObject.width, (parent.height - GObject.height) / 2, restraint, RelationType.Right_Right, RelationType.Middle_Middle);
                    break;
                case UILayout.Right_Bottom:
                    _SetPositionAndRelations(parent.width - GObject.width, parent.height - GObject.height, restraint, RelationType.Right_Right, RelationType.Bottom_Bottom);
                    break;
                case UILayout.Size:
                    GObject.SetPosition(0, 0, 0);
                    GObject.SetSize(parent.width, parent.height);
                    if (restraint) AddRelation(RelationType.Size);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(layout), layout, null);
            }
        }

        // 提取公共方法：获取父对象
        private GComponent GetParentComponent()
        {
            if (GObject == null) return null;
            return GObject.parent as GComponent ?? UIMgr.Ins.GetLayer(UILayer.Root);
        }

        protected void AddRelation(RelationType relation)
        {
            var parent = GetParentComponent();
            if (parent == null) return;
            GObject.AddRelation(parent, relation);
        }

        protected void RemoveRelation(RelationType relation)
        {
            var parent = GetParentComponent();
            if (parent == null) return;
            GObject.RemoveRelation(parent, relation);
        }

        // Token池管理方�?
        private static CancellationTokenSource CreateToken()
        {
            return new CancellationTokenSource();
        }

        private static void ReturnTokenToPool(CancellationTokenSource token)
        {
            if (token == null) return;

            try
            {
                if (!token.IsCancellationRequested)
                {
                    token.Cancel();
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            token.Dispose();
        }
    }
}

