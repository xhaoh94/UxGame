namespace Ux
{
    public interface IStateNode
    {
        string Name { get; }
        void Create(StateMachine machine, object args = null, bool isFromPool = true);
        bool CheckValid(object args = null);
        void Enter(object args = null);
        void Exit();
        void Update();
        void Release();
    }
    public abstract class StateNode : IStateNode
    {
        bool _isFromPool;
        public virtual string Name => GetType().Name;
        protected StateMachine Machine { get; private set; }
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
        bool IStateNode.CheckValid(object args) { return OnCheckValid(args); }
        void IStateNode.Enter(object args)
        {
            OnEnter(args);
        }
        void IStateNode.Exit()
        {
            OnExit();
        }

        void IStateNode.Update()
        {
            OnUpdate();
        }

        protected virtual void OnCreate(object args = null) { }
        protected virtual bool OnCheckValid(object args = null) { return true; }
        protected virtual void OnEnter(object args = null) { }
        protected virtual void OnExit() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnRelease() { }
    }
}