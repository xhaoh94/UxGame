using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ux
{
    public class StateMgr : Singleton<StateMgr>
    {
        Dictionary<string, SkillAsset> resToData = new Dictionary<string, SkillAsset>();
        public async UniTask<SkillAsset> GetSkillAssetAsync(string res)
        {
            if (resToData.TryGetValue(res, out var data))
            {
                return data;
            }
            var asset = await ResMgr.Ins.LoadAssetAsync<TimelineAsset>(res);

            data = new SkillAsset(asset);
            resToData[res] = data;
            return data;
        }

        Dictionary<long, List<IUnitState>> UnitStates = new Dictionary<long, List<IUnitState>>();
        Dictionary<long, Dictionary<string, bool>> TempBoolVar = new Dictionary<long, Dictionary<string, bool>>();
        HashSet<long> Move = new HashSet<long>();

        public void AddTempBoolVar(long id, string key, bool value)
        {
            if (!TempBoolVar.TryGetValue(id, out var dict))
            {
                dict = new Dictionary<string, bool>();
                TempBoolVar.Add(id, dict);
            }
            dict[key] = value;
        }
        public void RevemoTempBoolVar(long id, string key)
        {
            if (!TempBoolVar.TryGetValue(id, out var dict))
            {
                return;
            }
            dict.Remove(key);
        }
        public bool CheckTempBoolVar(long id, string key, bool value)
        {
            if (!TempBoolVar.TryGetValue(id, out var dict))
            {
                return false;
            }
            if (!dict.TryGetValue(key, out var temV))
            {
                return false;
            }
            return temV == value;
        }

        public void Update(long id)
        {
            if (!UnitStates.TryGetValue(id, out var stateList))
            {
                return;
            }
            foreach (var state in stateList)
            {
                if (state.IsMute) continue;
                if (state.IsValid)
                {
                    if (state.Machine.CurrentNode != state)
                    {
                        state.Machine.Enter(state.Name);
                    }
                    break;
                }
            }
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
            if (!UnitStates.TryGetValue(id, out var unitStates))
            {
                unitStates = new List<IUnitState>();
                UnitStates.Add(id, unitStates);
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

            foreach (var conditin in unitState.Conditions)
            {
                conditin.Init(unitState);
            }


        }
    }
}
