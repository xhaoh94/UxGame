#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Ux
{
    public abstract class EEBase
    {
        protected string tag;
        protected object entity;
        protected Action<object> cb;
        public EEBase(string tag, object entity, Action<object> cb)
        {
            this.tag = tag;
            this.entity = entity;
            this.cb = cb;
        }

        public void Layout()
        {
            var v = Value;
            if (v == null) return;
            if (cb != null)
            {
                cb(v);
                return;
            }
            Value = Parse(tag, v);
        }
        HashSet<object> foldHS = new HashSet<object>();
        protected object Parse(string tag, object v)
        {
            var vType = v.GetType();
            if (v is int vint)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return EditorGUILayout.IntField(vint);
                }
                return EditorGUILayout.IntField(tag, vint);
            }
            else if (v is uint vuint)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return EditorGUILayout.IntField((int)vuint);
                }
                return EditorGUILayout.IntField(tag, (int)vuint);
            }
            else if (v is short vshort)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return (short)EditorGUILayout.IntField(vshort);
                }
                return (short)EditorGUILayout.IntField(tag, vshort);
            }
            else if (v is ushort vushort)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return (ushort)EditorGUILayout.IntField(vushort);
                }
                return (ushort)EditorGUILayout.IntField(tag, vushort);
            }
            else if (v is long vlong)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return EditorGUILayout.LongField(vlong);
                }
                return EditorGUILayout.LongField(tag, vlong);
            }
            else if (v is ulong vulong)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return EditorGUILayout.LongField((long)vulong);
                }
                return EditorGUILayout.LongField(tag, (long)vulong);
            }
            else if (v is float vfloat)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return EditorGUILayout.FloatField(vfloat);
                }
                return EditorGUILayout.FloatField(tag, vfloat);
            }
            else if (v is double vdouble)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return EditorGUILayout.DoubleField(vdouble);
                }
                return EditorGUILayout.DoubleField(tag, vdouble);
            }
            else if (v is string vstring)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return EditorGUILayout.TextField(vstring);
                }
                return EditorGUILayout.TextField(tag, vstring);
            }
            else if (v is Vector2 vector2)
            {
                return EditorGUILayout.Vector2Field(tag, vector2);
            }
            else if (v is Vector2Int vectorInt)
            {
                return EditorGUILayout.Vector2IntField(tag, vectorInt);
            }
            else if (v is Vector3 vector)
            {
                return EditorGUILayout.Vector3Field(tag, vector);
            }
            else if (v is Vector3Int vector3Int)
            {
                return EditorGUILayout.Vector3IntField(tag, vector3Int);
            }
            else if (v is Quaternion quaternion)
            {
                var eulerAngles = EditorGUILayout.Vector3Field(tag, quaternion.eulerAngles);
                quaternion.eulerAngles = eulerAngles;
                return quaternion;
            }
            else if (v is Entity entity)
            {
                return EditorGUILayout.ObjectField(tag, entity.GoViewer, typeof(GameObject), true);
            }
            else if (typeof(IList).IsAssignableFrom(vType))
            {
                IList list = (IList)v;
                int count = list.Count;
                var fold = foldHS.Contains(v);
                if (string.IsNullOrEmpty(tag)) tag = vType.Name;


                fold = EditorGUILayout.Foldout(fold, tag);
                if (fold)
                {
                    foldHS.Add(v);
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < count; i++)
                    {
                        EditorGUILayout.BeginVertical();
                        Parse($"Element {i}", list[i]);
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUI.indentLevel--;
                }
                else
                {
                    foldHS.Remove(v);
                }
            }
            else if (typeof(IDictionary).IsAssignableFrom(vType))
            {
                IDictionary dic = v as IDictionary;
                int count = dic.Count;
                IDictionaryEnumerator enu = dic.GetEnumerator();
                var fold = foldHS.Contains(v);
                if (string.IsNullOrEmpty(tag)) tag = vType.Name;
                fold = EditorGUILayout.Foldout(fold, tag);
                if (fold)
                {
                    foldHS.Add(v);
                    EditorGUI.indentLevel++;
                    for (int i = 0; i < count; i++)
                    {
                        enu.MoveNext();
                        //GUILayout.Toolbar(-1, new[] { "Key", "Vallue" });
                        //EditorGUILayout.BeginHorizontal();
                        Parse("Key", enu.Key);
                        EditorGUI.indentLevel++;
                        Parse("Value", enu.Value);
                        EditorGUI.indentLevel--;
                        //EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
                else
                {
                    foldHS.Remove(v);
                }
            }
            else if (vType.IsClass)
            {
                var addAttrs = vType.GetCustomAttributes(typeof(EEViewerAttribute));
                if (addAttrs.Count() > 0)
                {
                    var infos = new EEInfos(v);
                    if (infos.Has)
                    {
                        var fold = foldHS.Contains(v);
                        if (string.IsNullOrEmpty(tag)) tag = vType.Name;
                        fold = EditorGUILayout.Foldout(fold, tag);
                        if (fold)
                        {
                            foldHS.Add(v);
                            EditorGUILayout.BeginVertical();
                            infos.Layout();
                            EditorGUILayout.EndVertical();
                        }
                        else
                        {
                            foldHS.Remove(v);
                        }
                    }
                }
            }

            return v;
        }

        protected abstract object Value { get; set; }
    }
    public class EEPropertyInfo : EEBase
    {
        PropertyInfo info;
        public EEPropertyInfo(string tag, object entity, PropertyInfo info, Action<object> cb) : base(tag, entity, cb)
        {
            this.info = info;
        }

        protected override object Value
        {
            get
            {
                return info.GetValue(entity);
            }
            set
            {
                info.SetValue(entity, value);
            }
        }

    }
    public class EEFieldInfo : EEBase
    {
        FieldInfo info;
        public EEFieldInfo(string tag, object entity, FieldInfo info, Action<object> cb) : base(tag, entity, cb)
        {
            this.info = info;
        }

        protected override object Value
        {
            get
            {
                return info.GetValue(entity);
            }
            set
            {
                info.SetValue(entity, value);
            }
        }

    }

    public class EEInfos
    {
        List<EEBase> infos;
        public EEInfos(object obj, bool onlyAttr = false)
        {
            Parse(obj, onlyAttr);
        }
        void Parse(object obj, bool onlyAttr = false)
        {
            infos = new List<EEBase>();
            var eType = obj.GetType();

            var flag = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var properties = eType.GetProperties(flag);

            foreach (var property in properties)
            {
                var nonAttr = property.GetCustomAttributes(typeof(NonEEViewerAttribute));
                if (nonAttr.Count() > 0)
                {
                    continue;
                }
                if (!onlyAttr && property.PropertyType.IsPublic)
                {
                    if (!property.PropertyType.IsClass || property.PropertyType == typeof(string))
                    {
                        var data = new EEPropertyInfo(property.Name, obj, property, null);
                        infos.Add(data);
                        continue;
                    }
                }

                var addAttrs = property.GetCustomAttributes(typeof(EEViewerAttribute)).ToArray();
                if (addAttrs.Length > 0)
                {
                    if ((addAttrs[0] is EEViewerAttribute view))
                    {
                        var name = string.IsNullOrEmpty(view.Name) ? property.Name : view.Name;
                        var data = new EEPropertyInfo(name, obj, property, view.CB);
                        infos.Add(data);
                    }
                }
            }


            var fields = eType.GetFields(flag);
            foreach (var field in fields)
            {
                var nonAttrs = field.GetCustomAttributes(typeof(NonEEViewerAttribute));
                if (nonAttrs.Count() > 0)
                {
                    continue;
                }

                if (!onlyAttr && field.FieldType.IsPublic)
                {
                    if (!field.FieldType.IsClass || field.FieldType == typeof(string))
                    {
                        var data = new EEFieldInfo(field.Name, obj, field, null);
                        infos.Add(data);
                        continue;
                    }
                }

                var addAttrs = field.GetCustomAttributes(typeof(EEViewerAttribute)).ToArray();
                if (addAttrs.Length > 0)
                {
                    if ((addAttrs[0] is EEViewerAttribute view))
                    {
                        var name = string.IsNullOrEmpty(view.Name) ? field.Name : view.Name;
                        var data = new EEFieldInfo(name, obj, field, view.CB);
                        infos.Add(data);
                    }
                }

            }
        }

        public void Layout()
        {
            if (infos == null) return;
            foreach (var data in infos)
            {
                data.Layout();
            }
        }
        public bool Has
        {
            get { return infos != null && infos.Count > 0; }
        }
    }
    public class EntityViewer : MonoBehaviour
    {
        EEInfos infos;
        public void SetEntity(Entity entity)
        {
            infos = new EEInfos(entity, true);
        }

        public void Layout()
        {
            infos?.Layout();
        }
    }
}

#endif