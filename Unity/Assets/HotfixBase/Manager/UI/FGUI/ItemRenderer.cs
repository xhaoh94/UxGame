using FairyGUI;
using System;
using System.Threading;

namespace Ux
{
    public interface IItemRenderer
    {
        int Index { get; }
        void Init(int index, UIList list);
        void Release();
        void Set(GObject gObject, object data, bool isAnim);
    }
    public abstract class ItemRenderer : UIObject, IItemRenderer
    {
        /// <summary>
        /// 由于ItemRenderer在滚动的时候会调用的很频繁，所以取消用特性注册事件
        /// </summary>
        protected override bool IsRegisterFastMethod => false;
        public int Index { get; private set; } = -1;
        public UIList List { get; private set; }

        bool _isRemove = true;
        //防止产生GC
        EventCallback0 __removeCb;
        //重写掉CreateChildren，使用代码生成的会自动重写此方法。
        //这里重写掉，主要是怕有些ItemRenderer,如果不是代码生成，会走基类的反射，
        //而ItemRenderer，在滚动的时候非常耗性能，所以要么使用代码生成，要么手动获取子对象       
        protected override void CreateChildren()
        {

        }

        public ItemRenderer()
        {
            __removeCb = OnRemovedFromStage;
        }
        void IItemRenderer.Init(int index, UIList list)
        {
            Index = index;
            List = list;
        }
        void IItemRenderer.Release()
        {
            OnRemovedFromStage();
            Index = -1;
            List = null;
            Pool.Push(this);
        }


        void IItemRenderer.Set(GObject gObject, object data, bool isAnim)
        {
            if (!_isRemove)
            {
                if (gObject == GObject)
                {
                    ToShow(false, 0, data, false, null);
                    return;
                }
                OnRemovedFromStage();
            }
            _isRemove = false;
            gObject.onRemovedFromStage.Add(__removeCb);
            Init(gObject, List);
            ToShow(false, 0, data, false, null);
            if (isAnim && ShowAnim != null)
            {
                ShowAnim?.SetToStart();
                ShowAnim?.Play();
            }
        }


        void OnRemovedFromStage()
        {
            if (_isRemove)
            {
                return;
            }
            _isRemove = true;
            GObject.onRemovedFromStage.Remove(__removeCb);
            ToHide(false, false, null);
            ToDispose(false);
        }

    }
}
