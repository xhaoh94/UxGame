using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using YooAsset;

namespace Ux
{
    public class GameMain : MonoBehaviour
    {
        public static GameMain Ins { get; private set; }
        public static StateMachine Machine { get; private set; }

        [SerializeField]
        private EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

        [SerializeField]
        private bool IngameDebug = true;

        void Awake()
        {
            int a1 = -604752908;
            int b1 = 1818870784;
            int c1 = -1098022624;
            long key1 = IDGenerater.GenerateId(c1, b1, a1);

            int a2 = -492354658;
            int b2 = 1818870784;
            int c2 = -1098022624;
            long key2 = IDGenerater.GenerateId(c2, b2, a2);

            Log.Debug("key1:" + key1 + "key2:" + key2);
            if (IngameDebug)
            {
                Instantiate(Resources.Load<GameObject>("IngameDebugConsloe/IngameDebugConsole"));
            }
            Ins = this;
            SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);
            zstring.Init(Log.Error);
            Machine = StateMachine.CreateByPool();
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

            yield return ResMgr.Ins.Initialize(PlayMode);
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