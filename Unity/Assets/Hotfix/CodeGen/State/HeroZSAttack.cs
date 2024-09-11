////自动生成的代码，请勿修改!!!
//using System.Collections.Generic;
//namespace Ux
//{
//	public partial class HeroZSAttack : UnitStateTimeLine
//	{
//		public override int Priority => 20;
//		public override string Name => "Attack";
//		public override string ResName => "ZS_Attack";
//		protected override void InitConditions()
//		{
//			Conditions = new List<StateConditionBase>()
//			{
//				CreateCondition(nameof(StateCondition),StateConditionBase.StateType.Include, new HashSet<string>
//				{
//					"Idle",
//					"Run",
//				}),
//				CreateCondition(nameof(ActionKeyboardCondition),UnityEngine.InputSystem.Key.Q, StateConditionBase.TriggerType.Up),
//			};
//		}
//	}
//}
