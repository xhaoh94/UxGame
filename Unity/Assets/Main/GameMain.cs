using System;
using UnityEngine;
using YooAsset;

namespace Ux
{
    public class GameMain : MonoBehaviour
    {
        public static GameMain Ins { get; private set; }

        public static StateMachine Machine { get; private set; }

        [SerializeField]
        public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

        [SerializeField]
        bool IngameDebug = true;

        void Awake()
        {
            if (IngameDebug)
            {
                Instantiate(Resources.Load<GameObject>("IngameDebugConsloe/IngameDebugConsole"));
            }
            Ins = this;
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


        void Start()
        {
#if !UNITY_EDITOR
            if (PlayMode == EPlayMode.EditorSimulateMode)
                PlayMode = EPlayMode.HostPlayMode;
#endif
            Log.Debug($"资源系统运行模式：{PlayMode}");

            // 运行补丁流程
            PatchMgr.Ins.Run();
        }

        public void AddUpdate(Action action)
        {
            _update += action;
        }
        public void RemoveUpdate(Action action)
        {
            _update -= action;
        }
        Action _update;
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
                _update?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public void AddLateUpdate(Action action)
        {
            _lateUpdate += action;
        }
        public void RemoveLateUpdate(Action action)
        {
            _lateUpdate -= action;
        }
        Action _lateUpdate;
        void LateUpdate()
        {
            try
            {
                _lateUpdate?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        public void AddFixedUpdate(Action action)
        {
            _fixedUpdate += action;
        }
        public void RemoveFixedUpdate(Action action)
        {
            _fixedUpdate -= action;
        }
        Action _fixedUpdate;
        private void FixedUpdate()
        {
            try
            {
                _fixedUpdate?.Invoke();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
        public void AddQuit(Action action)
        {
            _quit += action;
        }
        public void RemoveQuit(Action action)
        {
            _quit -= action;
        }
        Action _quit;
        private void OnApplicationQuit()
        {
            _quit?.Invoke();
        }

        public void AddLowMemory(Action action)
        {
            _lowMemory += action;
        }
        public void RemoveLowMemory(Action action)
        {
            _lowMemory -= action;
        }
        Action _lowMemory;
        void OnLowMemory()
        {
            YooMgr.Ins.OnLowMemory();
            Pool.Clear();
            _lowMemory?.Invoke();
        }
    }
}