using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ux
{
    [System.Serializable]
    public class TriggerPA : PlayableAsset
    {        
        public TriggerData triggerData;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
        {
            var playable = ScriptPlayable<TriggerPB>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.go = go;
            behaviour.triggerData = triggerData;
            return playable;
        }
    }
    [Serializable]
    public class TriggerPB : PlayableBehaviour
    {
        public TriggerData triggerData;
        public GameObject go;
        public override void OnGraphStart(Playable playable)
        {
        }

        public override void OnGraphStop(Playable playable)
        {
            playable.GetGraph();
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (go == null) return;
            var mono = go.GetComponent<EntityMono>();
            if (mono == null) return;
            var player = mono.GetEntity<Unit>();
            if (player == null) return;
            var operate = player.GetComponent<OperateComponent>();
            if (operate == null) return;
            operate.AddTrigger(triggerData);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (go == null) return;
            var mono = go.GetComponent<EntityMono>();
            if (mono == null) return;
            var player = mono.GetEntity<Unit>();
            if (player == null) return;
            var operate = player.GetComponent<OperateComponent>();
            if (operate == null) return;
            operate.RemoveTrigger();
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {

        }
    }
}
