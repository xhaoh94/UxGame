using System;

namespace Ux
{
    /// <summary>
    /// ECS 的更新调度。
    ///
    /// 组件不再逐个挂到全局帧事件上，而是登记到这里，由一条固定循环统一驱动：
    ///   1) 增删是 O(1)（HashSet + List），不会像多播委托那样每次复制整条委托链；
    ///   2) 每帧遍历连续 List，缓存友好，遍历顺序稳定；
    ///   3) 可以整体暂停/恢复（Release），不会和 UI、其他系统混在一起。
    ///
    /// 层级关系：FrameDriver（域级）→ EntityScheduler（ECS 域）→ 组件列表。
    /// 本类自己实现 IFrame* 接口，不再需要额外的 Bridge 包装。
    /// </summary>
    public sealed class EntityScheduler : IFrameUpdate, IFrameLateUpdate, IFrameFixedUpdate, IFrameQuit
    {
        public static readonly EntityScheduler Instance = new();

        private readonly FrameTickList<IUpdateSystem> _updates =
            new(ReferenceEqualityComparer<IUpdateSystem>.Instance);

        private readonly FrameTickList<ILateUpdateSystem> _lateUpdates =
            new(ReferenceEqualityComparer<ILateUpdateSystem>.Instance);

        private readonly FrameTickList<IFixedUpdateSystem> _fixedUpdates =
            new(ReferenceEqualityComparer<IFixedUpdateSystem>.Instance);

        private readonly FrameTickList<IApplicationQuitSystem> _quits =
            new(ReferenceEqualityComparer<IApplicationQuitSystem>.Instance);

        private static bool _inited;

        public static void Init()
        {
            if (_inited) return;
            _inited = true;

            // 同时实现 4 个接口，必须按接口显式注册，否则重载有歧义。
            FrameDriver.Register((IFrameUpdate)Instance);
            FrameDriver.Register((IFrameLateUpdate)Instance);
            FrameDriver.Register((IFrameFixedUpdate)Instance);
            FrameDriver.Register((IFrameQuit)Instance);
        }

        public static void Release()
        {
            if (!_inited) return;
            _inited = false;

            FrameDriver.Unregister((IFrameUpdate)Instance);
            FrameDriver.Unregister((IFrameLateUpdate)Instance);
            FrameDriver.Unregister((IFrameFixedUpdate)Instance);
            FrameDriver.Unregister((IFrameQuit)Instance);

            Instance._updates.Clear();
            Instance._lateUpdates.Clear();
            Instance._fixedUpdates.Clear();
            Instance._quits.Clear();
        }

        public static void Add(IUpdateSystem system) => Instance._updates.Add(system);
        public static void Remove(IUpdateSystem system) => Instance._updates.Remove(system);

        public static void Add(ILateUpdateSystem system) => Instance._lateUpdates.Add(system);
        public static void Remove(ILateUpdateSystem system) => Instance._lateUpdates.Remove(system);

        public static void Add(IFixedUpdateSystem system) => Instance._fixedUpdates.Add(system);
        public static void Remove(IFixedUpdateSystem system) => Instance._fixedUpdates.Remove(system);

        public static void Add(IApplicationQuitSystem system) => Instance._quits.Add(system);
        public static void Remove(IApplicationQuitSystem system) => Instance._quits.Remove(system);

        void IFrameUpdate.OnFrameUpdate()
        {
            _updates.Tick(_OnUpdate);
        }

        void IFrameLateUpdate.OnFrameLateUpdate()
        {
            _lateUpdates.Tick(_OnLateUpdate);

            // 实体销毁延迟队列（原 Entity._DequeueDelay），归 ECS 域的 LateUpdate 管。
            Entity.TickDelay();
        }

        void IFrameFixedUpdate.OnFrameFixedUpdate()
        {
            _fixedUpdates.Tick(_OnFixedUpdate);
        }

        void IFrameQuit.OnFrameQuit()
        {
            _quits.Tick(_OnQuit);
        }

        private static void _OnUpdate(IUpdateSystem system) => system.OnUpdate();
        private static void _OnLateUpdate(ILateUpdateSystem system) => system.OnLateUpdate();
        private static void _OnFixedUpdate(IFixedUpdateSystem system) => system.OnFixedUpdate();
        private static void _OnQuit(IApplicationQuitSystem system) => system.OnApplicationQuit();
    }
}
