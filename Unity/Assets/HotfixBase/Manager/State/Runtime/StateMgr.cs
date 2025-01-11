using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace Ux
{
    class UnitStateMap
    {

        Dictionary<StateConditionBase.ConditionType, HashSet<IUnitState>> _typeStates = new();
        List<IUnitState> _states = new();
        public void Add(IUnitState unitState)
        {
            _states.Add(unitState);
            foreach (var conditin in unitState.Conditions)
            {
                conditin.Init(unitState);
                if (!_typeStates.TryGetValue(conditin.Condition, out var units))
                {
                    units = new HashSet<IUnitState>();
                    _typeStates.Add(conditin.Condition, units);
                }
                units.Add(unitState);
            }
        }

        public void Update( StateConditionBase.ConditionType type)
        {
            if (!_typeStates.TryGetValue(type, out var stateList))
            {
                return;
            }

            foreach (var state in stateList)
            {
                if (state.IsMute) continue;
                if (state.IsValid)
                {
                    state.StateMachine.Enter(state);
                    if (!state.IsAdditive)
                    {
                        break;
                    }
                }
                else
                {
                    state.StateMachine.Exit(state);
                }
            }
        }
        public void Update()
        {
            foreach (var state in _states)
            {
                if (state.IsMute) continue;
                if (state.IsValid)
                {
                    if (state.IsAdditive)
                    {
                        state.StateMachine.Enter(state);
                    }
                    else
                    {
                        state.StateMachine.Enter(state.Name);
                        break;
                    }
                }
                else
                {
                    if (state.IsAdditive)
                    {
                        state.StateMachine.Exit(state);
                    }
                }
            }
        }

        public void Release()
        {
            _states.Clear();
            _typeStates.Clear();
            Pool.Push(this);
        }
        public void Sort()
        {
            _states.Sort((a, b) =>
            {
                if (b.Priority == a.Priority)
                {
                    return _states.IndexOf(a) - _states.IndexOf(b);
                }
                return b.Priority - a.Priority;
            });
        }
    }
    public class StateMgr : Singleton<StateMgr>
    {        
        Dictionary<long, UnitStateMap> _unitStates = new();
        Dictionary<long, HashSet<string>> _tempBoolVar = new();
        public void AddTempBoolVar(long id, string key)
        {
            if (!_tempBoolVar.TryGetValue(id, out var dict))
            {
                dict = new HashSet<string>();
                _tempBoolVar.Add(id, dict);
            }
            dict.Add(key);
        }
        public void RemoveTempBoolVar(long id, string key)
        {
            if (!_tempBoolVar.TryGetValue(id, out var dict))
            {
                return;
            }
            dict.Remove(key);
        }
        public bool CheckTempBoolVar(long id, string key)
        {
            if (!_tempBoolVar.TryGetValue(id, out var dict))
            {
                return false;
            }
            return dict.Contains(key);
        }

        public void Update(long id, StateConditionBase.ConditionType type)
        {
            if (!_unitStates.TryGetValue(id, out var unitStates))
            {
                return;
            }
            unitStates.Update(type);
        }
        public void Update(long id)
        {
            if (!_unitStates.TryGetValue(id, out var unitStates))
            {
                return;
            }
            unitStates.Update();
        }

        public void Remove(long id)
        {
            if(_unitStates.TryGetValue(id,out var unitStateDictionary))
            {
                unitStateDictionary.Release();
                _unitStates.Remove(id);
            }
            _tempBoolVar.Remove(id);            
        }

        public void AddState(IUnitState unitState, bool Sort)
        {
            if (unitState == null)
            {
                return;
            }
            var id = unitState.OwnerID;
            if (id == 0)
            {
                Log.Error($"需要实现状态机[{unitState.Name}]所属拥有者ID");
                return;
            }
            if (!_unitStates.TryGetValue(id, out var unitStates))
            {
                unitStates = Pool.Get<UnitStateMap>();
                _unitStates.Add(id, unitStates);
            }
            unitStates.Add(unitState);
            if (Sort)
            {
                unitStates.Sort();
            }            
        }
    }
}
