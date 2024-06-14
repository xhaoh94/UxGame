using System;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class BindFieldAttributeAttribute : Attribute
    {
        public string Name { get; }
        public BindFieldAttributeAttribute(string name)
        {
            Name = name;
        }
        public BindFieldAttributeAttribute()
        {
            Name = string.Empty;
        }
    }
}