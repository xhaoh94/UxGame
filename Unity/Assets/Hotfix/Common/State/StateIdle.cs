namespace Ux
{
    public class StateIdle : UnitStateNode
    {        
        public override string AnimName => "Hero_ZS@Idle01";
        protected override void OnEnter(object args = null)
        {

        }

    }
    public class StateRun : UnitStateNode
    {
        public override string AnimName => "Hero_ZS@Run";   
        protected override void OnEnter(object args = null)
        {

        }
    }
}
