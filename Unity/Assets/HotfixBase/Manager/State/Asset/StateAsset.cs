using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public class StateAsset : ScriptableObject
    {
        public string group;
        public int priority = 0;
        public bool isMute;        
        public string stateName = string.Empty;
        public string stateDesc = string.Empty;

        [SerializeReference]
        public List<StateConditionBase> conditions = new List<StateConditionBase>();

    }
}