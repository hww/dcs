using System;

namespace DynamicComponent
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class GenerateComponentAttribute : Attribute
    {
        public bool IsEvent { get; set; }
        public bool IsState { get; set; }
        public int Capacity { get; set; } = 1000;
    }

    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class GenerateEventAttribute : Attribute
    {
        public bool IsEvent { get; set; }
        public bool IsState { get; set; }
        public int Capacity { get; set; } = 1000;
    }
}