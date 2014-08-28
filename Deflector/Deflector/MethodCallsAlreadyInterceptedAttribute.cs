using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deflector
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class MethodCallsAlreadyInterceptedAttribute : Attribute
    {
    }
}
