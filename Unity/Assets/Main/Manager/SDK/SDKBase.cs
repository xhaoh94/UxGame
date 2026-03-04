using UnityEngine;

namespace Ux
{
    public interface ISDK
    {
        void Init();
        bool RegisterPlugin(string pluginClassName, string jsonParams = null);
        bool UnRegisterPlugin(string pluginClassName);
        bool HasPlugin(string pluginName);
        string Call(string pluginName, string methodName, string jsonParams = null);
    }
    public abstract class SDKBase : ISDK
    {
        public abstract void Init();
        public abstract bool RegisterPlugin(string pluginClassName, string jsonParams = null);
        public abstract bool UnRegisterPlugin(string pluginClassName);
        public abstract bool HasPlugin(string pluginName);
        public abstract string Call(string pluginName, string methodName, string jsonParams = null);
    }
    //默认sdk，用于测试
    public class SDKUx : SDKBase
    {
        public override void Init()
        {
            // Android SDK 初始化
        }
        public override string Call(string pluginName, string methodName, string jsonParams = null)
        {
            return string.Empty;
        }

        public override bool HasPlugin(string pluginName)
        {
            return true;
        }

        public override bool RegisterPlugin(string pluginClassName, string jsonParams = null)
        {
            return true;
        }
        public override bool UnRegisterPlugin(string pluginClassName)
        {
            return true;
        }
    }
}
