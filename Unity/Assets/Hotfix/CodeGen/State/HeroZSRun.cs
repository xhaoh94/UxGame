//自动生成的代码，请勿修改!!!
using System.Collections.Generic;
namespace Ux
{
	public partial class HeroZSRun : UnitStateAnim
	{
		public override string Name => "run";
		public override string ResName => "Hero_ZS@Run";
		public override List<StateConditionBase> Conditions { get; } = new List<StateConditionBase>()
		{
			new StateCondition(StateConditionBase.State.Include, new HashSet<string>
			{
                "Idle",
				"run",
			}),
			new ActionMoveCondition(),
		};
	}
}
