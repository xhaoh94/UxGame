using System;
using System.Collections.Generic;
using System.Linq;
namespace Ux
{
    public partial class EvalMgr
    {
        // 内置函数表
        static Dictionary<string, Func<double[], double>> _builtinFunctions =
            new Dictionary<string, Func<double[], double>>(StringComparer.OrdinalIgnoreCase)
            {
                ["max"] = args => args.Max(),
                ["min"] = args => args.Min(),
                ["pow"] = args => Math.Pow(args[0], args[1]),
                ["abs"] = args => Math.Abs(args[0]),
                ["acos"] = args => Math.Acos(args[0]),
                ["asin"] = args => Math.Asin(args[0]),
                ["atan"] = args => Math.Atan(args[0]),
                ["ceiling"] = args => Math.Ceiling(args[0]),
                ["ceil"] = args => Math.Ceiling(args[0]),
                ["cos"] = args => Math.Cos(args[0]),
                ["cosh"] = args => Math.Cosh(args[0]),
                ["exp"] = args => Math.Exp(args[0]),
                ["floor"] = args => Math.Floor(args[0]),
                ["sin"] = args => Math.Sin(args[0]),
                ["sqrt"] = args => Math.Sqrt(args[0]),
                ["tan"] = args => Math.Tan(args[0]),
                ["round"] = args => Math.Round(args[0]),
                ["avg"] = args => args.Average(),
                ["sum"] = args => args.Sum(),
                ["if"] = args => args[0] > 0 ? args[1] : args[2],
            };

        static IDictionary<string, Func<double[], double>> nameToDoubleFunc = new Dictionary<string, Func<double[], double>>();       

        /// <summary>
        /// 设置函数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func">获取函数</param>
        public void AddFunction(string name, Func<double[], double> func)
        {
            nameToDoubleFunc[name] = func;
        }
        public void RemoveFunction(string name)
        {
            nameToDoubleFunc.Remove(name);
        }
    }
}
