using System;
using UnityEngine;
using YooAsset;

namespace Ux
{
    public class GameStateMachine: StateMachine
    {
        public IStateNode StateNode { get; private set; }
        protected override void OnEnter(IStateNode node)
        {
            if (StateNode != null)
            {
                Exit(StateNode);
            }
            StateNode = node;            
        }
    }

    public class GameMain : MonoBehaviour
    {
        public static GameMain Ins { get; private set; }

        public static GameStateMachine Machine { get; private set; }

        [SerializeField]
        public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

        [SerializeField]
        bool IngameDebug = true;

        [SerializeField]        
        public bool HotfixCode = false;

        void Awake()
        {
            Ins = this;
            if (IngameDebug)
            {
                Instantiate(Resources.Load<GameObject>("IngameDebugConsloe/IngameDebugConsole"));
            }
            zstring.Init(Log.Error);
            Machine = StateMachine.Create<GameStateMachine>();
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Application.lowMemory += OnLowMemory;
        }

        private void OnDestroy()
        {
            Application.lowMemory -= OnLowMemory;
        }


        void Start()
        {
#if !UNITY_EDITOR
            if (PlayMode == EPlayMode.EditorSimulateMode) PlayMode = EPlayMode.HostPlayMode;
#endif

#if !UNITY_EDITOR && HOTFIX_CODE
            if (!HotfixCode) HotfixCode = true;
#endif
            Log.Debug($"资源系统运行模式：{PlayMode}");
            Log.Debug($"是否启用热更代码：{HotfixCode}");
            // 运行补丁流程
            PatchMgr.Ins.Run();
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
                GameMethod.Update?.Invoke();                
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
                GameMethod.LateUpdate?.Invoke();
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
                GameMethod.FixedUpdate?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void OnApplicationQuit()
        {
            GameMethod.Quit?.Invoke();
        }

        void OnLowMemory()
        {            
            Pool.Clear();
            GameMethod.LowMemory?.Invoke();
        }
    }
}