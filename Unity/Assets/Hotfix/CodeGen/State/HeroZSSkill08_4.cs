////自动生成的代码，请勿修改!!!
//using System.Collections.Generic;
//namespace Ux
//{
//	public partial class HeroZSSkill08_4 : UnitStateTimeLine
//	{
//		public override int Priority => 24;
//		public override string Name => "Skill08_4";
//		public override string ResName => "ZS_Skill08_4";
//		protected override void InitConditions()
//		{
//			Conditions = new List<StateConditionBase>()
//			{
//				CreateCondition(nameof(StateCondition),StateConditionBase.StateType.Include, new HashSet<string>
//				{
//					"Skill08_3",
//				}),
//				CreateCondition(nameof(TemBoolVarCondition),"Skill08_3"),
//				CreateCondition(nameof(ActionKeyboardCondition),UnityEngine.InputSystem.Key.R, StateConditionBase.TriggerType.Down),
//			};
//		}
//	}
//}
