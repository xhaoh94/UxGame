using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ux
{
    public partial class EvalMgr
    {

        static IDictionary<string, MatchData> OperatorMdDict = new Dictionary<string, MatchData>();
        static IDictionary<string, MatchData> Operator1MdDict = new Dictionary<string, MatchData>();
        static IDictionary<string, MatchData> Operator2MdDict = new Dictionary<string, MatchData>();
        static IDictionary<string, MatchData> FnMdDict = new Dictionary<string, MatchData>();
        static IDictionary<string, MatchData> VariableMdDict = new Dictionary<string, MatchData>();
        static IDictionary<string, MatchData> ValueMdDict = new Dictionary<string, MatchData>();
        static IDictionary<string, string[]> ArgSplitDict = new Dictionary<string, string[]>();
        static IDictionary<string, bool> ArgDict = new Dictionary<string, bool>();
        static IDictionary<string, ValueBool> ValueDict = new Dictionary<string, ValueBool>();

        enum OperatorType
        {
            None,
            Add,
            Subtract,
            Multiply,
            Divide,
            Remainder
        }
        struct ValueBool
        {
            public ValueBool(string input)
            {
                if (ValueRegex.IsMatch(input) && double.TryParse(input, out var _v))
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
            public bool IsValue { get; }
            public double Value { get; }
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
            protected EvalParse ep;
            public BaseType(string key, EvalParse ep, MatchData match)
            {
                this.Key = key;
                this.ep = ep;
                this.match = match;
            }
            bool IsArgs(string input)
            {
                if (!ArgDict.TryGetValue(input, out var b))
                {
                    b = ArgRegex.IsMatch(input);
                    ArgDict.Add(input, b);
                }
                return b;
            }
            bool IsValue(string input, ref double value)
            {
                if (ValueDict.TryGetValue(input, out var vb))
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
                    Log.Error(string.Format("公式解析错误:{0}", ep.text));
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
                if (variables.TryGetValue(input, out value))
                {
                    return true;
                }
                if (nameToVariable.TryGetValue(input, out var func))
                {
                    value = func();
                    return true;
                }

                if (IsArgs(input))
                {
                    value = ep.values[input];
                    return true;
                }

                if (IsValue(input, ref value))
                {
                    return true;
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
            public FunctionType(string key, EvalParse ep, MatchData match) : base(key, ep, match)
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
                        if (!ArgSplitDict.TryGetValue(content, out var vars))
                        {
                            vars = content.Split(',');
                            ArgSplitDict.Add(content, vars);
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
            public VariableType(string key, EvalParse ep, MatchData match) : base(key, ep, match)
            {
            }
            public override double Value => GetValue(match.V1);
        }
        class EvalParse
        {
            public Dictionary<string, double> values = new Dictionary<string, double>();
            public string text;

            List<BaseType> _queue = new List<BaseType>();
            string _argValue;

            public EvalParse(string _text)
            {
                text = _text;
                zstring input;
                using (zstring.Block())
                {
                    input = _text;
                    if (_Parse(ValueMdDict, ValueRegex, 1, ref input))
                    {
                        return;
                    }
                    if (_Parse(VariableMdDict, VariableRegex, 1, ref input))
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
                if (!FnMdDict.TryGetValue(input, out var match))
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
                    FnMdDict.Add(input, match);
                }
                argIndex++;
                if (string.IsNullOrEmpty(match.Value))
                {
                    _Parse(OperatorMdDict, OperatorRegex, argIndex, ref input);
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
    }
}
