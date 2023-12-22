using Bright.Serialization;
using SimpleJSON;
using UnityEngine;

namespace Ux
{
    public class ConfigMgr : Singleton<ConfigMgr>
    {
        private const string Prefix = "Config_{0}";
        public cfg.Tables Tables { get; private set; }
        public void Init()
        {
            var tablesCtor = typeof(cfg.Tables).GetConstructors()[0];
            var loaderReturnType = tablesCtor.GetParameters()[0].ParameterType.GetGenericArguments()[1];
            // 根据cfg.Tables的构造函数的Loader的返回值类型决定使用json还是ByteBuf Loader
            System.Delegate loader = loaderReturnType == typeof(ByteBuf) ?
                new System.Func<string, ByteBuf>(LoadByteBuf)
                : (System.Delegate)new System.Func<string, JSONNode>(LoadJson);
            Tables = (cfg.Tables)tablesCtor.Invoke(new object[] { loader });
        }
        zstring GetKey(string file)
        {
            zstring key;
            using (zstring.Block())
            {
                key = zstring.Format(Prefix, file);
            }
            return key;
        }
        private JSONNode LoadJson(string file)
        {
            var ta = ResMgr.Ins.LoadAsset<TextAsset>(file, ResType.Config);
            return JSON.Parse(ta.text);
        }

        private ByteBuf LoadByteBuf(string file)
        {
            var ta = ResMgr.Ins.LoadAsset<TextAsset>(file, ResType.Config);
            return new ByteBuf(ta.bytes);
        }
    }
}
