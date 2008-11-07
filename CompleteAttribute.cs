using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gitty
{
    [global::System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    sealed class CompleteAttribute : Attribute
    {
        public CompleteAttribute()
        {
        }

    }
}
