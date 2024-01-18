using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ux
{
    public partial class EvalMgr : Singleton<EvalMgr>
    {
        static readonly Regex FnRegex = new Regex(@"(?<fnName>(([\w\d])*))[(](?<varName>((arg#(\d+))|[\d\w\.,+\-*/%])*)[)]", RegexOptions.Compiled);
        static readonly Regex ArgRegex = new Regex(@"(^-?)arg#(\d+)$", RegexOptions.Compiled);
        static readonly Regex VariableRegex = new Regex(@"((arg#(\d+))|[\d\w\.,+\-*/%])+", RegexOptions.Compiled);
        static readonly Regex Operator1 = new Regex(@"(?<var1>((^[-]|((?<=[-|+])[-])?)(((arg#(\d+))|[\d\w\.])+)))(?<tag>[*|/|%])(?<var2>([-]?(((arg#(\d+))|[\d\w\.])+)))", RegexOptions.Compiled);
        static readonly Regex Operator2 = new Regex(@"(?<var1>(^[-]?(((arg#(\d+))|[\d\w\.])+)))(?<tag>[+|-])(?<var2>([-]?(((arg#(\d+))|[\d\w\.])+)))", RegexOptions.Compiled);
        static readonly string ArgStr = "arg#{0}";

        Dictionary<string, EvalParse> inputToExpssion = new Dictionary<string, EvalParse>();
        protected override void OnCreated()
        {
            InitGlobalFunc();
        }


        public double Parse(string input)
        {
            if (!inputToExpssion.TryGetValue(input, out var e))
            {
                e = new EvalParse(input);
                inputToExpssion.Add(input, e);
            }
            return e.Value;
        }
        public string Desc(string input)
        {
            if (!inputToExpssion.TryGetValue(input, out var e))
            {
                e = new EvalParse(input);
                inputToExpssion.Add(input, e);
            }
            return e.Desc;
        }
    }
}


