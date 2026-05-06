using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 提示信息工厂类，用于创建和管理提示信息（如Toast）
    /// 继承自UIBaseFactory，专门处理提示类UI
    /// </summary>
    public class UITipFactory : UIBaseFactory<UITip>
    {
        /// <summary>
        /// 提示数据结构体，存储提示的回调和内容
        /// </summary>
        public struct TipData
        {
            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="_showFn">显示回调</param>
            /// <param name="_hideFn">隐藏回调</param>
            /// <param name="content">提示内容</param>
            public TipData(Action<UITip> _showFn, Action<UITip> _hideFn, string content)
            {
                ShowCallBack = _showFn;
                HideCallBack = _hideFn;
                Content = content;
            }
            
            /// <summary>
            /// 隐藏回调函数
            /// </summary>
            public Action<UITip> HideCallBack { get; }
            
            /// <summary>
            /// 显示回调函数
            /// </summary>
            public Action<UITip> ShowCallBack { get; }
            
            /// <summary>
            /// 提示内容文本
            /// </summary>
            public string Content { get; }
        }

        /// <summary>
        /// 显示默认类型的提示信息
        /// </summary>
        /// <param name="content">提示内容</param>
        public void Show(string content)
        {
            Show(GetDefaultID(), content);
        }

        /// <summary>
        /// 显示指定类型的提示信息
        /// </summary>
        /// <typeparam name="T">提示类型，必须是UITip的子类</typeparam>
        /// <param name="content">提示内容</param>
        public void Show<T>(string content) where T : UITip
        {
            var id = GetUIID(typeof(T));
            Show(id, content);
        }

        /// <summary>
        /// 内部显示方法
        /// </summary>
        /// <param name="id">提示UI ID</param>
        /// <param name="content">提示内容</param>
        void Show(int id, string content)
        {
            if (!CheckDefault(id))
            {
                Log.Error("没有指定Tip面板,请检查是否已初始化SetDefaultType");
                return;
            }
            // 检查并调整位置，避免提示重叠
            CheckPos();            
            UIMgr.Ins.Create(id).SetParam(IUIParam.Create(new TipData(OnShow, OnHide, content))).Show();
        }

        /// <summary>
        /// 检查并调整已显示提示的位置
        /// 当新提示显示时，将已有的提示向上移动，避免重叠
        /// </summary>
        void CheckPos()
        {
            foreach (var id in _showed)
            {
                var ui = UIMgr.Ins.GetUI<UITip>(id);
                if (ui == null) continue;
                var gobj = ui.GObject;
                // 将已有提示向上移动30像素
                gobj.SetPosition(gobj.x, gobj.y - 30, 0);
            }
        }
    }
}