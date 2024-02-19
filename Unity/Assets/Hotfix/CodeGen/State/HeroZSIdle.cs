//自动生成的代码，请勿修改!!!
using System.Collections.Generic;
namespace Ux
{
	public partial class HeroZSIdle : UnitStateTimeLine
	{
		public override string Name => "Idle";
		public override string ResName => "ZS_Idle";
		protected override void InitConditions()
		{
			Conditions = new List<StateConditionBase>()
			{
				CreateCondition(nameof(StateCondition),StateConditionBase.State.Any, null),
			};
		}
	}
}
