namespace Ux
{
    public class StateIdle : UnitStateNode
    {        
        public override string AnimName => "Hero_CK@Idle01";
        protected override void OnEnter(object args = null)
        {

        }

    }
    public class StateRun : UnitStateNode
    {
        public override string AnimName => "Hero_CK@Run";   
        protected override void OnEnter(object args = null)
        {

        }
    }
}
