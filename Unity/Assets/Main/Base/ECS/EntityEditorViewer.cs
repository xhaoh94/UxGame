#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ux
{
    public abstract class EEBase
    {
        protected string tag;
        protected Entity entity;
        protected Action<object> cb;
        public EEBase(string tag, Entity entity, Action<object> cb)
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
            else if (v is float vfloat)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    return EditorGUILayout.FloatField(vfloat);
                }
                return EditorGUILayout.FloatField(tag, vfloat);
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
            else if (v is Entity entity)
            {
                return EditorGUILayout.ObjectField(tag, entity.GoViewer, typeof(GameObject), true);
            }
            else if (typeof(IList).IsAssignableFrom(vType))
            {
                IList list = (IList)v;
                int count = list.Count;
                var fold = foldHS.Contains(v);
                fold = EditorGUILayout.BeginFoldoutHeaderGroup(fold, tag);
                if (fold)
                {
                    foldHS.Add(v);
                    for (int i = 0; i < count; i++)
                    {
                        Parse(i.ToString(), list[i]);
                    }
                }
                else
                {
                    foldHS.Remove(v);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            else if (typeof(IDictionary).IsAssignableFrom(vType))
            {
                IDictionary dic = v as IDictionary;
                int count = dic.Count;
                IDictionaryEnumerator enu = dic.GetEnumerator();
                var fold = foldHS.Contains(v);
                fold = EditorGUILayout.BeginFoldoutHeaderGroup(fold, tag);
                if (fold)
                {
                    foldHS.Add(v);
                    for (int i = 0; i < count; i++)
                    {
                        enu.MoveNext();
                        GUILayout.Toolbar(-1, new[] { "Key", "Vallue" });                        
                        EditorGUILayout.BeginHorizontal();
                        Parse("", enu.Key);
                        Parse("", enu.Value);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    foldHS.Remove(v);
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            return v;
        }

        protected abstract object Value { get; set; }
    }
    public class EEPropertyInfo : EEBase
    {
        PropertyInfo info;
        public EEPropertyInfo(string tag, Entity entity, PropertyInfo info, Action<object> cb) : base(tag, entity, cb)
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
        public EEFieldInfo(string tag, Entity entity, FieldInfo info, Action<object> cb) : base(tag, entity, cb)
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
    public class EntityEditorViewer : MonoBehaviour
    {
        List<EEBase> datas = new List<EEBase>();

        public void SetEntity(Entity entity)
        {
            var eType = entity.GetType();
            var properties = eType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                                          BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var property in properties)
            {
                var addAttrs = property.GetCustomAttributes(typeof(EntityViewerAttribute)).ToArray();
                if (addAttrs.Length > 0)
                {
                    if ((addAttrs[0] is EntityViewerAttribute view))
                    {
                        var name = string.IsNullOrEmpty(view.Name) ? property.Name : view.Name;
                        var data = new EEPropertyInfo(name, entity, property, view.CB);
                        datas.Add(data);
                    }
                }
            }

            var fields = eType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                              BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                var addAttrs = field.GetCustomAttributes(typeof(EntityViewerAttribute)).ToArray();
                if (addAttrs.Length > 0)
                {
                    if ((addAttrs[0] is EntityViewerAttribute view))
                    {
                        var name = string.IsNullOrEmpty(view.Name) ? field.Name : view.Name;
                        var data = new EEFieldInfo(name, entity, field, view.CB);
                        datas.Add(data);
                    }
                }
            }
        }

        public void Layout()
        {
            foreach (var data in datas)
            {
                data.Layout();
            }
        }
    }
}

#endif