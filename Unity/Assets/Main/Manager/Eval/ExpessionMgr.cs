using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodingSeb.ExpressionEvaluator;
using System;
using System.Text.RegularExpressions;
using System.Text;
using static UnityEngine.InputSystem.InputControlScheme.MatchResult;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine.Windows;
using UnityEngine.InputSystem;

namespace Ux
{
    public class ExpessionMgr : Singleton<ExpessionMgr>
    {
        enum OperatorType
        {
            Operator1,
            Operator2
        }
        enum ArgType
        {
            Arg,
            Value,

        }
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
        static StringBuilder stringBuilder = new StringBuilder();
        static IDictionary<string, double> variables = new Dictionary<string, double>() {
            { "Pi", Math.PI },
            { "pi", Math.PI },
            { "E", Math.E },
            { "e", Math.E },
        };
        static IDictionary<string, Func<double>> nameToVariable = new Dictionary<string, Func<double>>();
        static IDictionary<string, Func<object[], double>> nameToFunction = new Dictionary<string, Func<object[], double>>();
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
        };

        static IDictionary<string, ExpessionMatch> Operator1Dict = new Dictionary<string, ExpessionMatch>();
        static IDictionary<string, ExpessionMatch> Operator2Dict = new Dictionary<string, ExpessionMatch>();
        static IDictionary<string, ExpessionMatch> FnDict = new Dictionary<string, ExpessionMatch>();
        static IDictionary<string, ExpessionMatch> VariableDict = new Dictionary<string, ExpessionMatch>();
        static IDictionary<string, ExpessionMatch> ValueDict = new Dictionary<string, ExpessionMatch>();
        static IDictionary<string, ExpessionMatch> OperatorDict = new Dictionary<string, ExpessionMatch>();
        static IDictionary<string, string[]> ArgDict = new Dictionary<string, string[]>();
        static IDictionary<string, ArgType> ArgTypeDict = new Dictionary<string, ArgType>();
        #endregion

        #region Type

        struct ExpessionMatch
        {
            public string Value { get; }
            public string V1 { get; }
            public string V2 { get; }
            public string Data { get; }
            public ExpessionMatch(string v, string v1, string v2, string data)
            {
                this.Value = v;
                this.V1 = v1;
                this.V2 = v2;
                this.Data = data;
            }
        }
        class BaseType
        {
            public string key;
            protected ExpessionMatch match;
            protected ExpessionParse expession;
            public BaseType(string key, ExpessionParse expession, ExpessionMatch match)
            {
                this.expession = expession;
                this.key = key;
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
                    Log.Error($"公式解析错误:{expession.text}");
                    return 0;
                }
                if (TryGetValue(input, out var value))
                {
                    return value;
                }
                double v = 0;
                int r = 0;
                stringBuilder.Clear();
                stringBuilder.Append(input);
                while (true)
                {
                    if (r <= 1 && TryParse(stringBuilder, OperatorType.Operator1, ref v))
                    {
                        r = 1;
                        continue;
                    }
                    if (r <= 2 && TryParse(stringBuilder, OperatorType.Operator2, ref v))
                    {
                        r = 2;
                        continue;
                    }
                    break;
                }
                if (r > 0)
                {
                    return v;
                }
                Log.Error($"公式解析错误:{stringBuilder.ToString()}");
                return 0;
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
            bool TryParse(StringBuilder input, OperatorType type, ref double v)
            {
                Regex regex = null;
                ExpessionMatch match;
                IDictionary<string, ExpessionMatch> dict = null;

                switch (type)
                {
                    case OperatorType.Operator1:
                        dict = Operator1Dict;
                        regex = Operator1;
                        break;
                    case OperatorType.Operator2:
                        dict = Operator2Dict;
                        regex = Operator2;
                        break;
                }
                if (dict == null || regex == null) return false;

                var key = input.ToString();
                if (!dict.TryGetValue(key, out match))
                {
                    var temMatch = regex.Match(key);
                    if (temMatch == null || !temMatch.Success)
                    {
                        return false;
                    }
                    var v1 = temMatch.Groups["var1"].Value;
                    var v2 = temMatch.Groups["var2"].Value;
                    var tag = temMatch.Groups["tag"].Value;
                    match = new ExpessionMatch(temMatch.Value, v1, v2, tag);
                    dict.Add(key, match);
                }

                if (!TryGetValue(match.V1, out var arg1))
                {
                    return false;
                }
                if (!TryGetValue(match.V2, out var arg2))
                {
                    return false;
                }
                switch (match.Data)
                {
                    case "%":
                        v = arg1 % arg2;
                        break;
                    case "*":
                        v = arg1 * arg2;
                        break;
                    case "/":
                        v = arg1 / arg2;
                        break;
                    case "+":
                        v = arg1 + arg2;
                        break;
                    case "-":
                        v = arg1 - arg2;
                        break;
                    default:
                        Log.Error($"未知的数学运算符{match.Data}");
                        break;
                }
                input.Replace(match.Value, v.ToString());
                return true;
            }

