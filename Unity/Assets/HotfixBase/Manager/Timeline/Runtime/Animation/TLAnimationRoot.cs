using System.Collections.Generic;
using UnityEngine;

namespace Ux
{
    public partial class TLAnimationRoot : Entity
    {
        public TimelineComponent Component => ParentAs<TimelineComponent>();
        private readonly List<TLAnimationOutput> _outputs = new(5);

        protected override void OnDestroy()
        {
            _outputs.Clear();
        }

        public TLAnimationOutput GetOutput(Animator animator)
        {
            if (animator == null)
            {
                return null;
            }

            foreach (var output in _outputs)
            {
                if (output != null && !output.IsDestroy && animator == output.Animator)
                {
                    return output;
                }
            }

            var index = _outputs.FindIndex(x => x == null);
            if (index < 0)
            {
                index = _outputs.Count;
                var output = Add<TLAnimationOutput, Animator, int>(animator, index, IsFromPool);
                if (output == null)
                {
                    return null;
                }
                _outputs.Add(output);
                return output;
            }

            var reusedOutput = Add<TLAnimationOutput, Animator, int>(animator, index, IsFromPool);
            if (reusedOutput == null)
            {
                return null;
            }
            _outputs[index] = reusedOutput;
            return reusedOutput;
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
