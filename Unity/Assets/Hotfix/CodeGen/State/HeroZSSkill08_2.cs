//自动生成的代码，请勿修改!!!
using System.Collections.Generic;
namespace Ux
{
	public partial class HeroZSSkill08_2 : UnitStateTimeLine
	{
		public override int Priority => 22;
		public override string Name => "Skill08_2";
		public override string ResName => "ZS_Skill08_2";
		protected override void InitConditions()
		{
			Conditions = new List<StateConditionBase>()
			{
				CreateCondition(nameof(StateCondition),StateConditionBase.State.Include, new HashSet<string>
				{
					"Skill08_1",
				}),
				CreateCondition(nameof(TemBoolVarCondition),"Skill08_1", true),
				CreateCondition(nameof(ActionKeyboardCondition),UnityEngine.InputSystem.Key.R, StateConditionBase.Trigger.Down),
			};
		}
	}
}
