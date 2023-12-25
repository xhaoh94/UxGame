using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ux
{
    public partial class EvalMgr : Singleton<EvalMgr>
    {        
        static readonly Regex FnRegex = new Regex(@"(?<fnName>(([0-9]|[a-z]|[A-Z])*))[(](?<varName>((arg#\d)|[0-9]|[a-z]|[A-Z]|[,+\-*/%])*)[)]");
        static readonly Regex OperatorRegex = new Regex(@"^[-]?(\w)+[(+|\-|*|/|%)](\w)+");
        static readonly Regex VariableRegex = new Regex(@"^[-]?(\w+)$");
        static readonly Regex ValueRegex = new Regex(@"^[-]?((\d+\.{1}\d+)|(\d+)$)");
        static readonly Regex ArgRegex = new Regex(@"^(arg#)[\d]$");
        static readonly Regex Operator1 = new Regex(@"(?<var1>^[-]?[#\.\d\w]+)(?<tag>[*|/|%])(?<var2>[-]?[#\.\d\w]+)");
        static readonly Regex Operator2 = new Regex(@"(?<var1>^[-]?[#\.\d\w]+)(?<tag>[+|-])(?<var2>[-]?[#\.\d\w]+)");
        static readonly string ArgStr = "arg#{0}";        
      
        Dictionary<string, EvalParse> inputToExpssion = new Dictionary<string, EvalParse>();
        protected override void OnInit()
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


