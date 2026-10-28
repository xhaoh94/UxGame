using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Ux
{
    /// <summary>
    /// YooAsset 模板加载器。只加载 Prefab 资源本身并持有句柄，实例化仍由列表的工厂完成，
    /// 因此模板不会被实例化进场景，也不会与 UnityPool 的对象池语义混淆。
    /// </summary>
    public sealed class YooAssetItemTemplateLoader : IDisposable
    {
        private readonly Dictionary<string, AssetHandle> _handles = new Dictionary<string, AssetHandle>(StringComparer.Ordinal);
        private readonly YooType _packageType;
        private bool _disposed;

        public YooAssetItemTemplateLoader(YooType packageType = YooType.Main)
        {
            _packageType = packageType;
        }

        public async Task<GameObject> LoadAsync(string location, CancellationToken token)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(YooAssetItemTemplateLoader));
            if (string.IsNullOrEmpty(location)) throw new ArgumentException("资源地址不能为空。", nameof(location));
            token.ThrowIfCancellationRequested();

            if (_handles.TryGetValue(location, out var existing))
            {
                var asset = existing?.GetAssetObject<GameObject>();
                if (asset != null) return asset;
                Release(location);
            }

            var package = YooMgr.Ins.GetPackage(_packageType);
            if (package?.Package == null)
                throw new InvalidOperationException($"YooAsset 资源包未初始化：{_packageType}");

            var handle = package.Package.LoadAssetAsync<GameObject>(location);
            try
            {
                await handle.ToUniTask();
                token.ThrowIfCancellationRequested();
                var prefab = handle.GetAssetObject<GameObject>();
                if (prefab == null)
                {
                    handle.Release();
                    throw new InvalidOperationException($"模板资源不是有效的 GameObject：{location}");
                }
                _handles[location] = handle;
                return prefab;
            }
            catch
            {
                if (!_handles.ContainsKey(location)) handle.Release();
                throw;
            }
        }

        public void Release(string location)
        {
            if (location == null || !_handles.TryGetValue(location, out var handle)) return;
            _handles.Remove(location);
            try { handle?.Release(); } catch { /* 句柄可能已被资源系统回收。 */ }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var handle in _handles.Values)
            {
                try { handle?.Release(); } catch { }
            }
            _handles.Clear();
        }
    }
}
