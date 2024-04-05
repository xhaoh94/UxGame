using FairyGUI;
using System;
using System.Collections;
using UnityEngine;

namespace Ux
{
    public interface IUIAnim
    {
        void Play(Action end = null);
        void Stop();
        /// <summary>
        /// 设置到开始状态
        /// </summary>
        void SetToStart();
        /// <summary>
        /// 设置到结束状态
        /// </summary>
        void SetToEnd();
    }

    public class UIAnimTransition : IUIAnim
    {
        Transition _transition;
        public UIAnimTransition(Transition transition)
        {
            _transition = transition;
        }
        public void Play(Action end)
        {
            _transition?.Play(() => end?.Invoke());
        }

        public void Stop()
        {
            _transition?.Stop();
        }

        public void SetToEnd()
        {
            _transition.Play(1, 0, _transition.totalDuration, _transition.totalDuration, null);
        }

        public void SetToStart()
        {
            _transition?.Play();
            _transition?.Stop(false, false);
        }
    }
}