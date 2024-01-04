using Cysharp.Threading.Tasks;
using FairyGUI;
using System;
using System.Threading;
using static Ux.UIMgr;

namespace Ux
{
    public interface IUI
    {
        UIState State { get; }
        UIType Type { get; }
        UIBlur Blur { get; }
#if UNITY_EDITOR
        string IDStr { get; }
#endif
        int ID { get; }
        IUIData Data { get; }
        bool IsDestroy { get; }
        bool Visable { get; set; }
        IFilter Filter { get; set; }
        void InitData(IUIData data, CallBackData initData);
        void Dispose();
        void DoShow(bool isAnim, int id, object param, bool isStack);
        void DoHide(bool isAnim, bool isStack);

    }

    public enum UIType
    {
        /// <summary>
        /// 不会关闭任何界面，但会被Stack界面关闭（Stack界面关闭的时候，会自动重新打开）
        /// </summary>
        None,
        /// <summary>
        /// 会关闭除Fixed之外的界面，也会被其他Stack界面关闭（Stack界面关闭的时候，会自动重新打开） 
        /// </summary>
        Stack,
        /// <summary>
        /// 固定界面,不会关闭其他界面，也不会被其他界面关闭
        /// </summary>
        Fixed,
    }

    //必须是2的n次方,可组合使用 (PS:Blur|Fixed)
    public enum UIBlur
    {
        /// <summary>
        /// 不会模糊其他界面也不会被其他界面模糊
        /// </summary>
        None = 0x1,
        /// <summary>
        /// 不会模糊其他界面但会被其他界面模糊
        /// </summary>
        Normal = 0x2,
        /// <summary>
        /// 模糊非固定界面
        /// </summary>
        Blur = 0x4,
        /// <summary>
        /// 模糊固定界面
        /// </summary>
        Fixed = 0x8,
        /// <summary>
        /// 模糊场景
        /// </summary>
        Scene = 0x10,
    }
    public abstract class UIBase : UIObject, IUI
    {
        protected abstract string PkgName { get; }
        protected abstract string ResName { get; }
        public virtual bool IsDestroy => true;
        public virtual UIType Type => UIType.None;
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

            return UIPackage.CreateObject(pkg, res);
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
#if UNITY_EDITOR
        public string IDStr => Data.IDStr;
#endif
        public IUIData Data { get; private set; }

        protected void Hide()
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

        protected virtual void OnOverwrite(object param)
        {
        }
        void IUI.DoShow(bool isAnim, int id, object param, bool isStack)
        {            
            switch (State)
            {
                case UIState.Show:
                case UIState.ShowAnim:
                    if (id == ID && param != null)
                    {
                        OnOverwrite(param);
                    }
                    _Show(id, param, isStack);
                    return;
                case UIState.HideAnim:
                    if (_hideToken != null)
                    {
                        _hideToken.Cancel();
                        _hideToken = null;
                    }
                    break;
                case UIState.Hide:
                    AddToStage();
                    OnLayout();
                    break;
            }
            _ReleaseShowToken();
            _showToken = new CancellationTokenSource();
            base.ToShow(isAnim, id, param, isStack, _showToken);
        }
        private void _Show(int id, object param, bool isStack)
        {
            if (id == ID && _cbData != null)
            {
                _cbData.Value.showCb?.Invoke(this, param, isStack);
            }
        }

        void IUI.DoHide(bool isAnim, bool isStack)
        {            
            switch (State)
            {
                case UIState.Hide:
                case UIState.HideAnim:
                    return;
                case UIState.ShowAnim:
                    if (_showToken != null)
                    {
                        _showToken.Cancel();
                        _showToken = null;
                    }
                    break;
            }
            if (_cbData != null)
            {
                _cbData.Value.stackCb?.Invoke(this, isStack);
            }
            _ReleaseHideToken();
            _hideToken = new CancellationTokenSource();
            base.ToHide(isAnim, isStack, _hideToken);
        }

        private void _Hide()
        {
            RemoveToStage();
            Filter = null;
            if (_cbData != null)
            {
                _cbData.Value.hideCb?.Invoke(this);
            }
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
                _showToken.Dispose();
                _showToken = null;
            }
        }
        void _ReleaseHideToken()
        {
            if (_hideToken != null)
            {
                _hideToken.Dispose();
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