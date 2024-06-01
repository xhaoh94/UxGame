using System;
using UnityEngine;

namespace Ux
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TLTrackAttribute : Attribute
    {
        public string Lb { get;private set; }
        public Color Color { get; private set; }
        public TLTrackAttribute(string lb, float r, float g, float b) { 
            this.Lb = lb;
            this.Color = new Color(r/255f, g/255f, b / 255f, 255);
        }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TLTrackClipTypeAttribute : Attribute
    {
        public Type ClipType { get; private set; }        
        public TLTrackClipTypeAttribute(Type clipType)
        {
            this.ClipType = clipType;            
        }
    }
}
