﻿using System;

namespace LinqInfer.Annotation
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class FeatureAttribute : Attribute
    {
        // public int Order { get; set; } ??
        public bool Ignore { get; set; }
        public Type Converter { get; set; }

        public string SetName { get; set; }
    }
}
