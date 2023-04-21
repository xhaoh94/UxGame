using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ux
{
    public class ModuleMgr : Singleton<ModuleMgr>
    {
        public readonly struct ModuleParse
        {
            public ModuleParse(Type type, int priority)
            {
                _type = type;
                this.priority = priority;
            }

            private readonly Type _type;

            public int priority { get; }

            public IModule Create()
            {
                int cnt = 0;
                var type = _type;
                while (true)
                {
                    if (type == null) break;
                    var _instance = type.GetField("_instance",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (_instance == null)
                    {
                        type = type.BaseType;
                        if (cnt > 10) break;
                        cnt++;
                    }
                    else
                    {
                        var mol = (IModule)Activator.CreateInstance(_type);
                        _instance.SetValue(null, mol);
                        return mol;
                    }
                }
                return null;
            }
        }

        readonly List<ModuleParse> _parses = new List<ModuleParse>();
        readonly List<IModule> _modules = new List<IModule>();
        public void Add(List<ModuleParse> parses)
        {
            this._parses.AddRange(parses);
        }
        public void Create()
        {
            int index = _modules.Count;
            _parses.Sort((a, b) => b.priority - a.priority);
            _parses.ForEach(mol =>
            {
                var module = mol.Create();
                if (module != null)
                {
                    _modules.Add(module);
                }
            });
            for (int i = index; i < _modules.Count; i++)
            {
                var module = _modules[i];
                module.Init();
            }
        }
        public void Release()
        {
            if (_modules.Count <= 0) return;
            foreach (var module in _modules)
            {
                module.Release();
            }
            _modules.Clear();
        }
        public static void ForEach(Action<IModule> fn)
        {
            Instance._ForEach(fn);
        }
        void _ForEach(Action<IModule> fn)
        {
            _modules.ForEach(fn);
        }
    }
}