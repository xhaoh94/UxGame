using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Ux
{
    public partial class EvalMgr : Singleton<EvalMgr>
    {
        OverdueMap<string, FormulaParser> _cache;
        const float _timeout = 60f;
        protected override void OnCreated()
        {
            GameMain.Ins.LowMemory += Release;
        }
        public void Release()
        {
            _cache?.Clear();
        }
        void _CheckTimeout(FormulaParser formulaParser)
        {
            formulaParser.Release();
        }
        public double Parse(string expression)
        {
            expression = Regex.Replace(expression, @"\s+", "").ToLowerInvariant();
            _cache ??= new OverdueMap<string, FormulaParser>(_timeout, _CheckTimeout);
            if (!_cache.TryGetValue(expression, out var e))
            {
                e = Pool.Get<FormulaParser>().Init(expression);
                _cache.Add(expression, e);
            }
            return e.Evaluate();
        }
    }
}


