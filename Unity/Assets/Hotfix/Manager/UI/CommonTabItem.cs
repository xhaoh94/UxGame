using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux.UI
{
    public class CommonTabItemAnim : IUIAnim
    {
        Transition transition;
        float _delay;
        public CommonTabItemAnim(Transition transition, float delay)
        {
            _delay = delay;
            this.transition = transition;
        }
        public void Play(Action end)
        {
            transition?.Play(1, _delay, () => end?.Invoke());
        }

        public void Stop()
        {                    
            transition?.Stop();
        }

        public void SetToEnd()
        {
            transition.Play(1, 0, transition.totalDuration, transition.totalDuration, null);
        }

        public void SetToStart()
        {
            transition?.Play();
            transition?.Stop(false, false);
        }
    }

    partial class CommonTabItem
    {
        protected override IUIAnim ShowAnim => new CommonTabItemAnim(showanim, Index * 0.3f);
    }
}
