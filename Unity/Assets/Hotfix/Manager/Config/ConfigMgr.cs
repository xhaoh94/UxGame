using Luban;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Ux
{
    public class ConfigMgr : Singleton<ConfigMgr>
    {
        public cfg.Tables Tables { get; private set; }
        public void Init()
        {
            var tablesCtor = typeof(cfg.Tables).GetConstructors()[0];
            var loaderReturnType = tablesCtor.GetParameters()[0].ParameterType.GetGenericArguments()[1];
            // 根据cfg.Tables的构造函数参数Loader的返回类型创建加载器;决定使用json加载器还是ByteBuf加载器
            System.Delegate loader = loaderReturnType == typeof(ByteBuf) ?
                new System.Func<string, ByteBuf>(LoadByteBuf)
                : (System.Delegate)new System.Func<string, JArray>(LoadJson);
            Tables = (cfg.Tables)tablesCtor.Invoke(new object[] { loader });
        }
        string _GetPath(string file)
        {
            return string.Format(PathHelper.Res.Config,file);
        }
        private JArray LoadJson(string file)
        {
            var ta = ResMgr.Ins.LoadAsset<TextAsset>(_GetPath(file));
            return JArray.Parse(ta.text);
        }

        private ByteBuf LoadByteBuf(string file)
        {
            var ta = ResMgr.Ins.LoadAsset<TextAsset>(_GetPath(file));
            return new ByteBuf(ta.bytes);
        }
    }
}