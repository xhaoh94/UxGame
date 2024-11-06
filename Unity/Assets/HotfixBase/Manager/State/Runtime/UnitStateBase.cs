using System;
using System.Collections.Generic;

namespace Ux
{
    public interface IUnitState : IStateNode
    {
        void Set(long id);
        bool IsMute { get; }
        int Priority { get; }
        bool IsValid { get; }
        bool IsAdditive { get; }
        long OwnerID { get; }
        UnitStateMachine StateMachine { get; }
        List<StateConditionBase> Conditions { get; }
    }

    //public interface ITimelineUnitState : IUnitState
    //{
    //    void Set(PlayableDirectorComponent director);
    //}
    public abstract class UnitStateBase : StateNode, IUnitState
    {
        public UnitStateMachine StateMachine => Machine as UnitStateMachine;
        public StateAsset Asset { get; private set; }
        public virtual bool IsMute => Asset.isMute;
        public virtual int Priority => Asset.priority;
        public virtual bool IsAdditive { get; } = false;
        public virtual string AssetName { get; } = null;
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
            if(string.IsNullOrEmpty(AssetName)) return;
            Asset = ResMgr.Ins.LoadAsset<StateAsset>($"{AssetName}");
            Conditions = Asset.conditions;
        }
                

        protected override void OnRelease()
        {
            base.OnRelease();
            OwnerID = 0;
            Conditions = null;
        }
    }

    //public abstract class UnitStateAnim : UnitStateBase, ITimelineUnitState
    //{
    //    void IUnitAnimState.Set(Ux.AnimComponent anim)
    //    {
    //        Anim = anim;
    //    }
    //    public AnimComponent Anim { get; private set; }
    //    protected override bool OnCheckValid()
    //    {
    //        if (Anim == null) return false;
    //        return base.OnCheckValid();
    //    }
    //    protected override void OnEnter()
    //    {
    //        base.OnEnter();
    //        LoadAsset().Forget();
    //    }
    //    protected override void OnExit()
    //    {
    //        base.OnExit();
    //    }
    //    protected override void OnRelease()
    //    {
    //        base.OnRelease();
    //        if (!string.IsNullOrEmpty(ResName))
    //        {
    //            Anim?.RemoveAnimation(ResName);
    //        }
    //        Anim = null;
    //    }

    //    protected async UniTaskVoid LoadAsset()
    //    {
    //        if (string.IsNullOrEmpty(ResName))
    //        {
    //            return;
    //        }
    //        if (Anim == null)
    //        {
    //            return;
    //        }
    //        if (!Anim.Has(ResName))
    //        {
    //            var clip = await ResMgr.Ins.LoadAssetAsync<AnimationClip>(ResName);
    //            Anim.AddAnimation(ResName, clip);
    //        }
    //        if (Machine.CurrentNode != this)
    //        {
    //            return;
    //        }
    //        Anim.Play(ResName, 0.3f);
    //    }

    //    public void Stop()
    //    {
    //        if (!string.IsNullOrEmpty(ResName))
    //        {
    //            Anim?.Stop(ResName);
    //        }
    //    }
    //}
    //public abstract class UnitStateTimeLine : UnitStateBase, IUnitTimelineState
    //{
    //    public virtual DirectorWrapMode WarpMode { get; } = DirectorWrapMode.None;
    //    void IUnitTimelineState.Set(Ux.PlayableDirectorComponent director)
    //    {
    //        PlayableDirector = director;
    //    }
    //    public PlayableDirectorComponent PlayableDirector { get; private set; }
    //    protected override bool OnCheckValid()
    //    {
    //        if (PlayableDirector == null) return false;
    //        return base.OnCheckValid();
    //    }
    //    protected override void OnRelease()
    //    {
    //        base.OnRelease();
    //        PlayableDirector = null;
    //    }
    //    protected override void OnEnter()
    //    {
    //        base.OnEnter();
    //        if (PlayableDirector != null)
    //        {
    //            PlayableDirector.OnPlayEndEvent += OnPlayEnd;
    //        }
    //        LoadAsset().Forget();
    //        if (Machine.PreviousNode is UnitStateAnim AnimNode)
    //        {
    //            AnimNode.Stop();
    //        }
    //    }
    //    protected override void OnExit()
    //    {
    //        base.OnExit();
    //        if (PlayableDirector != null)
    //        {
    //            PlayableDirector.OnPlayEndEvent -= OnPlayEnd;
    //        }
    //    }
    //    public async UniTaskVoid LoadAsset()
    //    {

    //        if (string.IsNullOrEmpty(ResName))
    //        {
    //            return;
    //        }
    //        if (PlayableDirector == null)
    //        {
    //            return;
    //        }
    //        var asset = await StateMgr.Ins.GetTimeLineAssetAsync(ResName);
    //        if (Machine.CurrentNode != this)
    //        {
    //            return;
    //        }
    //        PlayableDirector.Play(asset, WarpMode);
    //    }
    //    protected virtual void OnPlayEnd(PlayableDirector playableDirector)
    //    {
    //        StateMgr.Ins.Update(OwnerID);
    //    }
    //}
}