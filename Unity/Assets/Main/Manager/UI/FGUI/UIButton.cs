using FairyGUI;
using System;
using UnityEngine;

namespace Ux
{
    public class UIButton : UIObject
    {
        public UIButton()
        {

        }
        public UIButton(GObject gObject, UIObject parent)
        {
            Init(gObject, parent);
            parent?.Components?.Add(this);
        }
        protected override void CreateChildren()
        {
        }
        public GButton Button => ObjAs<GButton>();

        #region FairyGUI 属性-方法
        /// <summary>
        /// Play sound when button is clicked.
        /// </summary>
        public NAudioClip sound
        {
            get => Button.sound;
            set => Button.sound = value;
        }

        /// <summary>
        /// Volume of the click sound. (0-1)
        /// </summary>
        public float soundVolumeScale
        {
            get => Button.soundVolumeScale;
            set => Button.soundVolumeScale = value;
        }

        /// <summary>
        /// For radio or checkbox. if false, the button will not change selected status on click. Default is true.
        /// 如果为true，对于单选和多选按钮，当玩家点击时，按钮会自动切换状态。设置为false，则不会。默认为true。
        /// </summary>
        public bool changeStateOnClick => Button.changeStateOnClick;

        /// <summary>
        /// Show a popup on click.
        /// 可以为按钮设置一个关联的组件，当按钮被点击时，此组件被自动弹出。
        /// </summary>
        public GObject linkedPopup
        {
            get => Button.linkedPopup;
            set => Button.linkedPopup = value;
        }


        /// <summary>
        /// Dispatched when the button status was changed.
        /// 如果为单选或多选按钮，当按钮的选中状态发生改变时，此事件触发。
        /// </summary>
        public EventListener onChanged => Button.onChanged;

        /// <summary>
        /// Icon of the button.
        /// </summary>
        public string icon
        {
            get => Button.icon;
            set => Button.icon = value;
        }

        /// <summary>
        /// Title of the button
        /// </summary>
        public string title
        {
            get => Button.title;
            set => Button.title = value;
        }

        /// <summary>
        /// Same of the title.
        /// </summary>
        public string text
        {
            get => Button.title;
            set => Button.title = value;
        }

        /// <summary>
        /// Icon value on selected status.
        /// </summary>
        public string selectedIcon
        {
            get => Button.selectedIcon;
            set => Button.selectedIcon = value;
        }

        /// <summary>
        /// Title value on selected status.
        /// </summary>
        public string selectedTitle
        {
            get => Button.selectedTitle;
            set => Button.selectedTitle = value;
        }

        /// <summary>
        /// Title color.
        /// </summary>
        public Color titleColor
        {
            get => Button.titleColor;
            set => Button.titleColor = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public Color color
        {
            get => Button.titleColor;
            set => Button.titleColor = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public int titleFontSize
        {
            get => Button.titleFontSize;
            set => Button.titleFontSize = value;
        }

        /// <summary>
        /// If the button is in selected status.
        /// </summary>
        public bool selected
        {
            get => Button.selected;
            set => Button.selected = value;
        }

        /// <summary>
        /// Button mode.
        /// </summary>
        /// <seealso cref="ButtonMode"/>
        public ButtonMode mode
        {
            get => Button.mode;
            set => Button.mode = value;
        }

        /// <summary>
        /// A controller is connected to this button, the activate page of this controller will change while the button status changed.
        /// 对应编辑器中的单选控制器。
        /// </summary>
        public Controller relatedController
        {
            get => Button.relatedController;
            set => Button.relatedController = value;
        }

        /// <summary>
        /// 
        /// </summary>
        public string relatedPageId
        {
            get => Button.relatedPageId;
            set => Button.relatedPageId = value;
        }

        /// <summary>
        /// Simulates a click on this button.
        /// 模拟点击这个按钮。
        /// </summary>
        /// <param name="downEffect">If the down effect will simulate too.</param>
        /// <param name="clickCall">是否触发回调</param>
        public void FireClick(bool downEffect, bool clickCall = false)
        {
            Button.FireClick(downEffect, clickCall);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public GTextField GetTextField()
        {
            return Button.GetTextField();
        }
        #endregion
        public void AddClick(EventCallback0 fn0)
        {
            AddClick(Button, fn0);
        }
        public void AddClick(EventCallback1 fn1)
        {
            AddClick(Button, fn1);
        }

        public void AddLongPress(Func<bool> fn, float delay = 0.2f, int loopCnt = 0, int holdRangeRadius = 50)
        {
            AddLongPress(Button, fn, delay, loopCnt, holdRangeRadius);
        }
        public void AddLongPress(float first, Func<bool> fn, float delay = 0.2f, int loopCnt = 0, int holdRangeRadius = 50)
        {
            AddLongPress(Button, first, fn, delay, loopCnt, holdRangeRadius);
        }

        public void AddMultipleClick(EventCallback0 fn0, int clickCnt = 2, float gapTime = 0.2f)
        {
            AddMultipleClick(Button, fn0, clickCnt, gapTime);
        }
        /// <summary>
        /// 多次点击事件，注册了多次点击事件，即使是单击也会受到gapTime延时触发
        /// </summary>    
        public void AddMultipleClick(EventCallback1 fn1, int clickCnt = 2, float gapTime = 0.2f)
        {
            AddMultipleClick(Button, fn1, clickCnt, gapTime);
        }
    }
}
