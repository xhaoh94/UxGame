using System.Collections;
using UnityEngine;

namespace Ux
{
    public class UnitStateBase : StateNode
    {       
        public virtual string ResName { get; } = null;
    }

    public abstract class UnitAnimNode : UnitStateBase
    {        
        //protected override bool OnCheckValid(object args = null)
        //{
        //    if (Anim == null) return false;
        //    return base.OnCheckValid();
        //}
        //protected override void OnEnter(object args = null)
        //{
        //    base.OnEnter(args);
        //    LoadAsset().Forget();
        //}
        //protected override void OnExit()
        //{
        //    base.OnExit();
        //}
        //protected override void OnRelease()
        //{
        //    if (!string.IsNullOrEmpty(ResName))
        //    {
        //        Anim?.RemoveAnimation(ResName);
        //    }
        //}

        //protected async UniTaskVoid LoadAsset()
        //{
        //    if (string.IsNullOrEmpty(ResName))
        //    {
        //        return;
        //    }
        //    if (Anim == null)
        //    {
        //        return;
        //    }
        //    if (!Anim.Has(ResName))
        //    {
        //        var clip = await ResMgr.Ins.LoadAssetAsync<AnimationClip>(ResName);
        //        Anim.AddAnimation(ResName, clip);
        //    }
        //    if (Machine.CurrentNode != this)
        //    {
        //        return;
        //    }
        //    Anim.Play(ResName, 0.3f);
        //}
    }
    public abstract class UnitTimeLineNode : UnitStateBase
    {        

        //protected override bool OnCheckValid(object args = null)
        //{
        //    if (PlayableDirector == null) return false;
        //    return base.OnCheckValid();
        //}
        //protected override void OnEnter(object args = null)
        //{
        //    base.OnEnter(args);
        //    if (PlayableDirector != null)
        //    {
        //        PlayableDirector.OnPlayEndEvent += OnPlayEnd;
        //    }
        //    LoadAsset().Forget();
        //}
        //protected override void OnExit()
        //{
        //    base.OnExit();
        //    if (PlayableDirector != null)
        //    {
        //        PlayableDirector.OnPlayEndEvent -= OnPlayEnd;
        //    }
        //}
        //public async UniTaskVoid LoadAsset()
        //{

        //    if (string.IsNullOrEmpty(ResName))
        //    {
        //        return;
        //    }
        //    if (PlayableDirector == null)
        //    {
        //        return;
        //    }
        //    var asset = await SkillMgr.Ins.GetSkillAssetAsync(ResName);
        //    if (Machine.CurrentNode != this)
        //    {
        //        return;
        //    }
        //    PlayableDirector.SetPlayableAsset(asset);
        //    PlayableDirector.Play();
        //}
        //protected partial void OnPlayEnd(PlayableDirector playableDirector)
        //{
        //}
    }
}