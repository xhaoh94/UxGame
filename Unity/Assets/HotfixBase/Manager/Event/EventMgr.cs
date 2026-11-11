
namespace Ux
{
    public partial class EventMgr : Singleton<EventMgr>, IFrameUpdate
    {
       //全局事件,不允许直接监听int,必须使用EvenType或MainEventType枚举
        private EventSystem _defaultSystem;

        /// <summary>
        /// 所有 EventSystem 实例。全局只挂一条 GameMethod.Update，
        /// 实例数增长不会让全局委托链变长；遍历中 ReleaseSystem 也安全（延迟移除）。
        /// </summary>
        private readonly FrameTickList<IEventSystem> _systems = new(ReferenceEqualityComparer<IEventSystem>.Instance);

        protected override void OnCreated()
        {
            FrameDriver.Register(this);
            _defaultSystem = CreateSystem(200);
        }

        void IFrameUpdate.OnFrameUpdate()
        {
            _systems.Tick(_TickOne);
        }        
        private static void _TickOne(IEventSystem system) => system.Tick();

        /// <summary>
        /// 创建一个独立的事件系统（用于限流隔离，例如网络消息单独一套 _exeLimit）。
        /// 不要按业务对象（每个界面/每个组件）创建实例，那样会退化成"对象各自挂全局"。
        /// </summary>
        public EventSystem CreateSystem(int exeLimit = int.MaxValue)
        {
            var system = Pool.Get<EventSystem>();
            (system as IEventSystem).Init(exeLimit);
            _systems.Add(system);
            return system;
        }

        public void ReleaseSystem(EventSystem eventSystem)
        {
            if (eventSystem == null) return;
            if (!_systems.Contains(eventSystem)) return;   // 防止重复 Release 被二次回池
            _systems.Remove(eventSystem);
            (eventSystem as IEventSystem).Release();
        }
    }
}
