using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Playables;

namespace Ux
{
    partial class HeroZSAttack
    {        
        public Unit Unit => (Machine.Owner as StateComponent).ParentAs<Unit>();
        protected override void OnEnter()
        {
            base.OnEnter();
        }
        protected override void OnPlayEnd(PlayableDirector playableDirector)
        {
            base.OnPlayEnd(playableDirector);
        }
    }
    partial class HeroZSSkill08_1
    {
        public Unit Unit => (Machine.Owner as StateComponent).ParentAs<Unit>();
        protected override void OnEnter()
        {
            base.OnEnter();
        }
        protected override void OnPlayEnd(PlayableDirector playableDirector)
        {
            base.OnPlayEnd(playableDirector);
        }
    }
    partial class HeroZSSkill08_2
    {
        public Unit Unit => (Machine.Owner as StateComponent).ParentAs<Unit>();
        protected override void OnEnter()
        {
            base.OnEnter();
        }
        protected override void OnPlayEnd(PlayableDirector playableDirector)
        {
            base.OnPlayEnd(playableDirector);
        }
    }
    partial class HeroZSSkill08_4
    {
        public Unit Unit => (Machine.Owner as StateComponent).ParentAs<Unit>();
        protected override void OnEnter()
        {
            base.OnEnter();
        }
        protected override void OnPlayEnd(PlayableDirector playableDirector)
        {
            base.OnPlayEnd(playableDirector);
        }
    }
}
