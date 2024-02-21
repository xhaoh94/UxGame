using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class AnimNode
{
    AnimationMixerPlayable m_mixerPlayable;
    int inputport;
    float _fadeWeight = 0f;
    bool _isFading = false;
    private float _fadeSpeed = 0f;

    public void Init(AnimationMixerPlayable mixer, int inputPort)
    {
        m_mixerPlayable = mixer;
        inputport = inputPort;
    }
    public void StartWeightFade(float destWeight, float fadeDuration)
    {
        if (fadeDuration <= 0)
        {
            Weight = destWeight;
            _isFading = false;
            return;
        }

        //注意：保持统一的渐变速度
        _fadeSpeed = 1f / fadeDuration;
        _fadeWeight = destWeight;
        _isFading = true;
    }
    public void Update(float deltaTime)
    {
        if (_isFading)
        {
            Weight = Mathf.MoveTowards(Weight, _fadeWeight, _fadeSpeed * deltaTime);
            if (Mathf.Approximately(Weight, _fadeWeight))
            {
                _isFading = false;
            }
        }
    }

    public float Weight
    {
        set
        {
            m_mixerPlayable.SetInputWeight(inputport, value);
        }
        get
        {
            return m_mixerPlayable.GetInputWeight(inputport);
        }
    }
}
public class AnimPB : PlayableBehaviour
{
    AnimationMixerPlayable m_mixerPlayable;    
    PlayableGraph m_playableGraph;    

    static AnimationClip lastClip = null;

    AnimNode last;
    AnimNode now;
    public void Init(AnimationClip clip1)
    {
        var clip1Playable = AnimationClipPlayable.Create(m_playableGraph, clip1);        
        m_mixerPlayable.ConnectInput(0, clip1Playable, 0);        
        clip1Playable.SetSpeed(clip1.length);        

        if (lastClip != null)
        {
            var clip2Playable = AnimationClipPlayable.Create(m_playableGraph, lastClip);
            clip2Playable.Pause();
            clip2Playable.SetDone(true);
            m_mixerPlayable.ConnectInput(1, clip2Playable, 0);            
            clip2Playable.SetSpeed(lastClip.length);
            last = new AnimNode();
            last.Init(m_mixerPlayable, 1);            
        }

        now = new AnimNode();
        now.Init(m_mixerPlayable, 0);
        lastClip = clip1;
    }

    public override void OnPlayableCreate(Playable playable)
    {
        base.OnPlayableCreate(playable);
        m_playableGraph = playable.GetGraph();
        m_mixerPlayable = AnimationMixerPlayable.Create(m_playableGraph, 2);
        playable.ConnectInput(0, m_mixerPlayable, 0);
    }
    
    public override void OnGraphStart(Playable playable)
    {
        base.OnGraphStart(playable);
    }

    
    public override void OnGraphStop(Playable playable)
    {           
    }

    
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        //last?.StartWeightFade(1f, 0);
        //now?.StartWeightFade(0f, 0);
        last?.StartWeightFade(0, 0.3f);
        now?.StartWeightFade(1, 0.3f);
    }

   

    // Called each frame while the state is set to Play
    public override void PrepareFrame(Playable playable, FrameData info)
    {
        last?.Update(info.deltaTime);
        now?.Update(info.deltaTime);
    }

}
