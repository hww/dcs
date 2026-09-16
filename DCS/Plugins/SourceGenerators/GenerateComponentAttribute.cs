using System;

namespace DCS.SourceGenerators
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class GenerateComponentAttribute : Attribute
    {
        public bool IsEvent { get; set; }
        public bool IsState { get; set; }
        public int Capacity { get; set; } = 1000;
    }
}