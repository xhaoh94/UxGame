//自动生成的代码，请勿修改!!!
using System;
using System.Collections.Generic;
namespace Ux
{
    public static partial class StateMgrEx
    {
        readonly static Dictionary<string, HashSet<Type>> _stateGroup = new Dictionary<string, HashSet<Type>>()
        {
            { "HeroZs",new HashSet<Type>() { typeof(HeroZSAttack), typeof(HeroZSRun), typeof(HeroZSIdle),}},
        };
        public static void InitGroup(this UnitStateMachine machine, string group, long OwnerID)
        {
            if (string.IsNullOrEmpty(group)) return;
            if (_stateGroup.TryGetValue(group, out var states))
            {
                int index = 0;
                foreach (var state in states)
                {
                    var item = Activator.CreateInstance(state) as IUnitState;
                    item.Set(OwnerID);
                    machine.AddNode(item);
                    StateMgr.Ins.AddState(item, index == states.Count - 1);
                    index++;
                }
            }
        }
    }
}
