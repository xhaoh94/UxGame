using FairyGUI;
using System;
using System.Threading;

namespace Ux
{
    public interface IItemRenderer
    {
        int Index { get; }
        void Init(GObject gObject, UIList list);
        void Release();
        void Set(int index, object data, bool isAnim);
    }
    public abstract class ItemRenderer : UIObject, IItemRenderer
    {
        /// <summary>
        /// 由于ItemRenderer在滚动的时候会调用的很频繁，所以取消用特性注册事件
        /// </summary>
        //protected override bool IsRegisterFastMethod => false;
        public int Index { get; private set; } = -1;
        public UIList List => ParentAs<UIList>();

        void IItemRenderer.Init(GObject gObject, UIList list)
        {
            Init(gObject, list);
            gObject.onRemovedFromStage.Add(OnRemovedFromStage);
        }
        void IItemRenderer.Release()
        {
            OnRemovedFromStage();
            GObject.onRemovedFromStage.Remove(OnRemovedFromStage);
            Index = -1;
            ToDispose(false);
            Pool.Push(this);
        }

        void IItemRenderer.Set(int index, object data, bool isAnim)
        {
            Index = index;
            ToShow(false, 0, data, false, null);
            if (isAnim && ShowAnim != null)
            {
                ShowAnim?.SetToStart();
                ShowAnim?.Play();
            }
        }

        void OnRemovedFromStage()
        {
            ToHide(false, false, null);
        }


    }
}
