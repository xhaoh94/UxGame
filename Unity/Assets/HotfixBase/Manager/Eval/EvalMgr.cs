using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ux
{
    public partial class EvalMgr : Singleton<EvalMgr>
    {
        static readonly Regex FnRegex = new Regex(
            @"(?<!\d)(?<fnName>[a-zA-Z_]\w*)\((?<varName>(?:(?:arg#\d+)|[\w.,+*/%-])*)\)",
            RegexOptions.Compiled);
        static readonly Regex ArgRegex = new Regex(
            @"^-?arg#\d+$", 
            RegexOptions.Compiled);
        static readonly Regex VariableRegex = new Regex(
            @"[\w#.*+%/\-,-]+",
            RegexOptions.Compiled);
        static readonly Regex Operator1 = new Regex(
            @"(?<var1>-?(?:(?:arg#\d+)|[\w.])+)(?<tag>[*/%])(?<var2>-?(?:(?:arg#\d+)|[\w.])+)", 
            RegexOptions.Compiled);
        static readonly Regex Operator2 = new Regex(
            @"(?<var1>^-?(?:(?:arg#\d+)|[\w.])+)(?<tag>[+-])(?<var2>-?(?:(?:arg#\d+)|[\w.])+)",
            RegexOptions.Compiled);
        static readonly string ArgStr = "arg#{0}";

        OverdueMap<string,EvalParser> _inputToExpssion;
        long _timeoutKey;
        protected override void OnCreated()
        {
            InitGlobalFunc();
            GameMethod.LowMemory += Release;
        }
        public void Release()
        {
            _inputToExpssion.Clear();
            Operator1MdDict.Clear();
            Operator2MdDict.Clear();
            FnMdDict.Clear();
            VariableMdDict.Clear();
            ArgSplitDict.Clear();
            ArgDict.Clear();
            ValueDict.Clear();
        }
        void _ReleaseParset(EvalParser evalParser)
        {
            //回收对象池
            evalParser.Release();
        }


        public double Parse(string input)
        {            
            _inputToExpssion ??= new OverdueMap<string, EvalParser>(_timeout, _ReleaseParset);
            if (!_inputToExpssion.TryGetValue(input, out var e))
            {
                e = Pool.Get<EvalParser>();
                e.Init(input);
                _inputToExpssion.Add(input, e);
            }
            return e.Value;
        }
        public string Desc(string input)
        {
            _inputToExpssion ??= new OverdueMap<string, EvalParser>(_timeout, _ReleaseParset);
            if (!_inputToExpssion.TryGetValue(input, out var e))
            {
                e = Pool.Get<EvalParser>();
                e.Init(input);
                _inputToExpssion.Add(input, e);
            }
            return e.Desc;
        }


    }
}


