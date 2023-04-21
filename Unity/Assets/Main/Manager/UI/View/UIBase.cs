using FairyGUI;
using System;

namespace Ux
{
    public interface IUI
    {
        UIState State { get; }
        string ID { get; }
        IUIData Data { get; }
        bool IsDestroy { get; }
        void InitData(IUIData data, Action<IUI> remove);
        void Dispose(bool isDisposeGObject = true);
        void DoShow(bool isAnim, object param);
        void DoResume(object param);
        void DoHide(bool isAnim);
    }

    public abstract class UIBase : UIObject, IUI
    {

        protected abstract string PkgName { get; }
        protected abstract string ResName { get; }
        public virtual bool IsDestroy => true;

        Action<IUI> _remove;
        public virtual void InitData(IUIData data, Action<IUI> remove)
        {
            Data = data;
            _remove = remove;
            Init(CreateObject());
            OnHideCallBack += _Hide;
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

        public string ID => Data.ID;

        public IUIData Data { get; private set; }

        public virtual void Hide()
        {
            UIMgr.Instance.Hide(Data.ID);
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

        public override void DoShow(bool isAnim, object param)
        {
            AddToStage();
            OnLayout();
            base.DoShow(isAnim, param);
        }

        private void _Hide()
        {
            RemoveToStage();
            _remove?.Invoke(this);
        }

        protected override void OnDispose()
        {
            Data = null;
            _remove = null;
        }
        protected void SetLayout(UILayout layout, bool restraint = true)
        {
            if (GObject == null) return;
            GObject parent = GObject.parent ?? UIMgr.Instance.GetLayer(UILayer.Root);
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
            GObject parent = GObject.parent ?? UIMgr.Instance.GetLayer(UILayer.Root);
            GObject.AddRelation(parent, relation);
        }

        protected void RemoveRelation(RelationType relation)
        {
            if (GObject == null) return;
            GObject parent = GObject.parent ?? UIMgr.Instance.GetLayer(UILayer.Root);
            GObject.RemoveRelation(parent, relation);
        }
    }
}