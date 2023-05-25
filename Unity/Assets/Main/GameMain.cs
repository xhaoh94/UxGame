#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using YooAsset;
using UnityEngine.Profiling;

namespace Ux
{
    public class GameMain : MonoBehaviour
    {
        public static GameMain Ins { get; private set; }
        public static StateMachine Machine { get; private set; }

        public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

#if UNITY_EDITOR
        [LabelText("开启游戏内Debug")]
#endif

        [SerializeField]
        private bool IngameDebug = true;

        void Awake()
        {
            if (IngameDebug)
            {
                Instantiate(Resources.Load<GameObject>("IngameDebugConsloe/IngameDebugConsole"));
            }            
            Ins = this;
            zstring.Init(Log.Error);
            Machine = StateMachine.CreateByPool();
            SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Application.lowMemory += OnLowMemory;
        }

        private void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
        }


        IEnumerator Start()
        //void Start()
        {
#if !UNITY_EDITOR
            if (PlayMode == EPlayMode.EditorSimulateMode)
                PlayMode = EPlayMode.HostPlayMode;
#endif
            Log.Debug($"资源系统运行模式：{PlayMode}");

            yield return ResMgr.Ins.Initialize();

            typeof(GameMain).Assembly.Initialize();
            // 运行补丁流程
            PatchMgr.Ins.Run(PlayMode);           
        }

        
        void Update()
        {            
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isCompiling)
            {
                if (UnityEditor.EditorApplication.isPlaying)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                }
            }
#endif
            try
            {
                OneThreadSynchronizationContext.Instance.Update();
                TimeMgr.Ins.Update();
                EventMgr.Ins.Update();
                NetMgr.Ins.Update();
                Entity.Update();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }


        void LateUpdate()
        {
            try
            {
                TimeMgr.Ins.LateUpdate();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void FixedUpdate()
        {
            try
            {
                TimeMgr.Ins.FixedUpdate();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void OnApplicationQuit()
        {
            EventMgr.Ins.OnApplicationQuit();
        }


        void OnLowMemory()
        {
            UIMgr.Ins.OnLowMemory();
            ResMgr.Ins.OnLowMemory();
            Pool.Clear();
        }
    }
}