using System;
using System.Collections.Generic;

namespace Ux
{
    public partial class EvalMgr
    {
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

    }
}
