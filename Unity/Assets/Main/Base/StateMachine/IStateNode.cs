namespace Ux
{
    public interface IStateNode
    {
        string Name { get; }
        StateMachine Machine { get; }
        void Create(StateMachine machine, object args = null, bool isFromPool = true);        
        void Enter();
        void Exit();        
        void Release();
    }
    public abstract class StateNode : IStateNode
    {
        bool _isFromPool;
        public virtual string Name => GetType().Name;
        public StateMachine Machine { get; private set; }
        public void Release()
        {
            OnRelease();
            Machine = null;
            if (_isFromPool)
            {
                Pool.Push(this);
            }
        }
        void IStateNode.Create(StateMachine machine, object args, bool isFromPool)
        {
            _isFromPool = isFromPool;
            Machine = machine;
            OnCreate(args);
        }        
        void IStateNode.Enter()
        {
            OnEnter();
        }
        void IStateNode.Exit()
        {
            OnExit();
        }

        protected virtual void OnCreate(object args = null) { }        
        protected virtual void OnEnter() { }
        protected virtual void OnExit() { }        
        protected virtual void OnRelease() { }
    }
}