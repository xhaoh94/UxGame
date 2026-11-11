using System;

namespace Ux
{
    public static class EntityEx
    {
        /// <summary>
        /// 「同父实体下已经有 T 就立刻走逻辑，没有则订阅它后续被添加」。
        ///
        /// 这是业务层语法糖，框架只提供原子的 Get / OnListenAddEntity，
        /// 「存在即处理」这个语义由调用方决定：
        /// <code>
        /// void OnAwake()
        /// {
        ///     Parent.ListenOrNow&lt;ItemComponent&gt;(OnItemAdded);
        /// }
        /// </code>
        ///
        /// 只建议在 OnAwake 这类同步初始化阶段调用：
        /// 检查与订阅之间不会有其他代码插入组件添加，不存在竞态窗口。
        /// </summary>
        /// <returns>true = 已存在并立刻执行；false = 已订阅，等待后续添加（或参数/父实体不合法）</returns>
        public static bool ListenOrNow<T>(this Entity self, Action<T> onReady) where T : Entity
        {
            if (self == null || onReady == null) return false;

            var parent = self.Parent;
            if (parent == null)
            {
                Log.Error("{0} 监听 {1} 失败：还没有父实体", self.GetType().Name, typeof(T).Name);
                return false;
            }

            var exist = parent.Get<T>();
            if (exist != null)
            {
                onReady(exist);
                return true;
            }

            parent.OnListenAddEntity(onReady);
            return false;
        }
    }
}
