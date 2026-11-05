using System;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// 窗口矩形，全部是**相对父容器的比例**（0~1）。
    /// 不用 UnityEngine.Rect：后者是像素语义，存档后与分辨率绑定。
    /// </summary>
    [Serializable]
    public struct WindowRect
    {
        /// <summary>左边缘到父容器左边的距离比例。</summary>
        public float Left;
        /// <summary>上边缘到父容器顶边的距离比例。</summary>
        public float Top;
        /// <summary>宽度占父容器宽度的比例。</summary>
        public float Width;
        /// <summary>高度占父容器高度的比例。</summary>
        public float Height;

        public bool IsValid =>
            IsFinite(Left) && IsFinite(Top) && IsFinite(Width) && IsFinite(Height) &&
            Width > 0f && Height > 0f &&
            Left >= -0.05f && Top >= -0.05f &&
            Left + Width <= 1.05f && Top + Height <= 1.05f;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public override string ToString() =>
            $"({Left:F3}, {Top:F3}, {Width:F3}, {Height:F3})";
    }

    /// <summary>
    /// 单个窗口的持久化布局。
    /// 只保存与分辨率无关的量：比例矩形 + 状态 + 层级 + 实例序号。
    /// 不保存 sortingOrder —— 它是派生值，只由层内顺序决定。
    /// </summary>
    [Serializable]
    public struct WindowLayout
    {
        /// <summary>布局格式版本，迁移用。改变字段语义时递增。</summary>
        public const int CurrentVersion = 1;

        public int Version;

        /// <summary>同类型窗口的实例序号，用于级联偏移与存档键。</summary>
        public int InstanceSlot;

        public WindowState State;
        public UIWindowTier Tier;

        /// <summary>正常状态下的矩形。</summary>
        public WindowRect Normal;

        /// <summary>最大化之前的矩形（Normal ↔ Maximized 之间有意义）。</summary>
        public WindowRect Restore;

        public static WindowLayout CreateDefault(int slot, UIWindowTier tier, WindowRect normal)
        {
            return new WindowLayout
            {
                Version = CurrentVersion,
                InstanceSlot = slot,
                State = WindowState.Normal,
                Tier = tier,
                Normal = normal,
                Restore = normal,
            };
        }

        /// <summary>
        /// 存档校验。非法值一律回落默认布局，不抛异常：
        /// 存档损坏不该让玩家连界面都打不开。
        /// </summary>
        public bool TryMigrate(out WindowLayout layout)
        {
            layout = this;
            if (Version <= 0 || Version > CurrentVersion)
                return false;

            if (!Normal.IsValid)
                return false;

            // Restore 只在最大化场景使用，非法时回退到 Normal 即可，不整体判死。
            if (!Restore.IsValid)
                layout.Restore = Normal;

            if (State is < WindowState.Normal or > WindowState.Minimized)
                layout.State = WindowState.Normal;

            if (Tier is < UIWindowTier.Normal or > UIWindowTier.AlwaysOnTop)
                layout.Tier = UIWindowTier.Normal;

            if (InstanceSlot < 0)
                layout.InstanceSlot = 0;

            return true;
        }
    }

    /// <summary>
    /// 布局持久化后端。默认实现走 PlayerPrefs，需要云同步时换实现即可。
    /// </summary>
    internal interface IWindowLayoutStore
    {
        bool TryLoad(string key, out WindowLayout layout);
        void Save(string key, in WindowLayout layout);
        void Delete(string key);
        void Clear();
    }

    /// <summary>
    /// PlayerPrefs 实现：单机 UI 偏好足够，键前缀固定，便于一次性清理。
    /// </summary>
    internal sealed class PlayerPrefsWindowLayoutStore : IWindowLayoutStore
    {
        private const string Prefix = "Ux.WindowLayout.";

        public bool TryLoad(string key, out WindowLayout layout)
        {
            layout = default;
            if (string.IsNullOrEmpty(key))
                return false;

            var json = PlayerPrefs.GetString(Prefix + key, string.Empty);
            if (string.IsNullOrEmpty(json))
                return false;

            WindowLayout parsed;
            try
            {
                parsed = JsonUtility.FromJson<WindowLayout>(json);
            }
            catch (Exception ex)
            {
                Log.Warning("窗口布局反序列化失败，已忽略。Key[{0}] {1}", key, ex.Message);
                return false;
            }

            if (!parsed.TryMigrate(out layout))
            {
                Log.Warning("窗口布局不合法，已回落默认值。Key[{0}] Version[{1}]", key, parsed.Version);
                return false;
            }

            return true;
        }

        public void Save(string key, in WindowLayout layout)
        {
            if (string.IsNullOrEmpty(key))
                return;

            PlayerPrefs.SetString(Prefix + key, JsonUtility.ToJson(layout));
        }

        public void Delete(string key)
        {
            if (!string.IsNullOrEmpty(key))
                PlayerPrefs.DeleteKey(Prefix + key);
        }

        public void Clear()
        {
            // PlayerPrefs 无法按前缀删，只能从窗口注册表逐项删；
            // 因此真正的清理由 UIWindowService 按已知 key 调用 Delete。
        }
    }
}