            public virtual double Value => 0;
        }
        class FunctionType : BaseType
        {
            public FunctionType(string key, ExpessionParse expession, ExpessionMatch match) : base(key, expession, match)
            {
            }
            public override double Value
            {
                get
                {
                    if (singleDoubleMath.TryGetValue(match.V1, out var singleFunc))
                    {
                        return singleFunc(GetValue(match.V2));
                    }
                    if (!nameToFunction.TryGetValue(match.V1, out var func))
                    {
                        return 0;
                    }
                    if (match.V2.Length > 0)
                    {
                        if (!ArgDict.TryGetValue(match.V2, out var vars))
                        {
                            vars = match.V2.Split(',');
                            ArgDict.Add(match.V2, vars);
                        }
                        var objs = new object[vars.Length];
                        for (int i = 0; i < vars.Length; i++)
                        {
                            objs[i] = GetValue(vars[i]);
                        }
                        return func.Invoke(objs);
                    }
                    else
                    {
                        return func.Invoke(null);
                    }
                }
            }
        }
        class VariableType : BaseType
        {
            public VariableType(string key, ExpessionParse expession, ExpessionMatch match) : base(key, expession, match)
            {
            }
            public override double Value => GetValue(match.V2);
        }
        class ExpessionParse
        {
            public Dictionary<string, double> values = new Dictionary<string, double>();
            public string text;

            List<BaseType> _queue = new List<BaseType>();
            string _argKey;
            string _key;

            public ExpessionParse(string _text)
            {
                text = _text;
                _key = _text;
                if (_Parse(ValueDict, ValueRegex))
                {
                    return;
                }
                if (_Parse(VariableDict, VariableRegex))
                {
                    return;
                }
                _ParseFn(0);
            }
            bool _Parse(IDictionary<string, ExpessionMatch> dict, Regex regex, int argIndex = 1)
            {
                if (!dict.TryGetValue(_key, out var match))
                {
                    var temMatch = regex.Match(_key);
                    if (temMatch == null || !temMatch.Success)
                    {
                        match = new ExpessionMatch(null, null, null, null);
                    }
                    else
                    {
                        match = new ExpessionMatch(temMatch.Value, null, temMatch.Value, null);
                    }
                    dict.Add(_key, match);
                }

                if (string.IsNullOrEmpty(match.Value))
                {
                    return false;
                }
                _argKey = string.Format(ArgStr, argIndex);
                _queue.Add(new VariableType(_argKey, this, match));
                _key = _key.Replace(match.Value, _argKey);
                return true;
            }

            void _ParseFn(int argIndex)
            {
                if (!FnDict.TryGetValue(_key, out var match))
                {
                    var temMatch = FnRegex.Match(_key);
                    if (temMatch == null || !temMatch.Success)
                    {
                        match = new ExpessionMatch(null, null, null, null);
                    }
                    else
                    {
                        var v1 = temMatch.Groups["fnName"].Value;
                        var v2 = temMatch.Groups["varName"].Value;
                        match = new ExpessionMatch(temMatch.Value, v1, v2, null);
                    }
                    FnDict.Add(_key, match);
                }
                argIndex++;
                if (string.IsNullOrEmpty(match.Value))
                {
                    _Parse(OperatorDict, OperatorRegex, argIndex);
                    return;
                }
                _argKey = string.Format(ArgStr, argIndex);

                if (string.IsNullOrEmpty(match.V1))
                {
                    _queue.Add(new VariableType(_argKey, this, match));
                }
                else
                {
                    _queue.Add(new FunctionType(_argKey, this, match));
                }
                _key = _key.Replace(match.Value, _argKey);
                _ParseFn(argIndex);
            }

            public double Value
            {
                get
                {
                    values.Clear();
                    for (int i = 0; i < _queue.Count; i++)
                    {
                        var _q = _queue[i];
                        values[_q.key] = _q.Value;
                    }
                    return values[_argKey];
                }
            }
            public string Desc => _key.Replace(_argKey, Value.ToString());
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
        public void AddFunction(string name, Func<object[], double> func)
        {
            nameToFunction[name] = func;
        }
        public void RemoveFunction(string name)
        {
            nameToFunction.Remove(name);
        }
        #endregion

        Dictionary<string, ExpessionParse> textToExpssion = new Dictionary<string, ExpessionParse>();

        public double Parse(string _text)
        {
            if (!textToExpssion.TryGetValue(_text, out var e))
            {
                e = new ExpessionParse(_text);
                textToExpssion.Add(_text, e);
            }
            return e.Value;
        }
        public string Desc(string _text)
        {
            if (!textToExpssion.TryGetValue(_text, out var e))
            {
                e = new ExpessionParse(_text);
                textToExpssion.Add(_text, e);
            }
            return e.Desc;
        }
    }
}

