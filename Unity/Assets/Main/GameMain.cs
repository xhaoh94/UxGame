#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif
using Analysis;
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
            using (zstring.Block()) { }
            Ins = this;
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


        //IEnumerator Start()
        void Start()
        {
#if !UNITY_EDITOR
            if (PlayMode == EPlayMode.EditorSimulateMode)
                PlayMode = EPlayMode.HostPlayMode;
#endif
            //Log.Debug($"资源系统运行模式：{PlayMode}");

            //yield return ResMgr.Instance.Initialize();

            //typeof(GameMain).Assembly.Initialize();
            //// 运行补丁流程
            //PatchMgr.Instance.Run(PlayMode);
            ExpessionMgr.Ins.AddVariable("test", 11);
            ExpessionMgr.Ins.AddFunction("getRemainsStarParameter", (object[] args) =>
            {
                return Convert.ToDouble(args[0]) + Convert.ToDouble(args[1]);
            });
            ExpessionMgr.Ins.AddFunction("getRemainsAwakenParameter", (object[] args) =>
            {
                return 1;
            });
            ExpessionMgr.Ins.AddFunction("getDisplaysParameter", (object[] args) =>
            {
                return 1;
            });
            test = new AnalysisMain();
            test.Init();
            Test();
        }

        void Test()
        {
            string eval = $"floor((11+test+getRemainsAwakenParameter())*getRemainsStarParameter(10,2+test)+getDisplaysParameter())";
            Profiler.BeginSample("Eval 11");
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var tt = ExpessionMgr.Ins.Parse(eval);
            sw.Stop();
            Profiler.EndSample();
            Log.Debug("11 Eval Parse Time " + sw.ElapsedMilliseconds);
            Log.Debug("11 Eval Parse Time " + tt.ToString());

            test.TT(eval, 1);
        }

        AnalysisMain test;
        void Update()
        {
            Test();
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