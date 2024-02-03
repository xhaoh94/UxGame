using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ux;
using Cysharp.Threading.Tasks;
using UnityEngine.Playables;

namespace Ux
{
    public class UnitStateNode
    {
        UnitStateBase unitState;
        public UnitStateNode(UnitStateBase unitState)
        {
            this.unitState = unitState;
        }
        public Unit Unit => (unitState.Machine.Owner as StateComponent).ParentAs<Unit>();
    }

    public class UnitAnimNode : UnitStateNode
    {        
        public UnitAnimNode(UnitStateAnim stateAnim) : base(stateAnim) { }
        public AnimComponent Anim => Unit.Anim;
    }
    public abstract class UnitTimeLineNode : UnitStateNode
    {
        public UnitTimeLineNode(UnitStateAnim stateAnim) : base(stateAnim) { }
        public PlayableDirectorComponent PlayableDirector => Unit.Director;
    }
}