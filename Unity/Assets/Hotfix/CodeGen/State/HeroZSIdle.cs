//自动生成的代码，请勿修改!!!
using System.Collections.Generic;
namespace Ux
{
    public partial class HeroZSIdle : UnitStateAnim
    {
        public override string Name => "Idle";
        public override string ResName => "Hero_ZS@Stand";
        public override List<StateConditionBase> Conditions { get; } = new List<StateConditionBase>()
        {
            new StateCondition(StateConditionBase.State.Any, null),
        };
    }
}
