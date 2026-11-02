using System;
using UnityEngine;

namespace Ux
{
    public interface IUIAnim
    {
        void Play(Action end = null);
        void Stop();
        void SetToStart();
        void SetToEnd();
    }

    public sealed class UIAnimCanvasGroup : IUIAnim
    {
        private readonly CanvasGroup _group;
        private readonly float _start;
        private readonly float _end;

        public UIAnimCanvasGroup(GameObject target, float start = 0f, float end = 1f)
        {
            _group = target != null ? target.GetComponent<CanvasGroup>() ?? target.AddComponent<CanvasGroup>() : null;
            _start = start;
            _end = end;
        }

        public void Play(Action end = null)
        {
            SetToEnd();
            end?.Invoke();
        }

        public void Stop() { }
        public void SetToStart() { if (_group != null) _group.alpha = _start; }
        public void SetToEnd() { if (_group != null) _group.alpha = _end; }
    }
}
