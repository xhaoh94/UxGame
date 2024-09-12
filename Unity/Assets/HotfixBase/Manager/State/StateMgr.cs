using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace Ux
{
    public class StateMgr : Singleton<StateMgr>
    {
        Dictionary<string, StateTimeline> resToData = new Dictionary<string, StateTimeline>();
        public async UniTask<StateTimeline> GetTimeLineAssetAsync(string res)
        {
            if (resToData.TryGetValue(res, out var data))
            {
                return data;
            }
            var asset = await ResMgr.Ins.LoadAssetAsync<UnityEngine.Timeline.TimelineAsset>(res);

            data = new StateTimeline(asset);
            resToData[res] = data;
            return data;
        }

        Dictionary<long, Dictionary<StateConditionBase.ConditionType, HashSet<IUnitState>>> _typeStates = new();
        Dictionary<long, List<IUnitState>> _unitStates = new();
        Dictionary<long, HashSet<string>> _tempBoolVar = new();
        protected override void OnCreated()
        {
            base.OnCreated();
        }
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
            if (!_typeStates.TryGetValue(id, out var temDict))
            {
                return;
            }
            if (!temDict.TryGetValue(type, out var stateList))
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
        public void Update(long id)
        {
            if (!_unitStates.TryGetValue(id, out var stateList))
            {
                return;
            }
            foreach (var state in stateList)
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

        public void Remove(long id)
        {
            _unitStates.Remove(id);
            _tempBoolVar.Remove(id);
            _typeStates.Remove(id);
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
                unitStates = new List<IUnitState>();
                _unitStates.Add(id, unitStates);
            }
            unitStates.Add(unitState);
            if (Sort)
            {
                unitStates.Sort((a, b) =>
                {
                    if (b.Priority == a.Priority)
                    {
                        return unitStates.IndexOf(a) - unitStates.IndexOf(b);
                    }
                    return b.Priority - a.Priority;
                });
            }

            if (!_typeStates.TryGetValue(id, out var temDict))
            {
                temDict = new();
                _typeStates.Add(id, temDict);
            }
            foreach (var conditin in unitState.Conditions)
            {
                conditin.Init(unitState);
                if (!temDict.TryGetValue(conditin.Condition, out var units))
                {
                    units = new HashSet<IUnitState>();
                    temDict.Add(conditin.Condition, units);
                }
                units.Add(unitState);
            }
        }

    }
}
