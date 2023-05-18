using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Analysis
{
    public enum ArgType
    {
        //公式
        Formula,

        //常数
        Constant,

        //变量
        Variable,
    }

    public class AnalysisMain
    {
        public AnalysisMain()
        {
            InitSymbol();
            RegGlobalFunc();
            //FormulaTool.TT();
            //RegGlobalForParse();
            var test = GetRPN("(-10)+5*1+5*PARAM", out var tt);
        }

        public void TT(string eval)
        {
            SetFormula("getRemainsStarParameter", (object[] args) =>
            {
                return Convert.ToDouble(args[0]) + Convert.ToDouble(args[1]);
            });
            SetFormula("getRemainsAwakenParameter", (object[] args) =>
            {
                return 1;
            });
            SetFormula("getDisplaysParameter", (object[] args) =>
            {
                return 1;
            });            
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            var t = GetExpressionValue(eval); 
            sw.Stop();
            Log.Debug("22 Eval Parse Time " + sw.ElapsedMilliseconds);
            Log.Debug("22 Eval Parse Time " + t.ToString());
        }

        //public void RegGlobalForParse()
        //{
        //}

        //public ExpressionParser Parser
        //{
        //    get
        //    {
        //        return parser;
        //    }
        //}

        #region 注册全局方法

        public void RegGlobalFunc()
        {
            SetFormula("max", (args) =>
            {
                var strs = args as string[];
                if (strs == null || strs.Length < 2)
                {
                    Log.Error("max函数参数类型或数量错误");
                    return 0;
                }

                var x = double.Parse(strs[0]);
                var y = double.Parse(strs[1]);
                return x > y ? x : y;
            });

            SetFormula("MAX", (args) =>
            {
                var strs = args as string[];
                if (strs == null || strs.Length < 2)
                {
                    Log.Error("max函数参数类型或数量错误");
                    return 0;
                }

                var x = double.Parse(strs[0]);
                var y = double.Parse(strs[1]);
                return x > y ? x : y;
            });

            SetFormula("min", (args) =>
            {
                var strs = args as string[];
                if (strs == null || strs.Length < 2)
                {
                    Log.Error("min函数参数类型或数量错误");
                    return 0;
                }

                var x = double.Parse(strs[0]);
                var y = double.Parse(strs[1]);
                return x < y ? x : y;
            });


            SetFormula("MIN", (args) =>
            {
                var strs = args as string[];
                if (strs == null || strs.Length < 2)
                {
                    Log.Error("min函数参数类型或数量错误");
                    return 0;
                }

                var x = double.Parse(strs[0]);
                var y = double.Parse(strs[1]);
                return x < y ? x : y;
            });

            SetFormula("floor", (args) =>
            {
                var strs = args as string[];
                if (strs == null || strs.Length == 0)
                {
                    Log.Error("floor函数参数类型或数量错误");
                    return 0;
                }

                var x = double.Parse(strs[0]);
                return Math.Floor(x);
            });

            SetFormula("INT", (args) =>
            {
                var strs = args as string[];
                if (strs == null || strs.Length == 0)
                {
                    Log.Error("INT函数参数类型或数量错误");
                    return 0;
                }

                var x = double.Parse(strs[0]);
                return Math.Floor(x);
            });

            SetFormula("ceil", (args) =>
            {
                var strs = args as string[];
                if (strs == null || strs.Length == 0)
                {
                    Log.Error("ceil函数参数类型或数量错误");
                    return 0;
                }

                var x = double.Parse(strs[0]);
                return Math.Ceiling(x);
            });

            SetFormula("pow", (args) =>
            {
                var strs = args as string[];
                if (strs == null || strs.Length < 2)
                {
                    Log.Error("pow函数参数类型或数量错误");
                    return 0;
                }

                var x = double.Parse(strs[0]);
                var y = double.Parse(strs[1]);
                return Math.Pow(x, y);
            });

            SetFormula("halfUp", (args) =>
            {
                var strs = args as string[];
                if (strs == null || strs.Length < 2)
                {
                    Log.Error("halfUp函数参数类型或数量错误");
                    return 0;
                }

                var x = double.Parse(strs[0]);
                int bit = int.Parse(strs[1]);
                return Math.Round(x, bit);
            });

        }

        #endregion

        /// <summary>
        /// 清理所有非全局方法
        /// </summary>
        public void ClearAllNonResidentFunc()
        {
            FuncDic.Clear();
            MaxFuncDic.Clear();
            RegGlobalFunc();
        }

        /// <summary>
        /// 设定运算符的优先级
        /// </summary>
        private void InitSymbol()
        {
            mathSymbol = new Dictionary<string, int>();
            mathSymbol.Add("*", 1);
            mathSymbol.Add("/", 1);
            mathSymbol.Add("+", 2);
            mathSymbol.Add("-", 2);
            mathSymbol.Add("&", 3);
            mathSymbol.Add("|", 3);
            mathSymbol.Add("=", 3);
        }


        Dictionary<string, int> mathSymbol;

        /// <summary>
        /// 计算解析完成的后缀表达式公式
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public double Analysis(Expression expression)
        {
            Queue<string> formulaQueue = new Queue<string>(expression.FormulaQueue.ToArray());
            var argDic = expression.ArgsDic;
            Stack<double> argStack = new Stack<double>();
            for (int i = formulaQueue.Count; i > 0; i--)
            {
                var s = formulaQueue.Dequeue();
                if (IsSymbol(s))
                {
                    var a = argStack.Pop();
                    var b = argStack.Pop();
                    switch (s)
                    {
                        case "+":
                            argStack.Push(b + a);
                            break;
                        case "-":
                            argStack.Push(b - a);
                            break;
                        case "*":
                            argStack.Push(b * a);
                            break;
                        case "/":
                            argStack.Push(b / a);
                            break;
                        case "&":
                            argStack.Push(Convert.ToDouble(Convert.ToBoolean(a) && Convert.ToBoolean(b)));
                            break;
                        case "|":
                            argStack.Push(Convert.ToDouble(Convert.ToBoolean(a) || Convert.ToBoolean(b)));
                            break;
                        case "=":
                            argStack.Push(b + a - a);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    if (expression.ArgTypeDic[s] == ArgType.Constant)
                    {
                        argStack.Push(double.Parse(argDic[s]));
                    }
                    else if (expression.ArgTypeDic[s] == ArgType.Formula)
                    {
                        var formula = expression.FormulaDic[argDic[s]];
                        argStack.Push(formula.GetValue());
                    }
                    else if (expression.ArgTypeDic[s] == ArgType.Variable)
                    {
                        if (VariableDic.TryGetValue(argDic[s], out var value))
                        {
                            argStack.Push(value);
                        }
                        else if (VariableFuncDic.TryGetValue(argDic[s], out var fn))
                        {
                            argStack.Push(fn.Invoke());
                        }
                        else
                        {
                            argStack.Push(0);
                        }
                    }
                }
            }

            return argStack.Pop();
        }

        public Dictionary<string, Func<object[], double>> FuncDic { get; private set; } =
            new Dictionary<string, Func<object[], double>>();
        public Dictionary<string, Func<object[], bool>> MaxFuncDic { get; private set; } =
            new Dictionary<string, Func<object[], bool>>();
        public Dictionary<string, double> VariableDic { get; private set; } = new Dictionary<string, double>();
        public Dictionary<string, Func<double>> VariableFuncDic { get; private set; } = new Dictionary<string, Func<double>>();

        /// <summary>
        /// 设置公式，可手动设置也可以监听公式设置
        /// </summary>
        /// <param name="formulaName"></param>
        /// <param name="func"></param>
        public void SetFormula(string formulaName, Func<object[], double> func)
        {
            FuncDic[formulaName] = func;
        }
        public void SetMaxFormula(string formulaName, Func<object[], bool> func)
        {
            if (!MaxFuncDic.ContainsKey(formulaName))
                MaxFuncDic[formulaName] = func;
        }

        public void RemoveFormula(string formulaName)
        {
            if (FuncDic.ContainsKey(formulaName))
                FuncDic.Remove(formulaName);
        }

        public void RemoveMaxFormula(string formulaName)
        {
            if (MaxFuncDic.ContainsKey(formulaName))
                MaxFuncDic.Remove(formulaName);
        }
        /// <summary>
        /// 设置变量
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        public void SetVariable(string variable, double value)
        {
            VariableFuncDic.Remove(variable);
            VariableDic[variable] = value;
        }
        /// <summary>
        /// 设置变量
        /// </summary>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        public void SetVariable(string variable, Func<double> func)
        {
            VariableDic.Remove(variable);
            VariableFuncDic[variable] = func;
        }

        public void RemoveVariable(string variable)
        {
            if (VariableDic.ContainsKey(variable))
                VariableDic.Remove(variable);
            if (VariableFuncDic.ContainsKey(variable))
                VariableFuncDic.Remove(variable);
        }

        public static Dictionary<string, string> _Dictionary = new Dictionary<string, string>();

        /// <summary>
        /// 公式文本解析，将解析出的常量、公式和变量替换成@args_number
        /// </summary>
        /// <param name="input"></param>
        /// <param name="dic"></param>
        /// <returns></returns>
        public string TextParse(string input, Dictionary<string, string> dic)
        {
            if (_Dictionary.TryGetValue(input, out String str))
            {
                return str;
            }

            //[(](?<v>([0-9]|[a-z]|[A-Z]|[+\-*/?$\x22]+)+)[)]

            StringBuilder sb = new StringBuilder(input);            
            StringBuilder temp = new StringBuilder();
            Stack<char> stack = new Stack<char>();
            int lasEnd = 0;
            int index = 0;
            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] == '(')
                {
                    if (Regex.IsMatch(temp.ToString(), @"\w+"))
                    {
                        temp.Append(sb[i]);
                        stack.Push(sb[i]);
                        for (int j = i + 1; j < sb.Length; j++)
                        {
                            if (sb[j] == '(')
                                stack.Push(sb[j]);
                            else if (sb[j] == ')')
                                stack.Pop();
                            temp.Append(sb[j]);
                            if (stack.Count == 0)
                            {
                                i = j;
                                break;
                            }
                        }
                    }
                    else
                    {
                        lasEnd++;
                        continue;
                    }
                }

                if (mathSymbol.ContainsKey(sb[i].ToString()))
                {
                    var s = temp.ToString();
                    sb.Remove(lasEnd, s.Length);
                    var sign = string.Format("@args_{0}", index);
                    sb.Insert(lasEnd, sign);
                    dic.Add(sign, s);
                    i -= (s.Length - sign.Length);
                    index++;
                    lasEnd = i + 1;
                    temp.Clear();
                }
                else
                {
                    if (sb[i] != ')')
                    {
                        if (sb[i] == ',')
                        {
                            var s = temp.ToString();
                            sb.Remove(lasEnd, s.Length);
                            var sign = string.Format("@args_{0}", index);
                            sb.Insert(lasEnd, sign);
                            dic.Add(sign, s);
                            i -= (s.Length - sign.Length);
                            index++;
                            lasEnd = i + 1;
                            temp.Clear();
                        }
                        else
                        {
                            temp.Append(sb[i]);
                        }
                    }
                }

                if (i >= sb.Length - 1 && lasEnd < sb.Length)
                {
                    var s = temp.ToString();
                    sb.Remove(lasEnd, s.Length);
                    var sign = string.Format("@args_{0}", index);
                    sb.Insert(lasEnd, sign);
                    dic.Add(sign, s);
                    i -= (s.Length - sign.Length);
                    temp.Clear();
                    break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 将公式文本转化为后缀表达式队列
        /// </summary>
        /// <param name="formula">公式文本</param>
        /// <param name="argsDic">占位符@args_number对应的常量、变量、方法</param>
        /// <returns></returns>
        public Queue<string> GetRPN(string formula, out Dictionary<string, string> argsDic)
        {
            var dic = new Dictionary<string, string>();
            Stack<char> temp = new Stack<char>();
            Queue<string> right = new Queue<string>();
            Queue<string> targer = new Queue<string>();
            var dealFormula = TextParse(formula, dic);
            argsDic = dic;
            return GetQueueByDealText(dealFormula, out _);
        }

        public static Dictionary<string, Queue<string>> _Collections = new Dictionary<string, Queue<string>>();

        /// <summary>
        /// 将处理完毕的公式转化为后缀表达式
        /// </summary>
        /// <param name="dealFormula"></param>
        /// <param name="argsMatch"></param>
        /// <returns></returns>
        public Queue<string> GetQueueByDealText(string dealFormula, out MatchCollection argsMatch)
        {
            argsMatch = Regex.Matches(dealFormula, "@args_?[0-9]+");
            Queue<string> targer = new Queue<string>();

            if (_Collections.TryGetValue(dealFormula, out Queue<string> queue))
            {
                return queue;
            }

            Stack<char> temp = new Stack<char>();
            Queue<string> right = new Queue<string>();
            if (argsMatch.Count <= 0)
                return targer;
            var argIndex = 0;
            for (int i = 0; i < dealFormula.Length;)
            {
                int j = 0;

                switch (dealFormula[i])
                {
                    case '(':
                        temp.Push(dealFormula[i]);
                        i++;
                        break;
                    case ')':
                        for (j = temp.Count; j > 0; j--)
                        {
                            char s = temp.Pop();
                            if (s == '(')
                            {
                                break;
                            }
                            else
                            {
                                right.Enqueue(s.ToString());
                            }
                        }

                        i++;
                        break;
                    default:
                        if (!mathSymbol.ContainsKey(dealFormula[i].ToString()))
                        {
                            var arg = argsMatch[argIndex];
                            i += arg.Value.Length;
                            argIndex++;
                            right.Enqueue(arg.Value);
                        }
                        else
                        {
                            DealSymbol(right, temp, dealFormula[i]);
                            i++;
                        }

                        break;
                }
            }

            if (temp.Count > 0)
            {
                for (int i = temp.Count; i > 0; i--)
                {
                    right.Enqueue(temp.Pop().ToString());
                }
            }

            for (int i = right.Count; i > 0; i--)
            {
                targer.Enqueue(right.Dequeue());
            }

            return targer;
        }

        private void DealSymbol(Queue<string> valueQueue, Stack<char> symbolStack, char symbol)
        {
            if (symbolStack.Count == 0 || symbolStack.Peek() == '(')
            {
                symbolStack.Push(symbol);
                return;
            }
            else if (mathSymbol[symbolStack.Peek().ToString()] > mathSymbol[symbol.ToString()])
            {
                symbolStack.Push(symbol);
                return;
            }
            else
            {
                valueQueue.Enqueue(symbolStack.Pop().ToString());
                DealSymbol(valueQueue, symbolStack, symbol);
            }
        }

        public bool IsSymbol(string input)
        {
            return mathSymbol.ContainsKey(input);
        }

        public bool IsSymbolOrFormula(string input, out List<string> values, out List<bool> isSymbols)
        {
            var matchs = Regex.Matches(input, @"\{[\$\&][\s\S]+?\}");
            values = new List<string>();
            isSymbols = new List<bool>();
            if (matchs.Count == 0)
                return false;
            for (int i = 0; i < matchs.Count; i++)
            {
                var match = matchs[i];
                isSymbols.Add(Regex.IsMatch(match.Value, @"\&"));
                values.Add(ClearSpace(Regex.Match(match.Value, @"(?<=[\$\&])[\s\S]*(?=\})").Value));
            }

            return true;
        }

        public string ClearSpace(string text)
        {
            return Regex.Replace(text, @"\s", "");
        }

        public string AnalysisDesc(string input)
        {
            if (IsSymbolOrFormula(input, out var values, out var isSymbols))
            {
                List<double> list = new List<double>();
                for (int i = 0; i < values.Count; i++)
                {
                    if (!isSymbols[i])
                    {
                        list.Add(GetExpressionValue(values[i]));
                    }
                }

                int index = 0;
                int tempIndex = 0;
                var output = Regex.Replace(input, @"\{[\$\&][\s\S]+?\}", (s) =>
                {
                    if (index < list.Count)
                    {
                        var temp = list[index].ToString();
                        index++;
                        return temp;
                    }
                    else
                    {
                        index++;
                        var temp = $"{{{tempIndex}}}";
                        tempIndex++;
                        return temp;
                    }
                });
                return output;
            }
            else
                return input;
        }


        public static string MidStrEx(string sourse, string startstr, string endstr)
        {
            string result = string.Empty;
            int startindex, endindex;
            try
            {
                startindex = sourse.IndexOf(startstr);
                if (startindex == -1)
                    return result;
                string tmpstr = sourse.Substring(startindex + startstr.Length);
                endindex = tmpstr.IndexOf(endstr);
                if (endindex == -1)
                    return result;
                result = tmpstr.Remove(endindex);
            }

            catch (Exception ex)
            {
                // Log.WriteLog("MidStrEx Err:" + ex.Message);
            }
            return result;
        }

        public bool GetFormulaText(string input, out List<string> values, out List<bool> isSymbols,
            out string dealInput)
        {
            var index = 0;
            dealInput = Regex.Replace(input, @"\{[\$\&][\s\S]+?\}", (s) =>
            {
                var temp = $"{{{index}}}";
                index++;
                return temp;
            });
            return IsSymbolOrFormula(input, out values, out isSymbols);
        }

        /// <summary>
        /// 公式求值
        /// </summary>
        /// <param name="text">公式文本</param>
        /// <returns></returns>
        public double GetExpressionValue(string text)
        {
            if (double.TryParse(text, out var value))
            {
                return value;
            }
            var input = ClearSpace(text);
            var e = GetOrCreate(input);
            return e.GetValue();
        }

        public Expression GetOrCreate(string input)
        {
            if (expressionDic.TryGetValue(input, out var expression))
            {
                return expression;
            }
            else
            {                
                var e = new Expression(input, this);
                expressionDic.Add(input, e);
                return e;
            }
        }


        private Dictionary<string, Expression> expressionDic = new Dictionary<string, Expression>();


        #region 属性公式

        Dictionary<string, double> attributeDic;

        /// <summary>
        /// 设置属性字典
        /// </summary>
        /// <param name="dic"></param>
        public void SetAttributeDic(Dictionary<string, double> dic)
        {
            attributeDic = dic;
        }


        #endregion
    }

    public class Expression
    {
        AnalysisMain analysisMain;

        /// <summary>
        /// 后缀表达式队列
        /// </summary>
        public Queue<string> FormulaQueue { get; private set; }

        Dictionary<string, string> argsDic;

        Dictionary<string, ArgType> argTypeDic = new Dictionary<string, ArgType>();

        Dictionary<string, Formula> formulaDic = new Dictionary<string, Formula>();

        /// <summary>
        /// 公式占位符字典，key：@args_number，vakue：被替换掉源文本，可能是常量、公式或变量
        /// </summary>
        public Dictionary<string, string> ArgsDic
        {
            get => argsDic;
        }
        /// <summary>
        /// 公式占位符类型字典，key：@args_number,value: 类型
        /// </summary>
        public Dictionary<string, ArgType> ArgTypeDic
        {
            get => argTypeDic;
        }
        /// <summary>
        /// 函数字典，key：函数文本，value：函数
        /// </summary>
        public Dictionary<string, Formula> FormulaDic
        {
            get => formulaDic;
        }

        public Expression(string text, AnalysisMain analysisMain)
        {
            this.analysisMain = analysisMain;
            FormulaQueue = analysisMain.GetRPN(text, out argsDic);

            foreach (var item in argsDic)
            {
                if (IsValue(item.Value))
                {
                    argTypeDic.Add(item.Key, ArgType.Constant);
                }
                else if (IsFormula(item.Value))
                {
                    argTypeDic.Add(item.Key, ArgType.Formula);
                    formulaDic[item.Value] = Formula.CreateFormula(item.Value, analysisMain);
                }
                else if (IsVariable(item.Value))
                {
                    argTypeDic.Add(item.Key, ArgType.Variable);
                }
            }
        }

        public Expression(string dealText, Dictionary<string, string> dic, AnalysisMain analysisMain)
        {
            this.analysisMain = analysisMain;
            FormulaQueue = analysisMain.GetQueueByDealText(dealText, out var match);
            argsDic = new Dictionary<string, string>();
            for (int i = 0; i < match.Count; i++)
            {
                if (dic.ContainsKey(match[i].Value))
                {
                    argsDic.Add(match[i].Value, dic[match[i].Value]);

                    if (IsValue(dic[match[i].Value]))
                    {
                        argTypeDic.Add(match[i].Value, ArgType.Constant);
                    }
                    else if (IsFormula(dic[match[i].Value]))
                    {
                        argTypeDic.Add(match[i].Value, ArgType.Formula);
                        formulaDic[dic[match[i].Value]] = Formula.CreateFormula(dic[match[i].Value], analysisMain);
                    }
                    else if (IsVariable(dic[match[i].Value]))
                    {
                        argTypeDic.Add(match[i].Value, ArgType.Variable);
                    }
                }
            }
        }

        public double GetValue()
        {
            return analysisMain.Analysis(this);
        }

        public static bool IsValue(string input)
        {
            return Regex.IsMatch(input, @"^[-]?\d*[.]?\d+$")/* && !Regex.IsMatch(input, @"[a-zA-Z]")*/;
        }

        public static bool IsFormula(string input)
        {
            return Regex.IsMatch(input, @"\w+\([\s\S]*?\)");
        }

        public static bool IsVariable(string input)
        {
            return !IsFormula(input) && !IsValue(input) && Regex.IsMatch(input, @"[a-zA-Z]") &&
                   !Regex.IsMatch(input, @"""\w+""");
        }
    }

    public class Formula
    {
        AnalysisMain analysisMain;
        public Formula(AnalysisMain analysisMain)
        {
            this.analysisMain = analysisMain;
        }
        public static Dictionary<string, Formula> FormulaDic { get; private set; } = new Dictionary<string, Formula>();

        public string Name { get; private set; }

        object[] args;

        public object[] Args
        {
            get => args;
        }


        public static Formula CreateFormula(string formula, AnalysisMain analysisMain)
        {
            if (FormulaDic.TryGetValue(formula, out var f))
            {
                return f;
            }
            else
            {
                var temp = new Formula(formula, analysisMain);
                FormulaDic.Add(formula, temp);
                return temp;
            }
        }

        private Formula(string formula, AnalysisMain analysisMain)
        {
            this.analysisMain = analysisMain;
            Name = GetFormulaName(formula);
            if (GetFormulaParameter(formula, out var temp_args, out var dic))
            {
                args = new object[temp_args.Length];
                for (int i = 0; i < temp_args.Length; i++)
                {
                    if (dic.ContainsKey(temp_args[i]))
                    {
                        var isArg = Regex.IsMatch(dic[temp_args[i]], @"""\w+""");
                        if (!Expression.IsValue(temp_args[i]) && !isArg)
                        {
                            args[i] = analysisMain.GetOrCreate(dic[temp_args[i]]);
                        }
                        else if (isArg && !Expression.IsFormula(dic[temp_args[i]]))
                        {
                            args[i] = Regex.Replace(dic[temp_args[i]], @"""", "");
                        }
                        else
                        {
                            args[i] = analysisMain.GetOrCreate(dic[temp_args[i]]);
                        }
                    }
                    else
                    {
                        args[i] = new Expression(temp_args[i], dic, analysisMain);
                    }
                }
            }
        }

        /// <summary>
        /// 获取函数名称
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string GetFormulaName(string input)
        {
            return Regex.Match(input, "[a-zA-z]+?(?=\\([\\s\\S]*\\))").Value;
        }
        /// <summary>
        /// 获取函数参数，参数可能是另一个函数或另一条公式
        /// </summary>
        /// <param name="input"></param>
        /// <param name="args"></param>
        /// <param name="dic"></param>
        /// <returns></returns>
        public bool GetFormulaParameter(string input, out string[] args, out Dictionary<string, string> dic)
        {
            Match match;
            if ((match = Regex.Match(input, @"(?<=\().+(?=\))")) != null)
            {
                dic = new Dictionary<string, string>();
                var dealFormula = analysisMain.TextParse(match.Value, dic);
                var matchs = Regex.Matches(dealFormula, @"[^,\s]+");
                args = new string[matchs.Count];
                for (int i = 0; i < matchs.Count; i++)
                {
                    args[i] = matchs[i].Value;
                }

                return true;
            }

            args = null;
            dic = null;
            return false;
        }

        public double GetValue()
        {
            if (analysisMain.FuncDic.TryGetValue(Name, out var func))
            {
                var temp = new string[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] is Expression)
                    {
                        temp[i] = (args[i] as Expression).GetValue().ToString();
                    }
                    else
                    {
                        temp[i] = args[i].ToString();
                    }
                }

                return func.Invoke(temp);
            }
            else
            {
                Log.Error($"{Name} 公式未注册解析!");
                return 0;
            }


        }


    }
}