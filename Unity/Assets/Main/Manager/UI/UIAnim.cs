using FairyGUI;
using System;
using System.Collections;
using UnityEngine;

namespace Ux
{
    public interface IUIAnim
    {
        void Play(Action end);
        void Stop();

        void SetStart();
        void SetEnd();
    }

    public class UITransition : IUIAnim
    {
        Transition transition;
        public UITransition(Transition transition)
        {
            this.transition = transition;
        }
        public void Play(Action end)
        {
            transition?.Play(() => end?.Invoke());
        }

        public void Stop()
        {
            //transition.Play(1, 0, transition.totalDuration, transition.totalDuration, null);            
            transition?.Stop();
        }

        public void SetEnd()
        {
            transition.Play(1, 0, transition.totalDuration, transition.totalDuration, null);            
        }

        public void SetStart()
        {
            transition?.Play();
            transition?.Stop(false, false);
        }
    }
}