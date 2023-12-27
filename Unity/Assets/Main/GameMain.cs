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
            EvalMgr.Ins.AddVariable("Temp", 3);
            EvalMgr.Ins.AddFunction("TempFunc", (args) => args[0] + args[1]);
            var str = "TempFunc(-2+2100-(22*2*(1-2))%Temp+TempFunc(1,Temp),-4)";
            var v = EvalMgr.Ins.Parse(str);
            Log.Debug($"{str}:" + v);

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