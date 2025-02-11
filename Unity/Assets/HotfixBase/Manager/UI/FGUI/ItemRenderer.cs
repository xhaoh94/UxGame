﻿using FairyGUI;
using System;
using System.Threading;

namespace Ux
{
    public interface IItemRenderer
    {
        int Index { get; }
        void Init(GObject gObject, UIList list);
        void Release();
        void Set(int index, IUIParam data, bool isAnim);
    }
    public abstract class ItemRenderer : UIObject, IItemRenderer
    {
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

        void IItemRenderer.Set(int index, IUIParam data, bool isAnim)
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
