using Cysharp.Threading.Tasks;
using System;

namespace Ux
{
    public interface IModule
    {
        void Init();
        void Release();
    }

    public class ModuleBase<T> : IModule where T : class, new()
    {
        private static T _ins;
        public static T Ins
        {
            get
            {
                if (_ins == null)
                {
                    throw new Exception($"模块[{typeof(T).Name}]没有被创建，请检查ModuleParse.Create是否有创建");
                }
                return _ins;
            }
        }
        public ModuleBase()
        {
            _ins = this as T;
        }
        public void Init()
        {
            EventMgr.Ins.___RegisterFastMethod(this);
            OnInit();
        }
        protected virtual void OnInit() { }
        public void Release()
        {
            OnRelease();
            EventMgr.Ins.OffTag(this);
            TimeMgr.Ins.RemoveTag(this);
            _ins = null;
        }
        protected virtual void OnRelease() { }

    }
}