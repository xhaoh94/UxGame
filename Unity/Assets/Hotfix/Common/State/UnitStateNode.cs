using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ux;
using Cysharp.Threading.Tasks;

namespace Ux
{
    public abstract class UnitStateNode : StateNode
    {
        public virtual string AnimName { get; } = null;        
        public AnimComponent Anim { get; private set; }

        public override void Enter(object args = null)
        {
            base.Enter(args);
            RefreshAnim();
        }

        protected override void OnRelease()
        {            
            if (!string.IsNullOrEmpty(AnimName))
            {
                Anim?.RemoveAnimation(AnimName);
            }
            Anim = null;
        }

        public async UniTaskVoid AddAnimation(AnimComponent anim)
        {
            Anim = anim;
            if (string.IsNullOrEmpty(AnimName))
            {
                return;
            }

            var clip = await ResMgr.Ins.LoadAssetAsync<AnimationClip>(AnimName);
            anim.AddAnimation(AnimName, clip);
            if (Machine.CurrentNode == this)
            {
                RefreshAnim();
            }
        }

        protected virtual void RefreshAnim()
        {
            if (string.IsNullOrEmpty(AnimName))
            {
                return;
            }

            if (Anim != null)
            {
                Anim.Play(AnimName, 0.3f);
            }
        }
    }
}