using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ux
{
    public class ExpessionMgr : Singleton<ExpessionMgr>
    {
        #region 正则表达式
        static readonly Regex FnRegex = new Regex(@"(?<fnName>(([0-9]|[a-z]|[A-Z])*))[(](?<varName>((arg#\d)|[0-9]|[a-z]|[A-Z]|[,+\-*/%])*)[)]");
        static readonly Regex OperatorRegex = new Regex(@"^[-]?(\w)+[(+|\-|*|/|%)](\w)+");
        static readonly Regex VariableRegex = new Regex(@"^[-]?(\w+)$");
        static readonly Regex ValueRegex = new Regex(@"^[-]?((\d+\.{1}\d+)|(\d+)$)");
        static readonly Regex ArgRegex = new Regex(@"^(arg#)[\d]$");
        static readonly Regex Operator1 = new Regex(@"(?<var1>^[-]?[#\.\d\w]+)(?<tag>[*|/|%])(?<var2>[-]?[#\.\d\w]+)");
        static readonly Regex Operator2 = new Regex(@"(?<var1>^[-]?[#\.\d\w]+)(?<tag>[+|-])(?<var2>[-]?[#\.\d\w]+)");
        static readonly string ArgStr = "arg#{0}";
        #endregion

        #region 静态字典

        static IDictionary<string, double> variables = new Dictionary<string, double>() {
            { "Pi", Math.PI },
            { "pi", Math.PI },
            { "E", Math.E },
            { "e", Math.E },
        };
        static IDictionary<string, Func<double>> nameToVariable = new Dictionary<string, Func<double>>();
        static IDictionary<string, Func<double[], double>> nameToDoubleFunc = new Dictionary<string, Func<double[], double>>();
        static IDictionary<string, Func<string[], double>> nameToStringFunc = new Dictionary<string, Func<string[], double>>();
        static IDictionary<string, Func<double, double>> singleDoubleMath = new Dictionary<string, Func<double, double>>(StringComparer.Ordinal)
        {
            { "Abs", Math.Abs },
            { "abs", Math.Abs },
            { "Acos", Math.Acos },
            { "acos", Math.Acos },
            { "Asin", Math.Asin },
            { "asin", Math.Asin },
            { "Atan", Math.Atan },
            { "atan", Math.Atan },
            { "Ceiling", Math.Ceiling },
            { "ceiling", Math.Ceiling },
            { "Ceil", Math.Ceiling },
            { "ceil", Math.Ceiling },
            { "Cos", Math.Cos },
            { "cos", Math.Cos },
            { "Cosh", Math.Cosh },
            { "cosh", Math.Cosh },
            { "Exp", Math.Exp },
            { "exp", Math.Exp },
            { "Floor", Math.Floor },
            { "floor", Math.Floor },
            { "Log10", Math.Log10 },
            { "log10", Math.Log10 },
            { "Sin", Math.Sin },
            { "sin", Math.Sin },
            { "Sinh", Math.Sinh },
            { "sinh", Math.Sinh },
            { "Sqrt", Math.Sqrt },
            { "sqrt", Math.Sqrt },
            { "Tan", Math.Tan },
            { "tan", Math.Tan },
            { "Tanh", Math.Tanh },
            { "tanh", Math.Tanh },
            { "Truncate", Math.Truncate },
            { "truncate", Math.Truncate },
            { "Round", Math.Round },
            { "round", Math.Round },
        };

        static IDictionary<string, MatchData> Operator1Dict = new Dictionary<string, MatchData>();
        static IDictionary<string, MatchData> Operator2Dict = new Dictionary<string, MatchData>();
        static IDictionary<string, MatchData> FnDict = new Dictionary<string, MatchData>();
        static IDictionary<string, MatchData> VariableDict = new Dictionary<string, MatchData>();
        static IDictionary<string, MatchData> ValueDict = new Dictionary<string, MatchData>();
        static IDictionary<string, MatchData> OperatorDict = new Dictionary<string, MatchData>();
        static IDictionary<string, string[]> ArgDict = new Dictionary<string, string[]>();
        static IDictionary<string, ArgType> ArgTypeDict = new Dictionary<string, ArgType>();
        #endregion

        #region Type
        enum ArgType
        {
            Arg,
            Value,
        }

        enum OperatorType
        {
            None,
            Add,
            Subtract,
            Multiply,
            Divide,
            Remainder
        }

        struct MatchData
        {
            public string Value { get; }
            public string V1 { get; }
            public string V2 { get; }
            public string Data { get; }

            public OperatorType Type { get; }

            public MatchData(string v = null, string v1 = null, string v2 = null, string data = null)
            {
                Value = v;
                V1 = v1;
                V2 = v2;
                Data = data;
                switch (data)
                {
                    case null: Type = OperatorType.None; break;
                    case "+": Type = OperatorType.Add; break;
                    case "-": Type = OperatorType.Subtract; break;
                    case "*": Type = OperatorType.Multiply; break;
                    case "/": Type = OperatorType.Divide; break;
                    case "%": Type = OperatorType.Remainder; break;
                    default: Type = OperatorType.None; break;
                }
            }
        }
        class BaseType
        {
            public string Key { get; }
            protected MatchData match;
            protected ExpessionParse expession;
            public BaseType(string key, ExpessionParse expession, MatchData match)
            {
                this.Key = key;
                this.expession = expession;
                this.match = match;
            }
            bool IsArgs(string input)
            {
                return ArgRegex.IsMatch(input);
            }
            bool IsValue(string input)
            {
                return ValueRegex.IsMatch(input);
            }
            protected double GetValue(string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    Log.Error(zstring.Format("公式解析错误:{0}", expession.text));
                    return 0;
                }
                if (TryGetValue(input, out var value))
                {
                    return value;
                }

                zstring str;
                using (zstring.Block())
                {
                    str = input;
                }
                double v = 0;
                int r = 0;
                int cnt = 0;
                while (cnt < 1000)
                {
                    cnt++;
                    if (r <= 1 && TryParse(Operator1Dict, Operator1, ref str, ref v))
                    {
                        r = 1;
                        continue;
                    }
                    if (r <= 2 && TryParse(Operator2Dict, Operator2, ref str, ref v))
                    {
                        r = 2;
                        continue;
                    }
                    break;
                }
                if (r == 0)
                {
                    Log.Error(zstring.Format("公式解析错误:{0}", str));
                }
                return v;
            }
            bool TryGetValue(string input, out double value)
            {
                if (variables.TryGetValue(input, out value))
                {
                    return true;
                }
                if (nameToVariable.TryGetValue(input, out var func))
                {
                    value = func();
                    return true;
                }
                if (ArgTypeDict.TryGetValue(input, out var argType))
                {
                    switch (argType)
                    {
                        case ArgType.Arg:
                            value = expession.values[input];
                            break;
                        case ArgType.Value:
                            value = Convert.ToDouble(input);
                            break;
                    }
                    return true;
                }
                if (IsArgs(input))
                {
                    ArgTypeDict.Add(input, ArgType.Arg);
                    value = expession.values[input];
                    return true;
                }
                if (IsValue(input))
                {
                    if (double.TryParse(input, out value))
                    {
                        ArgTypeDict.Add(input, ArgType.Value);
                        return true;
                    }
                    Log.Error($"{input} 不是Value类型");
                }
                return false;
            }

            bool TryParse(IDictionary<string, MatchData> dict, Regex regex, ref zstring input, ref double v)
            {
                if (!dict.TryGetValue(input, out var match))
                {
                    var temMatch = regex.Match(input);
                    if (temMatch == null || !temMatch.Success)
                    {
                        match = new MatchData();
                    }
                    else
                    {
                        var v1 = temMatch.Groups["var1"].Value;
                        var v2 = temMatch.Groups["var2"].Value;
                        var tag = temMatch.Groups["tag"].Value;
                        match = new MatchData(temMatch.Value, v1, v2, tag);
                    }
                    dict.Add(input, match);
                }
                if (string.IsNullOrEmpty(match.Value))
                {
                    return false;
                }
                if (!TryGetValue(match.V1, out var arg1))
                {
                    Log.Error($"公式无法获取正确的值{match.V1}");
                    return false;
                }
                if (!TryGetValue(match.V2, out var arg2))
                {
                    Log.Error($"公式无法获取正确的值{match.V2}");
                    return false;
                }
                switch (match.Type)
                {
                    case OperatorType.Remainder:
                        v = arg1 % arg2;
                        break;
                    case OperatorType.Multiply:
                        v = arg1 * arg2;
                        break;
                    case OperatorType.Divide:
                        v = arg1 / arg2;
                        break;
                    case OperatorType.Add:
                        v = arg1 + arg2;
                        break;
                    case OperatorType.Subtract:
                        v = arg1 - arg2;
                        break;
                    default:
                        Log.Error($"未知的数学运算符:{match.Type}");
                        break;
                }
                using (zstring.Block())
                {
                    input = input.Replace(match.Value, (zstring)v);
                }
                return true;
            }

            public virtual double Value => 0;
        }
        class FunctionType : BaseType
        {
            public FunctionType(string key, ExpessionParse expession, MatchData match) : base(key, expession, match)
            {
            }
            public override double Value
            {
                get
                {
                    if (singleDoubleMath.TryGetValue(match.V2, out var singleFunc))
                    {
                        return singleFunc(GetValue(match.V1));
                    }
                    string[] Func(string content)
                    {
                        if (string.IsNullOrEmpty(content) || content.Length == 0)
                        {
                            return null;
                        }
                        if (!ArgDict.TryGetValue(content, out var vars))
                        {
                            vars = content.Split(',');
                            ArgDict.Add(content, vars);
                        }
                        return vars;
                    }
                    if (nameToDoubleFunc.TryGetValue(match.V2, out var func1))
                    {
                        var vars = Func(match.V1);
                        if (vars == null)
                        {
                            return func1.Invoke(null);
                        }
                        var objs = new double[vars.Length];
                        for (int i = 0; i < vars.Length; i++)
                        {
                            objs[i] = GetValue(vars[i]);
                        }
                        return func1.Invoke(objs);
                    }
                    if (nameToStringFunc.TryGetValue(match.V2, out var func2))
                    {
                        return func2.Invoke(Func(match.V1));
                    }
                    Log.Error($"{match.V2}没有注册解析函数");
                    return 0;
                }
            }
        }
        class VariableType : BaseType
        {
            public VariableType(string key, ExpessionParse expession, MatchData match) : base(key, expession, match)
            {
            }
            public override double Value => GetValue(match.V1);
        }
        class ExpessionParse
        {
            public Dictionary<string, double> values = new Dictionary<string, double>();
            public string text;

            List<BaseType> _queue = new List<BaseType>();
            string _argValue;

            public ExpessionParse(string _text)
            {
                text = _text;
                zstring input;
                using (zstring.Block())
                {
                    input = _text;
                    if (_Parse(ValueDict, ValueRegex, 1, ref input))
                    {
                        return;
                    }
                    if (_Parse(VariableDict, VariableRegex, 1, ref input))
                    {
                        return;
                    }
                    _ParseFn(0, ref input);
                    _argValue = input.ToString();
                }
            }
            bool _Parse(IDictionary<string, MatchData> dict, Regex regex, int argIndex, ref zstring input)
            {
                if (!dict.TryGetValue(input, out var match))
                {
                    var temMatch = regex.Match(input);
                    if (temMatch == null || !temMatch.Success)
                    {
                        match = new MatchData();
                    }
                    else
                    {
                        match = new MatchData(temMatch.Value, temMatch.Value);
                    }
                    dict.Add(input, match);
                }

                if (string.IsNullOrEmpty(match.Value))
                {
                    return false;
                }

                var argKey = zstring.Format(ArgStr, argIndex);
                _queue.Add(new VariableType(zstring.Format(ArgStr, argIndex), this, match));
                input = input.Replace(match.Value, argKey);

                return true;
            }

            void _ParseFn(int argIndex, ref zstring input)
            {
                if (!FnDict.TryGetValue(input, out var match))
                {
                    var temMatch = FnRegex.Match(input);
                    if (temMatch == null || !temMatch.Success)
                    {
                        match = new MatchData();
                    }
                    else
                    {
                        var fnName = temMatch.Groups["fnName"].Value;
                        var varName = temMatch.Groups["varName"].Value;
                        var value = temMatch.Value;
                        match = new MatchData(temMatch.Value, varName, fnName);
                    }
                    FnDict.Add(input, match);
                }
                argIndex++;
                if (string.IsNullOrEmpty(match.Value))
                {
                    _Parse(OperatorDict, OperatorRegex, argIndex, ref input);
                    return;
                }

                var argKey = zstring.Format(ArgStr, argIndex);
                if (string.IsNullOrEmpty(match.V2))
                {
                    _queue.Add(new VariableType(argKey, this, match));
                }
                else
                {
                    _queue.Add(new FunctionType(argKey, this, match));
                }

                input = input.Replace(match.Value, argKey);

                _ParseFn(argIndex, ref input);
            }

            public double Value
            {
                get
                {
                    values.Clear();
                    for (int i = 0; i < _queue.Count; i++)
                    {
                        var q = _queue[i];
                        if (i == _queue.Count - 1)
                        {
                            return q.Value;
                        }
                        values[q.Key] = q.Value;
                    }
                    return 0;
                }
            }
            public zstring Desc
            {
                get
                {
                    using (zstring.Block())
                    {
                        var str = (zstring)_argValue;
                        str = str.Replace(_queue[_queue.Count - 1].Key, Value.ToString());
                        return str;
                    }
                }
            }
        }
        #endregion

        #region 常量
        /// <summary>
        /// 设置常量
        /// </summary>
        public void AddVariable(string name, double value)
        {
            nameToVariable.Remove(name);
            variables[name] = value;
        }
        /// <summary>
        /// 设置常量
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func">获取常量函数</param>
        public void AddVariable(string name, Func<double> func)
        {
            variables.Remove(name);
            nameToVariable[name] = func;
        }

        public void RemoveVariables(string name)
        {
            variables.Remove(name);
            nameToVariable.Remove(name);
        }
        #endregion

        #region 方法

        /// <summary>
        /// 设置函数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func">获取函数</param>
        public void AddFunction(string name, Func<double[], double> func)
        {
            nameToStringFunc.Remove(name);
            nameToDoubleFunc[name] = func;
        }
        /// <summary>
        /// 设置函数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func">获取函数</param>
        public void AddFunction(string name, Func<string[], double> func)
        {
            nameToDoubleFunc.Remove(name);
            nameToStringFunc[name] = func;
        }
        public void RemoveFunction(string name)
        {
            nameToDoubleFunc.Remove(name);
            nameToStringFunc.Remove(name);
        }
        #endregion

        Dictionary<string, ExpessionParse> inputToExpssion = new Dictionary<string, ExpessionParse>();
        protected override void OnInit()
        {
            InitGlobalFunc();
        }

        #region 全局方法
        void InitGlobalFunc()
        {
            AddFunction("Max", Max);
            AddFunction("max", Max);
            AddFunction("Min", Min);
            AddFunction("min", Min);
            AddFunction("Pow", Pow);
            AddFunction("pow", Pow);
        }
        double Max(double[] objs)
        {
            if (objs.Length != 2)
            {
                Log.Error("Max 参数错误");
                return 0;
            }
            return Math.Max(objs[0], objs[1]);
        }
        double Min(double[] objs)
        {
            if (objs.Length != 2)
            {
                Log.Error("Min 参数错误");
                return 0;
            }
            return Math.Min(objs[0], objs[1]);
        }
        double Pow(double[] objs)
        {
            if (objs.Length != 2)
            {
                Log.Error("Pow 参数错误");
                return 0;
            }
            return Math.Pow(objs[0], objs[1]);
        }
        #endregion

        #region 解析
        public double Parse(string input)
        {
            if (!inputToExpssion.TryGetValue(input, out var e))
            {
                e = new ExpessionParse(input);
                inputToExpssion.Add(input, e);
            }
            return e.Value;
        }
        public string Desc(string input)
        {
            if (!inputToExpssion.TryGetValue(input, out var e))
            {
                e = new ExpessionParse(input);
                inputToExpssion.Add(input, e);
            }
            return e.Desc;
        }
        #endregion
    }
}


