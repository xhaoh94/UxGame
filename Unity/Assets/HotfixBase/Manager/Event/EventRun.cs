using UnityEngine;

namespace Ux
{
    public interface IEventRun
    {
        EventMgr.EventSystem Run(int eType);
        EventMgr.EventSystem Run<A>(int eType, A a);
        EventMgr.EventSystem Run<A, B>(int eType, A a, B b);
        EventMgr.EventSystem Run<A, B, C>(int eType, A a, B b, C c);
    }
    partial class EventMgr : IEventRun
    {
        EventSystem IEventRun.Run(int eType)
        {
            return _defaultSystem.Run(eType);
        }

        EventSystem IEventRun.Run<A>(int eType, A a)
        {
            return _defaultSystem.Run(eType, a);
        }

        EventSystem IEventRun.Run<A, B>(int eType, A a, B b)
        {
            return _defaultSystem.Run(eType, a, b);
        }

        EventSystem IEventRun.Run<A, B, C>(int eType, A a, B b, C c)
        {
            return _defaultSystem.Run(eType, a, b, c);
        }

        //Á¢¼´µ÷ÓÃ
        public void Immediate()
        {
            _defaultSystem.Immediate();
        }
    }
}
