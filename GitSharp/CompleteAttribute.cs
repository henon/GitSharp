using System;

namespace GitSharp
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    sealed class CompleteAttribute : Attribute
    {
    }
}
