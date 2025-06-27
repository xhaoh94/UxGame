using System;
using System.Collections.Generic;

namespace Ux
{
    public partial class EventMgr : Singleton<EventMgr>
    {
       //全局事件,不允许直接监听int,必须使用EvenType或MainEventType枚举
        private EventSystem _defaultSystem;
        
        protected override void OnCreated()
        {
            GameMethod.LowMemory += _LowMemory;
            _defaultSystem = CreateSystem(200);
        }
        void _LowMemory()
        {
            _fastMethodRefDic.Clear();
        }

        public EventSystem CreateSystem(int exeLimit = int.MaxValue)
        {
            var system = Pool.Get<EventSystem>();
            system.Init(exeLimit);
            return system;
        }
       
        public void SetEvtAttribute<T>() where T : Attribute, IEvtAttribute
        {
            _defaultSystem.SetEvtAttribute<T>();
        }
        public void RegisterEventTrigger(IEventTrigger triggerObject)
        {
            _defaultSystem.RegisterEventTrigger(triggerObject);
        }
        public void RegisterFastMethod(object target)
        {
            _defaultSystem.RegisterFastMethod(target);
        }

        
    }
}