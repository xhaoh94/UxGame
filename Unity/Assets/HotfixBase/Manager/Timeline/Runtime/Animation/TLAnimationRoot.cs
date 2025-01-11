using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Ux
{

    public partial class TLAnimationRoot : Entity
    {
        public TimelineComponent Component => ParentAs<TimelineComponent>();
        private readonly List<TLAnimationOutput> _outputs = new(5);
        protected override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < _outputs.Count; i++)
            {
                _outputs[i] = null;
            }
        }

        public TLAnimationOutput GetOutput(Animator animator)
        {
            if (animator == null)
                return null;
            foreach (var _output in _outputs)
            {
                if (_output!=null && animator == _output.Animator)
                    return _output;
            }

            TLAnimationOutput output = null;
            int index = _outputs.FindIndex(x => x == null);
            if (index == -1)
            {                
                output = Add<TLAnimationOutput, Animator, int>(animator, _outputs.Count + 1);
                _outputs.Add(output);
            }
            else
            {
                output = Add<TLAnimationOutput, Animator, int>(animator, index);
                _outputs[index] = output;
            }

            return output;
        }
        public void RemoveOutput(int index)
        {
            if (index >= 0 && index < _outputs.Count)
            {
                _outputs[index] = null;
            }
        }
    }
}
