#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using YooAsset;

namespace Ux
{
    public class GameMain : MonoBehaviour
    {
        public static GameMain Instance { get; private set; }
        public static StateMachine Machine { get; private set; }

        public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

#if UNITY_EDITOR
        [LabelText("开启游戏内Debug")]
#endif

        [SerializeField]
        private bool IngameDebug = true;

        void Awake()
        {
            using (zstring.Block())
            {
            }

            if (IngameDebug)
            {
                Instantiate(Resources.Load<GameObject>("IngameDebugConsloe/IngameDebugConsole"));
            }

            Instance = this;
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
        {
#if !UNITY_EDITOR
            if (PlayMode == EPlayMode.EditorSimulateMode)
                PlayMode = EPlayMode.HostPlayMode;
#endif
            Log.Debug($"资源系统运行模式：{PlayMode}");

            yield return ResMgr.Instance.Initialize();

            typeof(GameMain).Assembly.Initialize();
            // 运行补丁流程
            PatchMgr.Instance.Run(PlayMode);
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
                TimeMgr.Instance.Update();
                EventMgr.Instance.Update();
                NetMgr.Instance.Update();
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
                TimeMgr.Instance.LateUpdate();
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
                TimeMgr.Instance.FixedUpdate();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        
        private void OnApplicationQuit()
        {            
            EventMgr.Instance.OnApplicationQuit();
        }


        void OnLowMemory()
        {
            UIMgr.Instance.OnLowMemory();
            ResMgr.Instance.OnLowMemory();
            Pool.Clear();
        }
    }
}