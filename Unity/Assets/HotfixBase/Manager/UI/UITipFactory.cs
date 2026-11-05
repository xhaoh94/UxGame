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
            if (UIMgr.Ins.TryGetTypeId(_defaultType, out var id))
            {
                Show(id, content);
            }
            else
            {
                Log.Error("没有指定UI面板,请检查是否已初始化SetDefaultType");
            }
        }

        /// <summary>
        /// 显示指定类型的提示信息
        /// </summary>
        /// <typeparam name="T">提示类型，必须是UITip的子类</typeparam>
        /// <param name="content">提示内容</param>
        public void Show<T>(string content) where T : UITip
        {
            if (UIMgr.Ins.TryGetTypeId(typeof(T), out var id))
            {
                Show(id, content);
            }

        }

        /// <summary>
        /// 内部显示方法
        /// </summary>
        /// <param name="id">提示UI ID</param>
        /// <param name="content">提示内容</param>
        void Show(int id, string content)
        {            
            // 检查并调整位置，避免提示重叠
            CheckPos();
            // 飘字要能同时堆叠，必须走多实例；否则后来的会覆盖前一条。
            UIMgr.Ins.Request(id).SetParam(IUIParam.Create(new TipData(OnShow, OnHide, content)))
                .SetMode(UIShowMode.NewInstance)
                .Show();
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
                if (ui.GameObject == null)
                    continue;

                var rect = ui.GameObject.transform as RectTransform;
                if (rect != null)
                    rect.anchoredPosition += new Vector2(0f, 30f);
            }
        }
    }
}