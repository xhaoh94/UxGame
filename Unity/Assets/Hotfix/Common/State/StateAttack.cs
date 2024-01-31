using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Playables;

namespace Ux
{
    internal class StateAttack : UnitTimeLineNode
    {
        public override string ResName => "ZS_Attack";
        protected override bool OnCheckValid(object args = null)
        {
            Log.Debug(Machine.CurrentNode.Name);
            if (Machine.CurrentNode == this)
            {
                return false;
            }
            if (Machine.CurrentNode.Name == nameof(StateSkilll08))
            {
                return false;
            }
            return base.OnCheckValid(args);
        }
        protected override void OnPlayEnd(PlayableDirector playableDirector)
        {
            base.OnPlayEnd(playableDirector);

            Machine.Enter<StateIdle>();
        }
    }
    internal class StateSkilll08 : UnitTimeLineNode
    {
        public override string ResName => "ZS_Skill08";
        protected override void OnPlayEnd(PlayableDirector playableDirector)
        {
            base.OnPlayEnd(playableDirector);

            Machine.Enter<StateIdle>();
        }
    }
}
