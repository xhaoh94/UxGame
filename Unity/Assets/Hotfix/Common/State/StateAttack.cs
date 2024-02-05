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
    //internal class StateSkilll08 : UnitTimeLineNode
    //{
    //    public override string ResName => "ZS_Skill08";
    //    protected override void OnPlayEnd(PlayableDirector playableDirector)
    //    {
    //        base.OnPlayEnd(playableDirector);

    //        Machine.Enter<StateIdle>();
    //    }
    //}
}
