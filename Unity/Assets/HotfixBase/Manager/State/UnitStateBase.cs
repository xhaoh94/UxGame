using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ux
{
    public abstract class UnitStateBase : StateNode
    {
        public virtual bool IsMute { get; }
        public virtual int Priority { get; }
        public virtual long OwnerID { get; }
        public virtual string ResName { get; } = null;
        public List<StateConditionBase> Conditions { get; protected set; }
        public virtual bool IsValid
        {
            get
            {
                if (Conditions == null) return false;
                foreach (var condition in Conditions)
                {
                    if (!condition.IsValid) return false;
                }
                return true;
            }
        }

        protected override void OnCreate(object args = null)
        {
            base.OnCreate(args);
            InitConditions();
            StateMgr.Ins.AddState(this);
        }

        protected virtual void InitConditions()
        {

        }

        protected virtual StateConditionBase CreateCondition(string condition, params object[] args)
        {
            Type type = null;
            switch (condition)
            {
                case nameof(StateCondition):
                    type = typeof(StateCondition);
                    break;
                case nameof(TemBoolVarCondition):
                    type = typeof(TemBoolVarCondition);
                    break;
                case nameof(ActionMoveCondition):
                    type = typeof(ActionMoveCondition);
                    break;
                case nameof(ActionKeyboardCondition):
                    type = typeof(ActionKeyboardCondition);
                    break;
                case nameof(ActionInputCondition):
                    type = typeof(ActionInputCondition);
                    break;
            }
            return (StateConditionBase)Activator.CreateInstance(type, args);
        }

    }

    public abstract class UnitStateAnim : UnitStateBase
    {
        public virtual AnimComponent Anim { get; }
        protected override bool OnCheckValid()
        {
            if (Anim == null) return false;
            return base.OnCheckValid();
        }
        protected override void OnEnter()
        {
            base.OnEnter();
            LoadAsset().Forget();
        }
        protected override void OnExit()
        {
            base.OnExit();
        }
        protected override void OnRelease()
        {
            if (!string.IsNullOrEmpty(ResName))
            {
                Anim?.RemoveAnimation(ResName);
            }
        }

        protected async UniTaskVoid LoadAsset()
        {
            if (string.IsNullOrEmpty(ResName))
            {
                return;
            }
            if (Anim == null)
            {
                return;
            }
            if (!Anim.Has(ResName))
            {
                var clip = await ResMgr.Ins.LoadAssetAsync<AnimationClip>(ResName);
                Anim.AddAnimation(ResName, clip);
            }
            if (Machine.CurrentNode != this)
            {
                return;
            }
            Anim.Play(ResName, 0.3f);
        }
    }
    public abstract class UnitStateTimeLine : UnitStateBase
    {
        public virtual PlayableDirectorComponent PlayableDirector { get; }
        protected override bool OnCheckValid()
        {
            if (PlayableDirector == null) return false;
            return base.OnCheckValid();
        }
        protected override void OnEnter()
        {
            base.OnEnter();
            if (PlayableDirector != null)
            {
                PlayableDirector.OnPlayEndEvent += OnPlayEnd;
            }
            LoadAsset().Forget();
        }
        protected override void OnExit()
        {
            base.OnExit();
            if (PlayableDirector != null)
            {
                PlayableDirector.OnPlayEndEvent -= OnPlayEnd;
            }
        }
        public async UniTaskVoid LoadAsset()
        {

            if (string.IsNullOrEmpty(ResName))
            {
                return;
            }
            if (PlayableDirector == null)
            {
                return;
            }
            var asset = await StateMgr.Ins.GetSkillAssetAsync(ResName);
            if (Machine.CurrentNode != this)
            {
                return;
            }
            PlayableDirector.SetPlayableAsset(asset);
            PlayableDirector.Play();
        }
        protected virtual void OnPlayEnd(PlayableDirector playableDirector)
        {
        }
    }
}