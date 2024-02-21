//自动生成的代码，请勿修改!!!
using System.Collections.Generic;
namespace Ux
{
	public partial class HeroZSSkill08_3 : UnitStateTimeLine
	{
		public override int Priority => 23;
		public override string Name => "Skill08_3";
		public override string ResName => "ZS_Skill08_3";
		protected override void InitConditions()
		{
			Conditions = new List<StateConditionBase>()
			{
				CreateCondition(nameof(StateCondition),StateConditionBase.State.Include, new HashSet<string>
				{
					"Skill08_2",
				}),
				CreateCondition(nameof(TemBoolVarCondition),"Skill08_2"),
				CreateCondition(nameof(ActionKeyboardCondition),UnityEngine.InputSystem.Key.R, StateConditionBase.Trigger.Down),
			};
		}
	}
}
