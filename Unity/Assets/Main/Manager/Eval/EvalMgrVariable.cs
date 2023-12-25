using System;
using System.Collections.Generic;

namespace Ux
{
    public partial class EvalMgr
    {
        static IDictionary<string, double> variables = new Dictionary<string, double>() {
            { "Pi", Math.PI },
            { "pi", Math.PI },
            { "E", Math.E },
            { "e", Math.E },
        };
        static IDictionary<string, Func<double>> nameToVariable = new Dictionary<string, Func<double>>();

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
    }
}
