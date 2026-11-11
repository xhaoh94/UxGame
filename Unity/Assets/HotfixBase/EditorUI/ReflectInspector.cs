#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ux
{
    /// <summary>
    /// 纯 C# 对象的 UIElements 反射面板。
    ///
    /// 和原来的 IMGUI 版（EEBase/EEInfos）相比：
    ///   1) 保留模式：字段值不变就不重绘，只有 Refresh 或用户输入才更新；
    ///   2) 可复用到任何 UIElements 窗口 / Inspector；
    ///   3) 支持按类型注册自定义绘制（RegisterCustom）。
    ///
    /// 用法：
    /// <code>
    /// var view = ReflectInspector.Build(entity);   // entity 可以是任意 C# 对象
    /// container.Add(view);
    /// view.Refresh();                              // 数据被外部修改后手动刷新
    /// </code>
    ///
    /// 约定：
    ///   - 成员名/过滤沿用 [EEViewer] / [NonEEViewer]；
    ///   - 列表、字典、嵌套对象是"构建时快照"，元素增删需要 Rebuild（Refresh 只更新值）；
    ///   - Entity 等引用类型字段以只读方式展示（显示其场景锚点）。
    /// </summary>
    public static class ReflectInspector
    {
        /// <summary>按类型注册自定义绘制器：(label, getter, setter) → VisualElement。</summary>
        private static readonly Dictionary<Type, Func<string, Func<object>, Action<object>, VisualElement>> _custom = new();

        public static void RegisterCustom<T>(Func<string, Func<object>, Action<object>, VisualElement> builder)
        {
            _custom[typeof(T)] = builder;
        }

        internal static bool TryGetCustom(Type type,
            out Func<string, Func<object>, Action<object>, VisualElement> builder)
        {
            return _custom.TryGetValue(type, out builder);
        }

        public static ReflectView Build(object instance, bool onlyAttr = false)
        {
            var view = new ReflectView(instance, onlyAttr);
            view.Rebuild();
            return view;
        }
    }

    /// <summary>一个可刷新的反射面板。</summary>
    public sealed class ReflectView : VisualElement
    {
        private readonly object _instance;
        private readonly bool _onlyAttr;
        private readonly List<(Func<object> get, Action<object> apply)> _bindings = new();
        private readonly List<ReflectView> _children = new();

        internal ReflectView(object instance, bool onlyAttr)
        {
            _instance = instance;
            _onlyAttr = onlyAttr;
            style.flexDirection = FlexDirection.Column;
        }

        /// <summary>重新构建（成员增删、列表长度变化时用）。</summary>
        public void Rebuild()
        {
            Clear();
            _bindings.Clear();
            _children.Clear();

            if (_instance == null) return;

            var specs = ReflectSpecs.Get(_instance.GetType(), _onlyAttr);
            for (int i = 0; i < specs.Length; i++)
            {
                var element = BuildMember(specs[i]);
                if (element != null) Add(element);
            }
        }

        /// <summary>刷新显示值（数据被外部修改后调用）。</summary>
        public void Refresh()
        {
            for (int i = 0; i < _bindings.Count; i++)
            {
                var binding = _bindings[i];
                try
                {
                    binding.apply(binding.get());
                }
                catch (Exception)
                {
                    // 数据在刷新间隙被销毁/置空，忽略本次刷新
                }
            }

            for (int i = 0; i < _children.Count; i++) _children[i].Refresh();
        }

        // ------------------------------------------------------------------

        private VisualElement BuildMember(ReflectSpec spec)
        {
            return BuildValue(spec.Name, spec.Type,
                () => spec.Get(_instance),
                value =>
                {
                    spec.Set(_instance, value);
                    spec.Callback?.Invoke(value);
                });
        }

        private VisualElement BuildValue(string label, Type type, Func<object> get, Action<object> onChange)
        {
            if (ReflectInspector.TryGetCustom(type, out var custom))
            {
                var customElement = custom(label, get, onChange);
                if (customElement != null) return customElement;
            }

            // ---- 整数族：统一用 IntegerField/LongField，值经 Convert 双向转换 ----
            if (type == typeof(int) || type == typeof(uint) || type == typeof(short) ||
                type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte))
            {
                return Bind(new IntegerField(label), label, type, get, onChange);
            }

            if (type == typeof(long) || type == typeof(ulong))
            {
                return Bind(new LongField(label), label, type, get, onChange);
            }

            if (type == typeof(float)) return Bind(new FloatField(label), label, type, get, onChange);
            if (type == typeof(double)) return Bind(new DoubleField(label), label, type, get, onChange);
            if (type == typeof(bool)) return Bind(new Toggle(label), label, type, get, onChange);
            if (type == typeof(string)) return Bind(new TextField(label), label, type, get, onChange);

            // ---- enum ----
            if (type.IsEnum)
            {
                var field = new EnumField(label, (Enum)Enum.ToObject(type, 0));
                field.RegisterValueChangedCallback(e => onChange(e.newValue));
                _bindings.Add((get, v => field.SetValueWithoutNotify((Enum)v)));
                SetFieldValue(field, get());
                return field;
            }

            // ---- Unity 类型 ----
            if (type == typeof(Vector2)) return Bind(new Vector2Field(label), label, type, get, onChange);
            if (type == typeof(Vector3)) return Bind(new Vector3Field(label), label, type, get, onChange);
            if (type == typeof(Vector4)) return Bind(new Vector4Field(label), label, type, get, onChange);
            if (type == typeof(Vector2Int)) return Bind(new Vector2IntField(label), label, type, get, onChange);
            if (type == typeof(Vector3Int)) return Bind(new Vector3IntField(label), label, type, get, onChange);
            if (type == typeof(Color)) return Bind(new ColorField(label), label, type, get, onChange);
            if (type == typeof(Rect)) return Bind(new RectField(label), label, type, get, onChange);
            if (type == typeof(Bounds)) return Bind(new BoundsField(label), label, type, get, onChange);

            if (type == typeof(Quaternion))
            {
                var field = new Vector3Field(label);
                field.RegisterValueChangedCallback(e => onChange(Quaternion.Euler(e.newValue)));
                _bindings.Add((get, v =>
                {
                    if (v is Quaternion q) field.SetValueWithoutNotify(q.eulerAngles);
                }));
                if (get() is Quaternion init) field.SetValueWithoutNotify(init.eulerAngles);
                return field;
            }

            // ---- Entity 引用：只读展示名字/类型 ----
            if (typeof(Entity).IsAssignableFrom(type))
            {
                var entity = get() as Entity;
                return new Label(entity == null
                    ? $"{label}: null"
                    : $"{label}: {entity.Name} [{entity.GetType().Name}]");
            }

            // ---- 列表 ----
            if (typeof(IList).IsAssignableFrom(type))
            {
                var fold = new Foldout { text = label, value = false };
                if (get() is IList list)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        var element = BuildValue($"Element {i}",
                            item?.GetType() ?? typeof(object),
                            () => item, _ => { });
                        if (element != null) fold.Add(element);
                    }
                }
                return fold;
            }

            // ---- 字典 ----
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                var fold = new Foldout { text = label, value = false };
                if (get() is IDictionary dict)
                {
                    foreach (DictionaryEntry entry in dict)
                    {
                        fold.Add(new Label($"Key: {entry.Key}"));
                        var element = BuildValue("Value",
                            entry.Value?.GetType() ?? typeof(object),
                            () => entry.Value, _ => { });
                        if (element != null) fold.Add(element);
                    }
                }
                return fold;
            }

            // ---- 带 [EEViewer] 的嵌套对象 ----
            var current = get();
            if (current != null && type.IsClass && type.IsDefined(typeof(EEViewerAttribute), false))
            {
                var child = new ReflectView(current, false);
                child.Rebuild();
                _children.Add(child);

                var fold = new Foldout { text = label, value = false };
                fold.Add(child);
                return fold;
            }

            // ---- 自定义值类型：成员是装箱副本，写回去落不到原对象，整棵子树只读 ----
            if (current != null && IsPlainValueType(current.GetType()))
            {
                var element = BuildReadOnly(label, current, 0);
                // 嵌套内容是构建时快照，Refresh 只更新标题上的摘要值。
                _bindings.Add((get, v =>
                {
                    if (element is Foldout fold) fold.text = $"{label}: {DescribeValue(v)}";
                }));
                return element;
            }

            // ---- 兜底 ----
            return new Label(current == null ? $"{label}: null" : $"{label}: {current}");
        }

        /// <summary>
        /// 值类型的只读展示：不用输入控件，递归成 Label / Foldout（列表最多展开 32 项）。
        /// </summary>
        private VisualElement BuildReadOnly(string label, object value, int depth)
        {
            if (value == null) return new Label($"{label}: null");

            var type = value.GetType();

            if (value is IEnumerable enumerable && !(value is string))
            {
                var fold = new Foldout { text = label, value = false };
                int index = 0;
                foreach (var item in enumerable)
                {
                    if (index >= 32)
                    {
                        fold.Add(new Label("……"));
                        break;
                    }
                    fold.Add(BuildReadOnly($"[{index}]", item, depth + 1));
                    index++;
                }
                return fold;
            }

            if (IsPlainValueType(type) && depth < 4)
            {
                var fold = new Foldout { text = $"{label}: {DescribeValue(value)}", value = false };
                var specs = ReflectSpecs.Get(type, false);
                for (int i = 0; i < specs.Length; i++)
                {
                    object member;
                    try
                    {
                        member = specs[i].Get(value);
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    fold.Add(BuildReadOnly(specs[i].Name, member, depth + 1));
                }
                return fold;
            }

            return new Label($"{label}: {value}");
        }

        /// <summary>Foldout 标题摘要：值类型优先取它的 Value 成员，否则用 ToString。</summary>
        private static string DescribeValue(object value)
        {
            if (value == null) return "null";

            var type = value.GetType();
            if (IsPlainValueType(type))
            {
                var property = type.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
                if (property != null && property.CanRead && property.GetIndexParameters().Length == 0 &&
                    property.PropertyType != type)
                {
                    try
                    {
                        return property.GetValue(value)?.ToString() ?? "null";
                    }
                    catch (Exception)
                    {
                        // 取不到就退回 ToString
                    }
                }
            }

            return value.ToString();
        }

        /// <summary>需要展开成结构体来看的值类型；decimal / 日期时间 / Guid 这类当标量显示。</summary>
        private static bool IsPlainValueType(Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum &&
                   type != typeof(decimal) && type != typeof(DateTime) &&
                   type != typeof(TimeSpan) && type != typeof(Guid);
        }

        /// <summary>基础类型绑定：字段值 ↔ BaseField 值用 Convert 双向转换。</summary>
        private VisualElement Bind<T>(BaseField<T> field, string label, Type memberType,
            Func<object> get, Action<object> onChange)
        {
            field.label = label;
            field.RegisterValueChangedCallback(e => onChange(ToMember(e.newValue)));
            _bindings.Add((get, v => field.SetValueWithoutNotify(ToField<T>(v))));
            SetFieldValue(field, get());
            return field;

            object ToMember(object value)
            {
                if (typeof(T) == memberType) return value;
                return value == null ? null : Convert.ChangeType(value, memberType);
            }
        }

        private static void SetFieldValue<T>(BaseField<T> field, object value)
        {
            if (value == null) return;
            try
            {
                field.SetValueWithoutNotify(ToField<T>(value));
            }
            catch (Exception)
            {
                // 类型不匹配（比如契约外的自定义类型），保持默认值
            }
        }

        private static T ToField<T>(object value)
        {
            if (value is T typed) return typed;
            if (value == null) return default;
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }

    /// <summary>类型级成员描述（与实例无关，按类型缓存）。</summary>
    public sealed class ReflectSpec
    {
        public string Name;
        public Type Type;
        public Func<object, object> GetValue;
        public Action<object, object> SetValue;
        public Action<object> Callback;

        public object Get(object instance) => GetValue(instance);
        public void Set(object instance, object value) => SetValue(instance, value);
    }

    /// <summary>按 (类型, onlyAttr) 缓存反射结果。</summary>
    public static class ReflectSpecs
    {
        private static readonly Dictionary<(Type type, bool onlyAttr), ReflectSpec[]> _cache = new();

        public static ReflectSpec[] Get(Type type, bool onlyAttr)
        {
            var key = (type, onlyAttr);
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var specs = new List<ReflectSpec>();
            const BindingFlags flag = BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                      BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var property in type.GetProperties(flag))
            {
                if (property.IsDefined(typeof(NonEEViewerAttribute), false)) continue;
                if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;

                if (!onlyAttr && property.PropertyType.IsPublic)
                {
                    if (!property.PropertyType.IsClass || property.PropertyType == typeof(string))
                    {
                        specs.Add(Make(property.Name, property.PropertyType, property.GetValue,
                            (obj, v) => { if (property.CanWrite) property.SetValue(obj, v); }, null));
                        continue;
                    }
                }

                var view = property.GetCustomAttribute<EEViewerAttribute>();
                if (view != null)
                {
                    specs.Add(Make(string.IsNullOrEmpty(view.Name) ? property.Name : view.Name,
                        property.PropertyType, property.GetValue,
                        (obj, v) => { if (property.CanWrite) property.SetValue(obj, v); },
                        view.CB));
                }
            }

            foreach (var field in type.GetFields(flag))
            {
                if (field.IsDefined(typeof(NonEEViewerAttribute), false)) continue;
                // 自动属性的 <X>k__BackingField 和属性本身重复，跳过
                if (field.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)) continue;

                if (!onlyAttr && field.FieldType.IsPublic)
                {
                    if (!field.FieldType.IsClass || field.FieldType == typeof(string))
                    {
                        specs.Add(Make(field.Name, field.FieldType, field.GetValue, field.SetValue, null));
                        continue;
                    }
                }

                var view = field.GetCustomAttribute<EEViewerAttribute>();
                if (view != null)
                {
                    specs.Add(Make(string.IsNullOrEmpty(view.Name) ? field.Name : view.Name,
                        field.FieldType, field.GetValue, field.SetValue, view.CB));
                }
            }

            var result = specs.ToArray();
            _cache[key] = result;
            return result;
        }

        private static ReflectSpec Make(string name, Type type,
            Func<object, object> get, Action<object, object> set, Action<object> callback)
        {
            return new ReflectSpec
            {
                Name = name,
                Type = type,
                GetValue = get,
                SetValue = set,
                Callback = callback,
            };
        }
    }
}
#endif
