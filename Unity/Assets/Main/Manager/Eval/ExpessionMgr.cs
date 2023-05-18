using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodingSeb.ExpressionEvaluator;
using System;
using System.Text.RegularExpressions;
using System.Text;

namespace Ux
{
    public class ExpessionMgr : Singleton<ExpessionMgr>
    {
        enum OperatorType
        {
            Operator1,
            Operator2
        }
        #region 正则表达式
        static readonly string FnRegex = @"(?<fnName>(\d|[a-z]|[A-Z]?)+)[(](?<varName>(()|\d|[a-z]|[A-Z]|[_,+\-*/%]+)+)[)]";
        static readonly string VariableRegex = @"^[-]?(\w)+[(+|\-|*|/|%)](\w)+";
        static readonly string ValueRegex = @"^[-]?\d*[.]?\d+$";
        static readonly string ArgRegex = @"^($arg_)([\d]+)$";
        static readonly string Operator1 = $@"(?<var1>[-]?[.\d\w]+)(?<tag>[*|/|%])(?<var2>[-]?[.\d\w]+)";
        static readonly string Operator2 = $@"(?<var1>[-]?[.\d\w]+)(?<tag>[+|-])(?<var2>[-]?[.\d\w]+)";
        #endregion

        #region 静态字典
        static IDictionary<string, double> variables = new Dictionary<string, double>();
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
        #endregion

        #region Type
        class BaseType
        {
            public string key;
            protected string varName;
            protected ExpessionParse expession;
            public BaseType(ExpessionParse expession, string key, string varName)
            {
                this.expession = expession;
                this.key = key;
                this.varName = varName;
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
                bool b = false;
                var sb = new StringBuilder(input);
                while (true)
                {
                    if (TryParse(sb, OperatorType.Operator1, ref v))
                    {
                        b = true;                        
                        continue;
                    }
                    if (TryParse(sb, OperatorType.Operator2, ref v))
                    {
                        b = true;                        
                        continue;
                    }
                    break;
                }
                if (b)
                {
                    return v;
                }
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
                if (IsArgs(input))
                {
                    value = expession.values[input];
                    return true;
                }
                else if (IsValue(input))
                {
                    value = Convert.ToDouble(input);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            bool TryParse(StringBuilder input, OperatorType tag, ref double v)
            {
                Match match = null;
                switch (tag)
                {
                    case OperatorType.Operator1:
                        match = Regex.Match(input.ToString(), Operator1);
                        break;
                    case OperatorType.Operator2:
                        match = Regex.Match(input.ToString(), Operator2);
                        break;
                }
                if (match == null)
                {
                    return false;
                }
                var v1 = match.Groups["var1"].Value;
                var v2 = match.Groups["var2"].Value;
                var vtag = match.Groups["tag"].Value;
                if (!TryGetValue(v1, out var arg1))
                {                    
                    return false;
                }
                if (!TryGetValue(v2, out var arg2))
                {                    
                    return false;
                }
                switch (vtag)
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
                        Log.Error($"未知的数学运算符{vtag}");
                        break;
                }
                input.Replace(match.Value, v.ToString());
                return true;
            }

            public virtual double Value => 0;
        }
        class FunctionType : BaseType
        {
            public string _fnName;
            public FunctionType(string key, string fnName, string varName, ExpessionParse expession) : base(expession, key, varName)
            {
                _fnName = fnName;
            }
            public override double Value
            {
                get
                {
                    if (singleDoubleMath.TryGetValue(_fnName, out var singleFunc))
                    {
                        return singleFunc(GetValue(varName));
                    }
                    if (!nameToFunction.TryGetValue(_fnName, out var func))
                    {
                        return 0;
                    }
                    if (varName.Length > 0)
                    {
                        var vars = varName.Split(',');
                        object[] objs = new object[vars.Length];
                        for (int i = 0; i < vars.Length; i++)
                        {
                            objs[i] = GetValue(vars[i]);
                        }
                        return func.Invoke(objs);
                    }
                    else
                    {
                        return func.Invoke(new object[] { });
                    }
                }
            }
        }
        class VariableType : BaseType
        {
            public VariableType(string key, string varName, ExpessionParse expession) : base(expession, key, varName)
            {
            }
            public override double Value => GetValue(varName);
        }
        class ExpessionParse
        {
            public Dictionary<string, double> values;
            public string text;

            Queue<BaseType> _queue;
            string arg_key;
            string _key;

            public ExpessionParse(string _text)
            {
                text = _text;
                _key = _text;
                _ParseFn(0);
            }
            void _ParseVar(int argIndex)
            {
                var m = Regex.Match(_key, VariableRegex);
                if (m == null || m.Captures.Count == 0)
                {
                    return;
                }
                argIndex++;
                arg_key = string.Format("$arg_{0}", argIndex);
                _queue.Enqueue(new VariableType(arg_key, m.Value, this));
                _key = _key.Replace(m.Value, arg_key);
            }
            void _ParseFn(int argIndex)
            {
                var m = Regex.Match(_key, FnRegex);
                if (m == null || m.Captures.Count == 0)
                {
                    _ParseVar(argIndex);
                    return;
                }

                argIndex++;
                arg_key = string.Format("$arg_{0}", argIndex);
                var fnName = m.Groups["fnName"].Value;
                var varName = m.Groups["varName"].Value;
                _queue ??= new Queue<BaseType>();
                if (string.IsNullOrEmpty(fnName))
                {
                    _queue.Enqueue(new VariableType(arg_key, varName, this));
                }
                else
                {
                    _queue.Enqueue(new FunctionType(arg_key, fnName, varName, this));
                }
                _key = _key.Replace(m.Value, arg_key);
                _ParseFn(argIndex);
            }

            public double Value
            {
                get
                {
                    if (IsValue(arg_key))
                    {
                        return Convert.ToDouble(arg_key);
                    }
                    values ??= new Dictionary<string, double>();
                    values.Clear();
                    while (_queue.Count > 0)
                    {
                        var _q = _queue.Dequeue();
                        values[_q.key] = _q.Value;
                    }
                    return values[arg_key];
                }
            }
            public string Desc => _key.Replace(arg_key, Value.ToString());
        }
        #endregion

        #region 常量
        /// <summary>
        /// 设置常量
        /// </summary>
        public void AddVariables(string name, double value)
        {
            nameToVariable.Remove(name);
            variables[name] = value;
        }
        /// <summary>
        /// 设置常量
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func">获取常量函数</param>
        public void AddVariables(string name, Func<double> func)
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
        static bool IsArgs(string input)
        {
            return Regex.IsMatch(input, ArgRegex);
        }
        static bool IsValue(string input)
        {
            return Regex.IsMatch(input, ValueRegex);
        }
        public double Parse(string _text)
        {
            if (!textToExpssion.TryGetValue(_text, out var e))
            {
                e = new ExpessionParse(_text);
                textToExpssion.Add(_text, e);
            }
            return e.Value;
        }
    }
}

