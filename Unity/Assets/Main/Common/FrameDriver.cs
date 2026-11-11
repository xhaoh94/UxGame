using System;
using System.Collections.Generic;

namespace Ux
{
    /// <summary>域级每帧更新入口：实现后通过 FrameDriver.Register 注册。</summary>
    public interface IFrameUpdate
    {
        void OnFrameUpdate();
    }

    /// <summary>域级 LateUpdate 入口。</summary>
    public interface IFrameLateUpdate
    {
        void OnFrameLateUpdate();
    }

    /// <summary>域级 FixedUpdate 入口。</summary>
    public interface IFrameFixedUpdate
    {
        void OnFrameFixedUpdate();
    }

    /// <summary>域级 Quit 入口。</summary>
    public interface IFrameQuit
    {
        void OnFrameQuit();
    }

    /// <summary>
    /// 帧驱动注册中心。
    ///
    /// 设计目的：不再向业务暴露「任意委托订阅全局 Update」的入口。
    /// 想参与每帧驱动必须实现对应接口并 Register —— 由类型系统强制，
    /// 业务无法再写 GameMethod.Update += lambda。
    ///
    /// 使用规范：
    /// 1) 域级系统（ECS、事件、时间、UI…）实现接口 + Register，整个域只注册一次；
    /// 2) 对象级更新（界面、组件、特效）不要注册到这里：
    ///    数量少用 TimeMgr.Ins.Frame，数量多用所属域的 FrameTickList；
    /// 3) Register / Unregister 成对调用（Init 时注册，Release 时解除）。
    /// </summary>
    public static class FrameDriver
    {
        private static readonly FrameTickList<IFrameUpdate> _updates =
            new(ReferenceEqualityComparer<IFrameUpdate>.Instance);

        private static readonly FrameTickList<IFrameLateUpdate> _lateUpdates =
            new(ReferenceEqualityComparer<IFrameLateUpdate>.Instance);

        private static readonly FrameTickList<IFrameFixedUpdate> _fixedUpdates =
            new(ReferenceEqualityComparer<IFrameFixedUpdate>.Instance);

        private static readonly FrameTickList<IFrameQuit> _quits =
            new(ReferenceEqualityComparer<IFrameQuit>.Instance);

        public static void Register(IFrameUpdate system) => _updates.Add(system);
        public static void Unregister(IFrameUpdate system) => _updates.Remove(system);

        public static void Register(IFrameLateUpdate system) => _lateUpdates.Add(system);
        public static void Unregister(IFrameLateUpdate system) => _lateUpdates.Remove(system);

        public static void Register(IFrameFixedUpdate system) => _fixedUpdates.Add(system);
        public static void Unregister(IFrameFixedUpdate system) => _fixedUpdates.Remove(system);

        public static void Register(IFrameQuit system) => _quits.Add(system);
        public static void Unregister(IFrameQuit system) => _quits.Remove(system);

        internal static void TickUpdate() => _updates.Tick(_OnUpdate);
        internal static void TickLateUpdate() => _lateUpdates.Tick(_OnLateUpdate);
        internal static void TickFixedUpdate() => _fixedUpdates.Tick(_OnFixedUpdate);
        internal static void TickQuit() => _quits.Tick(_OnQuit);

        private static void _OnUpdate(IFrameUpdate system) => system.OnFrameUpdate();
        private static void _OnLateUpdate(IFrameLateUpdate system) => system.OnFrameLateUpdate();
        private static void _OnFixedUpdate(IFrameFixedUpdate system) => system.OnFrameFixedUpdate();
        private static void _OnQuit(IFrameQuit system) => system.OnFrameQuit();
    }
}
