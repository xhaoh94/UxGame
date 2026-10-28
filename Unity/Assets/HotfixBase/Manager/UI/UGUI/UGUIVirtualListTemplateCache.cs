using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ux
{
    /// <summary>
    /// Item 模板异步加载缓存。用注入的加载器（如 YooAsset）按地址加载 Prefab，去重并发请求，
    /// 加载成功后写入组件运行时模板；释放时回调资源释放。纯 C#，不依赖具体资源系统。
    /// </summary>
    public sealed class UGUIVirtualListTemplateCache : IDisposable
    {
        private readonly Func<string, CancellationToken, Task<GameObject>> _load;
        private readonly Action<string> _release;
        private readonly Dictionary<string, GameObject> _assets = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, Task<GameObject>> _pending = new Dictionary<string, Task<GameObject>>(StringComparer.Ordinal);
        private readonly HashSet<string> _applied = new HashSet<string>(StringComparer.Ordinal);
        private CancellationTokenSource _source = new CancellationTokenSource();
        private bool _disposed;

        public UGUIVirtualListTemplateCache(
            Func<string, CancellationToken, Task<GameObject>> load,
            Action<string> release = null)
        {
            _load = load ?? throw new ArgumentNullException(nameof(load));
            _release = release ?? (_ => { });
        }

        public int LoadedCount => _assets.Count;
        public bool IsLoaded(string location) => !_disposed && location != null && _assets.ContainsKey(location);

        /// <summary>
        /// 按 typeId→地址 加载并写入组件模板。部分失败会返回 false，已成功的仍生效；
        /// 失败地址记录在 LastError，可再次调用仅重试失败项。
        /// </summary>
        public async Task<bool> LoadAsync(UGUIVirtualListComponent component,
            IReadOnlyDictionary<int, string> locations, CancellationToken token = default)
        {
            if (_disposed) return false;
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (locations == null) throw new ArgumentNullException(nameof(locations));
            if (token.IsCancellationRequested) return false;

            var linked = CancellationTokenSource.CreateLinkedTokenSource(_source.Token, token);
            try
            {
                bool all = true;
                foreach (var pair in locations)
                {
                    if (string.IsNullOrEmpty(pair.Value)) { all = false; continue; }
                    GameObject asset;
                    try { asset = await GetOrLoadAsync(pair.Value, linked.Token); }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { LastError = ex; all = false; continue; }
                    if (asset == null) { all = false; continue; }
                    if (!_applied.Contains(Key(pair.Key, pair.Value)))
                    {
                        component.SetRuntimeTemplate(pair.Key, asset);
                        _applied.Add(Key(pair.Key, pair.Value));
                    }
                }
                return all;
            }
            catch (OperationCanceledException) { return false; }
            finally { linked.Dispose(); }
        }

        public Exception LastError { get; private set; }

        private Task<GameObject> GetOrLoadAsync(string location, CancellationToken token)
        {
            if (_assets.TryGetValue(location, out var cached) && cached != null)
                return Task.FromResult(cached);
            if (_pending.TryGetValue(location, out var pending) && !pending.IsCompleted)
                return pending;
            var task = LoadOneAsync(location, token);
            _pending[location] = task;
            return task;
        }

        private async Task<GameObject> LoadOneAsync(string location, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var asset = await _load(location, token);
            if (_disposed) return null;
            token.ThrowIfCancellationRequested();
            if (asset == null) throw new InvalidOperationException($"模板加载失败：{location}");
            if (_assets.ContainsKey(location))
            {
                // 并发重复完成时释放后到的一份。
                _release(location);
                return _assets[location];
            }
            _assets.Add(location, asset);
            return asset;
        }

        private static string Key(int typeId, string location) => typeId + "|" + location;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { _source.Cancel(); } catch { }
            _source.Dispose();
            _pending.Clear();
            foreach (var location in _assets.Keys)
            {
                try { _release(location); } catch { /* 释放失败不阻断其余资源。 */ }
            }
            _assets.Clear();
            _applied.Clear();
        }
    }
}
