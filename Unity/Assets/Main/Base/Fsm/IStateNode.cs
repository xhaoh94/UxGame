namespace Ux
{
    public interface IStateNode
    {
        string Name { get; }
        void Create(StateMachine machine, object args = null, bool isFromPool = true);
        void Enter(object args = null);
        void Exit();
        void Update();
        void Release();
    }
    public abstract class StateNode : IStateNode
    {
        bool _isFromPool;
        public virtual string Name => GetType().FullName;
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
        public virtual void Create(StateMachine machine, object args = null, bool isFromPool = true)
        {
            _isFromPool = isFromPool;
            Machine = machine;
            OnCreate(args);
        }

        public virtual void Enter(object args = null)
        {
            OnEnter(args);
        }
        public virtual void Exit()
        {
            OnExit();
        }

        public virtual void Update()
        {
            OnUpdate();
        }

        protected virtual void OnCreate(object args = null) { }
        protected virtual void OnEnter(object args = null) { }
        protected virtual void OnExit() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnRelease() { }
    }
}