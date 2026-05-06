using FairyGUI;
using System;
using System.Collections;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// UI动画接口，定义UI动画的基本操作
    /// </summary>
    public interface IUIAnim
    {
        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="end">动画结束回调</param>
        void Play(Action end = null);
        
        /// <summary>
        /// 停止动画
        /// </summary>
        void Stop();
        
        /// <summary>
        /// 设置到动画开始状态
        /// </summary>
        void SetToStart();
        
        /// <summary>
        /// 设置到动画结束状态
        /// </summary>
        void SetToEnd();
    }

    /// <summary>
    /// FairyGUI Transition动画适配器
    /// 将FairyGUI的Transition封装为IUIAnim接口
    /// </summary>
    public class UIAnimTransition : IUIAnim
    {
        Transition _transition;  // FairyGUI的Transition对象
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="transition">FairyGUI的Transition对象</param>
        public UIAnimTransition(Transition transition)
        {
            _transition = transition;
        }
        
        /// <summary>
        /// 播放动画
        /// </summary>
        /// <param name="end">动画结束回调</param>
        public void Play(Action end)
        {
            _transition?.Play(() => end?.Invoke());
        }

        /// <summary>
        /// 停止动画
        /// </summary>
        public void Stop()
        {
            _transition?.Stop();
        }

        /// <summary>
        /// 设置到动画结束状态
        /// </summary>
        public void SetToEnd()
        {
            _transition.Play(1, 0, _transition.totalDuration, _transition.totalDuration, null);
        }

        /// <summary>
        /// 设置到动画开始状态
        /// </summary>
        public void SetToStart()
        {
            _transition?.Play();
            _transition?.Stop(false, false);
        }
    }
}