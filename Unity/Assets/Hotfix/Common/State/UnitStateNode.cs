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
        public Entity Unit { get; private set; }
        public AnimComponent Anim { get; private set; }


        public override void Create(StateMachine machine, object args = null, bool isFromPool = true)
        {
            Unit = args as Entity;
            base.Create(machine, args, isFromPool);
        }

        public override void Enter(object args = null)
        {
            base.Enter(args);
            RefreshAnim();
        }

        protected override void OnRelease()
        {
            Unit = null;
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

            var handle = ResMgr.Ins.LoadAssetAsync<AnimationClip>(AnimName);
            await handle.ToUniTask();
            var clip = handle.GetAssetObject<AnimationClip>();
            handle.Release();
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