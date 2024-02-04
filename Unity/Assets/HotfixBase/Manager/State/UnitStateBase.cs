using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Ux
{
    public interface IUnitState : IStateNode
    {
        void Set(long id);
        bool IsMute { get; }
        int Priority { get; }
        bool IsValid { get; }
        long OwnerID { get; }
        List<StateConditionBase> Conditions { get; }
    }
    public interface IUnitAnimState : IUnitState
    {
        void Set(AnimComponent anim);
    }
    public interface IUnitTimelineState : IUnitState
    {
        void Set(PlayableDirectorComponent director);
    }
    public abstract class UnitStateBase : StateNode, IUnitState
    {
        public virtual bool IsMute { get; }
        public virtual int Priority { get; }
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

        public long OwnerID { get; private set; }
        void IUnitState.Set(long id)
        {
            OwnerID = id;
            InitConditions();
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
                case nameof(ActionKeyboardCondition):
                    type = typeof(ActionKeyboardCondition);
                    break;
                case nameof(ActionInputCondition):
                    type = typeof(ActionInputCondition);
                    break;
            }
            if (type == null)
            {
                Log.Error($"没有找到名字为{condition}的条件");
                return null;
            }
            return (StateConditionBase)Activator.CreateInstance(type, args);
        }


        protected override void OnRelease()
        {
            base.OnRelease();
            OwnerID = 0;
            Conditions = null;
        }
    }

    public abstract class UnitStateAnim : UnitStateBase, IUnitAnimState
    {
        void IUnitAnimState.Set(Ux.AnimComponent anim)
        {
            Anim = anim;
        }
        public AnimComponent Anim { get; private set; }
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
            base.OnRelease();
            if (!string.IsNullOrEmpty(ResName))
            {
                Anim?.RemoveAnimation(ResName);
            }
            Anim = null;
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
    public abstract class UnitStateTimeLine : UnitStateBase, IUnitTimelineState
    {
        void IUnitTimelineState.Set(Ux.PlayableDirectorComponent director)
        {
            PlayableDirector = director;
        }
        public PlayableDirectorComponent PlayableDirector { get; private set; }
        protected override bool OnCheckValid()
        {
            if (PlayableDirector == null) return false;
            return base.OnCheckValid();
        }
        protected override void OnRelease()
        {
            base.OnRelease();
            PlayableDirector = null;
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
            StateMgr.Ins.Update(OwnerID);
        }
    }
}