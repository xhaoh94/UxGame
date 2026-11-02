using UnityEngine;

namespace Ux
{
    public interface IItemRenderer
    {
        int Index { get; }
        void Init(GameObject gameObject, UIList list);
        void Release();
        void Set(int index, IUIParam data, bool isAnim);
    }

    public abstract class ItemRenderer : UIObject, IItemRenderer
    {
        public int Index { get; private set; } = -1;
        public UIList List => ParentAs<UIList>();

        void IItemRenderer.Init(GameObject gameObject, UIList list)
        {
            Init(gameObject, list);
        }

        void IItemRenderer.Release()
        {
            ToHide(false, false, 0);
            Index = -1;
            (this as IUISetParam).SetParam(null);
            ToDispose(false);
        }

        void IItemRenderer.Set(int index, IUIParam data, bool isAnim)
        {
            Index = index;
            (this as IUISetParam).SetParam(data);
            ToShow(false, 0, false, 0);
        }
    }
}
