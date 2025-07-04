using SJ;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ux
{
    public partial class EvalMgr
    {
        const float _timeout = 60f;

        static OverdueMap<string, MatchData> Operator1MdDict = new OverdueMap<string, MatchData>(_timeout);
        static OverdueMap<string, MatchData> Operator2MdDict = new OverdueMap<string, MatchData>(_timeout);
        static OverdueMap<string, MatchData> FnMdDict = new OverdueMap<string, MatchData>(_timeout);
        static OverdueMap<string, MatchData> VariableMdDict = new OverdueMap<string, MatchData>(_timeout);
        static OverdueMap<string, string[]> ArgSplitDict = new OverdueMap<string, string[]>(_timeout);
        static OverdueMap<string, ArgBool> ArgDict = new OverdueMap<string, ArgBool>(_timeout);
        static OverdueMap<string, ValueBool> ValueDict = new OverdueMap<string, ValueBool>(_timeout);

        static IDictionary<string, Func<double, double, double>> SymbolFnDict = new Dictionary<string, Func<double, double, double>>()
        {
            {"+",(a,b)=>a+b },
            {"-",(a,b)=>a-b },
            {"*",(a,b)=>a*b },
            {"/",(a,b)=>a/b },
            {"%",(a,b)=>a%b },
        };
        struct ArgBool
        {
            public bool IsArg { get; }
            public string ArgStr { get; }
            public ArgBool(string input)
            {
                IsArg = ArgRegex.IsMatch(input);
                ArgStr = IsArg ? input : null;    
            }
        }
        struct ValueBool
        {
            public bool IsValue { get; }
            public double Value { get; }
            public ValueBool(string input)
            {
                if (double.TryParse(input, out var _v))
                {
                    Value = _v;
                    IsValue = true;
                }
                else
                {
                    Value = 0;
                    IsValue = false;
                }
            }

        }
        struct MatchData
        {
            public string Input { get; }
            public string V1 { get; }
            public string V2 { get; }
            public string V3 { get; }

            public MatchData(string input = null, string v1 = null, string v2 = null, string v3 = null)
            {
                Input = input;
                V1 = v1;
                V2 = v2;
                V3 = v3;
            }
        }
        class VariableParser
        {
            public string Key { get; private set; }
            protected MatchData mMatch;
            protected EvalParser mEp;
            public void Init(string key, EvalParser ep, MatchData match)
            {
                Key = key;
                mEp = ep;
                mMatch = match;
            }
            public virtual void Release()
            {
                Key = null;
                mEp = null;
                mMatch = default;
                Pool.Push(this);
            }
            bool IsArgs(string input, ref double value)
            {
                if (!ArgDict.TryGetValue(input, out var ab))
                {
                    ab = new ArgBool(input);
                    ArgDict.Add(input, ab);
                }
                if (ab.IsArg)
                {
                    value = mEp.TryGetValue(ab.ArgStr);     
                    return true;
                }
                return false;
            }
            bool IsValue(string input, ref double value)
            {
                if (!ValueDict.TryGetValue(input, out var vb))
                {
                    vb = new ValueBool(input);
                    ValueDict.Add(input, vb);
                }
                if (vb.IsValue)
                {
                    value = vb.Value;
                    return true;
                }
                return false;
            }
            protected double GetValue(string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    Log.Error("公式解析错误:Input IsNullOrEmpty");
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
                    if (r <= 1 && TryParse(Operator1MdDict, Operator1, ref str, ref v))
                    {
                        r = 1;
                        continue;
                    }
                    if (r <= 2 && TryParse(Operator2MdDict, Operator2, ref str, ref v))
                    {
                        r = 2;
                        continue;
                    }
                    break;
                }
                if (r == 0)
                {
                    using (zstring.Block())
                    {
                        Log.Error(zstring.Format("公式解析错误:{0}", str));
                    }
                }
                return v;
            }
            bool TryGetValue(string input, out double value)
            {
                bool IsSymbol = false;
                if (input.StartsWith('-'))
                {
                    input = input.Substring(1);
                    IsSymbol = true;
                }
                bool result = false;
                if (variables.TryGetValue(input, out value))
                {
                    result = true;
                }
                if (!result && nameToVariable.TryGetValue(input, out var func))
                {
                    value = func();
                    result = true;
                }

                if (!result && IsArgs(input, ref value))
                {
                    result = true;
                }

                if (!result && IsValue(input, ref value))
                {
                    result = true;
                }
                if (IsSymbol)
                {
                    value *= -1;
                }
                return result;
            }

            bool TryParse(OverdueMap<string, MatchData> map, Regex regex, ref zstring input, ref double v)
            {
                if (!map.TryGetValue(input, out var match))
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
                    map.Add(input, match);
                }
                if (string.IsNullOrEmpty(match.Input))
                {
                    return false;
                }
                if (string.IsNullOrEmpty(match.V3))
                {
                    Log.Error($"公式运算符为Null:{input}");
                    return false;
                }
                if (!TryGetValue(match.V1, out var arg1))
                {
                    Log.Error($"公式无法获取值:{match.V1}");
                    return false;
                }
                if (!TryGetValue(match.V2, out var arg2))
                {
                    Log.Error($"公式无法获取值:{match.V2}");
                    return false;
                }

                if (!SymbolFnDict.TryGetValue(match.V3, out var fn))
                {
                    Log.Error($"未注册的数学运算符:{match.V3}");
                    return false;
                }
                v = fn(arg1, arg2);
                //Log.Debug($"{arg1}{match.V3}{arg2}={v}");
                using (zstring.Block())
                {
                    input = input.Replace(match.Input, v.ToString());
                }
                return true;
            }

            public virtual double Value => GetValue(mMatch.V1);
        }
        class FunctionParser : VariableParser
        {
            OverdueMap<int, double[]> doubleMap = new OverdueMap<int, double[]>(_timeout);
            public override void Release()
            {
                doubleMap.Clear();
                base.Release();
            }
            string[] SplitFunc(string content)
            {
                if (string.IsNullOrEmpty(content) || content.Length == 0)
                {
                    return null;
                }
                if (!ArgSplitDict.TryGetValue(content, out var vars))
                {
                    vars = content.Split(',');
                    ArgSplitDict.Add(content, vars);
                }
                return vars;
            }
            public override double Value
            {
                get
                {
                    if (singleDoubleMath.TryGetValue(mMatch.V2, out var singleFunc))
                    {
                        return singleFunc(GetValue(mMatch.V1));
                    }

                    if (nameToDoubleFunc.TryGetValue(mMatch.V2, out var func1))
                    {
                        var vars = SplitFunc(mMatch.V1);
                        if (vars == null)
                        {
                            return func1.Invoke(null);
                        }
                        if (!doubleMap.TryGetValue(vars.Length, out var objs))
                        {
                            objs = new double[vars.Length];
                            doubleMap.Add(vars.Length, objs);
                        }
                        for (int i = 0; i < vars.Length; i++)
                        {
                            objs[i] = GetValue(vars[i]);
                        }
                        return func1.Invoke(objs);
                    }
                    if (nameToStringFunc.TryGetValue(mMatch.V2, out var func2))
                    {
                        return func2.Invoke(SplitFunc(mMatch.V1));
                    }
                    Log.Error($"{mMatch.V2}没有注册解析函数");
                    return 0;
                }
            }
        }
        class EvalParser
        {
            //存储已经解析出来值
            Dictionary<string, double> _values = new Dictionary<string, double>();
            public string InputText { get; private set; }

            List<VariableParser> _queue = new List<VariableParser>();
            string _argValue;

            public void Init(string _text)
            {
                InputText = _text;
                zstring input;
                using (zstring.Block())
                {
                    input = _text;
                    _ParseFn(0, ref input);
                    _argValue = input.ToString();
                }
            }
            public void Release()
            {
                _argValue = null;
                _values.Clear();
                for (int i = _queue.Count - 1; i >= 0; i--)
                {
                    _queue[i].Release();
                }
                _queue.Clear();
                InputText = null;
                Pool.Push(this);
            }
            //解析变量
            void _ParseVar(int argIndex, ref zstring input)
            {
                if (!VariableMdDict.TryGetValue(input, out var match))
                {
                    var temMatch = VariableRegex.Match(input);
                    if (temMatch == null || !temMatch.Success)
                    {
                        match = new MatchData();
                    }
                    else
                    {
                        match = new MatchData(temMatch.Value, temMatch.Value);
                    }
                    VariableMdDict.Add(input, match);
                }

                if (string.IsNullOrEmpty(match.Input))
                {
                    return;
                }

                var argKey = zstring.Format(ArgStr, argIndex);
                var parser = Pool.Get<VariableParser>();
                parser.Init(zstring.Format(ArgStr, argIndex), this, match);
                _queue.Add(parser);
                input = input.Replace(match.Input, argKey);
            }
            //解析函数
            void _ParseFn(int argIndex, ref zstring input)
            {
                if (!FnMdDict.TryGetValue(input, out var match))
                {
                    var temMatch = FnRegex.Match(input);
                    if (temMatch == null || !temMatch.Success)
                    {
                        match = new MatchData();
                    }
                    else
                    {
                        match = new MatchData(
                            temMatch.Value,
                            temMatch.Groups["varName"].Value,
                            temMatch.Groups["fnName"].Value);
                    }
                    FnMdDict.Add(input, match);
                }
                argIndex++;
                if (string.IsNullOrEmpty(match.Input))
                {
                    _ParseVar(argIndex, ref input);
                    return;
                }

                var argKey = zstring.Format(ArgStr, argIndex);
                Type varType = null;
                if (string.IsNullOrEmpty(match.V2))
                {
                    varType = typeof(VariableParser);
                }
                else
                {
                    varType = typeof(FunctionParser);
                }
                var parser = Pool.Get<VariableParser>(varType);
                parser.Init(argKey, this, match);
                _queue.Add(parser);

                input = input.Replace(match.Input, argKey);
                if (input == argKey) return;
                _ParseFn(argIndex, ref input);
            }

            public double TryGetValue(string text)
            {
                if (_values.TryGetValue(text, out var v))
                {
                    return v;
                }
                return double.NaN;
            }
            public double Value
            {
                get
                {
                    _values.Clear();
                    for (int i = 0; i < _queue.Count; i++)
                    {
                        var q = _queue[i];
                        if (i == _queue.Count - 1)
                        {
                            return q.Value;
                        }
                        _values[q.Key] = q.Value;
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


    }
}
